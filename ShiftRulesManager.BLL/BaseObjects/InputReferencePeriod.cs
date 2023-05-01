using System;

namespace ShiftRulesManager.BLL
{
    public class InputReferencePeriod
    {
        public InputReferencePeriod()
        {
        }

        public DateTime StartPeriod { get; set; }
        public DateTime EndPeriod { get; set; }
        public int PuntoVenditaId { get; set; }
        public int RepartoId { get; set; }
    }
}