namespace ShiftRulesManager.BLL
{
    public enum MessageLevel
    {
        OK = 0,
        Error = 1,
        Warning = 2,
        KO = 9,
    }

    public class ValidationMessage
    {
        public ValidationMessage()
        {
            Level = MessageLevel.OK;
            Message = "OK";
        }

        public int EventId { get; set; }
        public MessageLevel Level { get; set; }
        public string Message { get; set; }
    }
}
