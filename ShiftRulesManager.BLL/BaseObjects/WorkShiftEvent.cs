using System;

namespace ShiftRulesManager.BLL
{
    public enum CheckStatusEnum
    {
        OK,
        KO,
    }

    public class WorkShiftEvent
    {
        public int EventId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int dipendenteId { get; set; }
        public int idPuntoVendita { get; set; }
        public int idReparto { get; set; }
        public string? stato { get; set; }

        public CheckStatusEnum CheckStatus { get; set; }

        public WorkShiftEvent Clone()
        {
            WorkShiftEvent clone = new WorkShiftEvent()
            {
                EventId = this.EventId,
                Title = this.Title,
                Description = this.Description,
                Start = this.Start,
                End = this.End,
                dipendenteId = this.dipendenteId,
                idPuntoVendita = this.idPuntoVendita,
                idReparto = this.idReparto,
                stato = this.stato,
                CheckStatus = this.CheckStatus,
            };

            return clone;
        }
    }
}
