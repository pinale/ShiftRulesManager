using System.Collections.Generic;
using System.Linq;

namespace ShiftRulesManager.BLL
{
    public class RulesValidation : IRulesValidation
    {
        public RulesValidation() 
        { 
        }

        public IEnumerable<ValidationMessage> GetAllValidationRules(InputReferencePeriod referencePeriod, List<WorkShiftEvent> eventsList, List<EmployeeMasterData> employeesMasterData)
        {
            // elenco dei risulati da restutuire
            List<ValidationMessage> checkResults = new List<ValidationMessage>();

            // raggruppamento tutti gli eventi per dipendente
            var grpEvents =
                (from evt in eventsList
                 group evt by new { evt.dipendenteId } into g
                 select new
                 {
                     dipendenteId = g.Key.dipendenteId,
                     Eventi = g.ToList()
                 })
                 .ToList();

            // ciclo sui gruppi per processare un dipendente alla volta
            foreach (var grp in grpEvents)
            {
                // cerca i dati anagrafici
                var masterData = employeesMasterData.Where(x => x.EmployeeId == grp.dipendenteId).FirstOrDefault();
                if (masterData == null)
                {
                    checkResults.Add(new ValidationMessage()
                    {
                        EventId = grp.dipendenteId,
                        Level = MessageLevel.KO,
                        Message = $"Dati anagrafici non presenti per il dipendente [{grp.dipendenteId}]"
                    });
                }
                else
                {
                    // per ciascun dipendente crea il contesto di regole, assegnando gli eventi collegati e i parametri del periodo da analizzare 
                    EmployeeRulesContext context = new EmployeeRulesContext()
                    {
                        EmployeeId = grp.dipendenteId,
                        Events = grp.Eventi,
                        MasterData = masterData,
                        ReferencePeriod = referencePeriod
                    };

                    // Istanzia il controllore
                    EmployeeRulesManager rulesToCheck = new EmployeeRulesManager(context);

                    // Verifica tutte le regole per il dipendente corrente
                    var results = rulesToCheck.AllValidationResults();

                    // accoda gli esiti del singolo dipendente all'elenco generale
                    checkResults.AddRange(results);
                }
            }

            // Restituisce gli esiti
            return checkResults;
        }
    }
}
