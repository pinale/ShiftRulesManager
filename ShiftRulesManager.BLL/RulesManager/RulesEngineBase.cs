using System.Collections.Generic;
using System.Linq;

namespace ShiftRulesManager.BLL
{
    public enum Priority
	{
		Ascending,
		Descending,
	}

    public abstract class Rule<T>
    {
        public string RuleName { get; set; }
        public int PriorityId { get; set; }
        public List<ValidationMessage> ValidationMessages { get; set; }
        public abstract bool IsValid(T ctx);
    }

    public class RulesEngineBase<T>
    {
        #region Properties

        public T Context { get; set; }
        public Priority Priority { get; set; }
        public IEnumerable<Rule<T>>? Rules { get; set; }

        #endregion

        #region Public Methods (Metodo Principale)

        // -    Controlla tutte le regole e restituisce gli esiti (una collezione di ValidationMessage).
        // -    Una singola regola può avere più messaggi di esito associati per gestire il caso in cui
        //      vengano rilevate più violazioni della stessa regola.
        public virtual List<ValidationMessage> AllValidationResults() 
        {
            var ValidationResults = new List<ValidationMessage>();

            var rules = GetRuleByPriority();

            foreach (var rule in rules)
            {
                var b = rule.IsValid(Context);
                if (!b)
                {
                    ValidationResults.AddRange(rule.ValidationMessages);
                }
                else
                    ValidationResults.Add(new ValidationMessage() { Level = MessageLevel.OK });
            }

            return ValidationResults;
        }

        #endregion

        #region Private Methods

        private IEnumerable<Rule<T>> GetRuleByPriority()
        {
            return 
                Priority == Priority.Ascending ?
                Rules.OrderBy(x => x.PriorityId) :
                Rules.OrderByDescending(x => x.PriorityId);
        }

        #endregion


    }
}
