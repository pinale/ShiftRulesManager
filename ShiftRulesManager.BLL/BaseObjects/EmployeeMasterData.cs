namespace ShiftRulesManager.BLL
{
    public class EmployeeMasterData
    {
        public EmployeeMasterData()
        {
        }

        public int EmployeeId { get; set; }
        public double? MaxWeeklyHours { get; set; }
        public double? MinDailyHours { get; set; }
        public double? MaxDailyHours { get; set; }
        public double? MinNbHoursBetweenShift { get; set; }
        public double? MinWeeklyRest { get; set; }
    }
}
