using System.Collections.Generic;

namespace ShiftRulesManager.BLL
{
    public interface IRulesValidation
    {
        IEnumerable<ValidationMessage> GetAllValidationRules(InputReferencePeriod referencePeriod, List<WorkShiftEvent> EventsList, List<EmployeeMasterData> employeesMasterDat);
    }
}
