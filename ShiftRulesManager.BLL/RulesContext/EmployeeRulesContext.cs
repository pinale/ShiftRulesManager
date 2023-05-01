using System.Collections.Generic;

namespace ShiftRulesManager.BLL
{
    public class EmployeeRulesContext
    {
        public EmployeeRulesContext()
        {
            ReferencePeriod = new InputReferencePeriod();
            Events = new List<WorkShiftEvent>();
            MasterData = new EmployeeMasterData();
        }

        public int EmployeeId { get; set; }
        public InputReferencePeriod ReferencePeriod { get; set; }
        public List<WorkShiftEvent> Events { get; set; }
        public EmployeeMasterData MasterData { get; set; }
    }
}
