using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ShiftRulesManager.BLL
{
    public class EmployeeRulesManager : RulesEngineBase<EmployeeRulesContext>
    {
        // -    Implementa i controlli per ciascun dipendente.
        // -    Per gli eventi invalidi viene posto CheckStatus = KO e viene aggiunto un messaggio nella coda dei messaggi di validazione. 
        //      Questi verrano poi ignorati nel successivo controllo che pertanto acquisirà solo quelli con CheckStatus = OK.
        public EmployeeRulesManager(EmployeeRulesContext context)
        {
            Context = context;
            Priority = Priority.Ascending;
            Rules = new List<Rule<EmployeeRulesContext>> 
            {
                new CheckEventPeriods($"Verifica_Slot_Temporali_Dip.{context.EmployeeId}", 1),
                new CheckOverlapping($"Verifica_Sovrapposizioni_Periodi_Dip.{context.EmployeeId}", 2),
                new CheckDailyHours($"Verifica_Max_H_Giornaliere_Dip.{context.EmployeeId}", 3),
                new CheckGapBetweenShifts($"Verifica_Periodi_Fra_Turni_Dip.{context.EmployeeId}", 4),
                new Check1DayRest($"Verifica_Riposo_Settimanale_Dip.{context.EmployeeId}", 5),
            };
        }

        // -    Controlla che ciascuno degli eventi passati in input abbia data inizio e data fine consistenti.
        private class CheckEventPeriods : Rule<EmployeeRulesContext>
        {
            public CheckEventPeriods(string ruleName, int priority)
            {
                RuleName = ruleName;
                PriorityId = priority;
                ValidationMessages = new List<ValidationMessage>();
            }

            public override bool IsValid(EmployeeRulesContext context)
            {
                foreach (var evt in context.Events.Where(x => x.CheckStatus != CheckStatusEnum.KO))
                {
                    if (evt.End <= evt.Start)
                    {
                        ValidationMessages.Add(new ValidationMessage()
                        {
                            EventId = evt.EventId,
                            Level = MessageLevel.Error,
                            //Message = $"Evento: [({evt.EventId}) {evt.Title}] - " +
                            //          $"Data Fine Evento [{evt.End}] <= Data Inizio Evento [{evt.Start}]."
                            Message = $"Il dipendente [{context.MasterData.EmployeeName}] ha un turno con Data Fine [{evt.End}] <= di Data Inizio [{evt.Start}]."
                        });
                        evt.CheckStatus = CheckStatusEnum.KO;
                    }
                    else if (evt.End < context.ReferencePeriod.StartPeriod || evt.Start > context.ReferencePeriod.EndPeriod)
                    {
                        ValidationMessages.Add(new ValidationMessage()
                        {
                            EventId = evt.EventId,
                            Level = MessageLevel.Warning,
                            //Message = $"Evento: [({evt.EventId}) {evt.Title}] - " +
                            //          $"Periodo [{evt.Start}-{evt.End}] non incluso nell'intervallo di analisi."
                            Message = $"Il dipendente {context.MasterData.EmployeeName} ha il turno [{evt.Start}-{evt.End}] non compreso nella settimana in analisi."
                        });
                        evt.CheckStatus = CheckStatusEnum.KO;
                    }
                }

                bool bRet = ValidationMessages.All(x => x.Level == MessageLevel.OK);

                return bRet;
            }
        }

        // -    Controlla che non ci siano periodi duplicati o sovrapposti per ciascun dipendente.
        // -    Esempi di casi non consentiti:
        //      - un dipendente con un turno su più reparti, nello stesso orario ma in punti di vendita diversi.
        //      - un dipendente con un turno in un giorno/PdV/Reparto che inizia a un orario precedente alla fine di un
        //        altro turno già assegnato al dipendente per lo stesso giorno/PdV.
        private class CheckOverlapping : Rule<EmployeeRulesContext>
        {
            public CheckOverlapping(string ruleName, int priority)
            {
                RuleName = ruleName;
                PriorityId = priority;
                ValidationMessages = new List<ValidationMessage>();
            }

            public override bool IsValid(EmployeeRulesContext context)
            {
                #region Duplicazioni 

                // Raggruppa gli eventi per tutte le chiavi
                var grpEvents1 =
                        (from e in context.Events.Where(x => x.CheckStatus != CheckStatusEnum.KO)
                         group e by new
                         {
                             e.dipendenteId,
                             e.Start,
                             e.End,
                             e.idPuntoVendita,
                             e.idReparto
                         } into g
                         select new
                         {
                             EmployeeId = g.Key.dipendenteId,
                             Start = g.Key.Start,
                             End = g.Key.End,
                             Pdv = g.Key.idPuntoVendita,
                             Reparto = g.Key.idReparto,
                             Items = g.ToList()
                         })
                         .ToList();

                // Cicla sugli eventi duplicati
                foreach (var grp in grpEvents1.Where(x => x.Items.Count() > 1))
                {
                    foreach (var evt in grp.Items)
                    {
                        ValidationMessages.Add(new ValidationMessage()
                        {
                            EventId = evt.EventId,
                            Level = MessageLevel.Error,
                            Message = $"Evento: [({evt.EventId}) {evt.Title}], PdV: [{evt.idPuntoVendita}], Reparto: [{evt.idReparto}], Periodo: [{evt.Start}-{evt.End}] - " +
                                      $"Periodo duplicato."
                        });
                        evt.CheckStatus = CheckStatusEnum.KO;
                    }
                }

                #endregion

                #region Sovrapposizioni Periodi

                // Raggruppa gli eventi per Dipendente/PdV/Reparto
                var grpEvents2 =
                        (from e in context.Events.Where(x => x.CheckStatus != CheckStatusEnum.KO)
                         group e by new
                         {
                             e.dipendenteId,
                             e.idPuntoVendita,
                             e.idReparto
                         } into g
                         select new
                         {
                             EmployeeId = g.Key.dipendenteId,
                             Pdv = g.Key.idPuntoVendita,
                             Reparto = g.Key.idReparto,
                             Items = g.ToList()
                         })
                         .ToList();

                // Cicla sui gruppi e per ciascuno cicla sugli eventi ordinati per data inizio turno
                foreach (var grp in grpEvents2)
                {
                    var ordereditems = grp.Items.OrderBy(o => o.Start).ToList();

                    WorkShiftEvent? previousEvt = null;
                    foreach (var evt in ordereditems)
                    {
                        if (previousEvt != null && evt.Start < previousEvt.End)
                        {
                            ValidationMessages.Add(new ValidationMessage()
                            {
                                EventId = evt.EventId,
                                Level = MessageLevel.Error,
                                //Message = $"Evento: [({evt.EventId}) {evt.Title}], Periodo: [{evt.Start}-{evt.End}] - " +
                                //          $"Sovrapposizione con evento: [({previousEvt.EventId}) {previousEvt.Title}], Periodo: [{previousEvt.Start}-{previousEvt.End}]."
                                Message = $"Il dipendente {context.MasterData.EmployeeName} ha il turno: [{evt.Start}-{evt.End}] in sovrapposizione con il turno: [{previousEvt.Start}-{previousEvt.End}]."
                            });
                            evt.CheckStatus = CheckStatusEnum.KO;
                        }

                        previousEvt = evt.Clone();
                    }
                }

                #endregion

                #region Sovrapposizioni PdV

                // Raggruppa gli eventi per Dipendente/Reparto
                var grpEvents3 =
                        (from e in context.Events.Where(x => x.CheckStatus != CheckStatusEnum.KO)
                         group e by new
                         {
                             e.dipendenteId,
                             e.Start,
                             e.End,
                             e.idReparto
                         } into g
                         select new
                         {
                             EmployeeId = g.Key.dipendenteId,
                             Start = g.Key.Start,
                             End = g.Key.End,
                             Reparto = g.Key.idReparto,
                             Items = g.ToList()
                         })
                         .ToList();

                // Cicla sui gruppi e per ciascuno cicla sugli eventi ordinati per data inizio turno
                foreach (var grp in grpEvents3.Where(x => x.Items.Count() > 1))
                {
                    foreach (var evt in grp.Items)
                    {
                        ValidationMessages.Add(new ValidationMessage()
                        {
                            EventId = evt.EventId,
                            Level = MessageLevel.Error,
                            Message = $"Evento: [({evt.EventId}) {evt.Title}], Periodo: [{evt.Start}-{evt.End}], PdV: [{evt.idPuntoVendita}], Reparto: [{evt.idReparto}] - " +
                                        $"Sovrapposizione PdV."
                        });
                        evt.CheckStatus = CheckStatusEnum.KO;
                    }
                }


                #endregion

                bool bRet = ValidationMessages.All(x => x.Level == MessageLevel.OK);

                return bRet;
            }
        }

        // -    Controlla che un dipendende non superi le ore giornaliere stabilite dal contratto.
        private class CheckDailyHours : Rule<EmployeeRulesContext>
        {
            public CheckDailyHours(string ruleName, int priority)
            {
                RuleName = ruleName;
                PriorityId = priority;
                ValidationMessages = new List<ValidationMessage>();
            }

            public override bool IsValid(EmployeeRulesContext context)
            {
                var maxDailyHours = context.MasterData != null && context.MasterData.MaxDailyHours.HasValue ? 
                                    context.MasterData.MaxDailyHours.Value : 0;
                var minDailyHours = context.MasterData != null && context.MasterData.MinDailyHours.HasValue ?
                                    context.MasterData.MinDailyHours.Value : 0;

                var days = new Dictionary<DateTime, double>();

                foreach (var evt in context.Events.Where(x => x.CheckStatus != CheckStatusEnum.KO && 
                                                              x.stato.Trim().ToLower() != "riposo")
                                                  .OrderBy(o => o.Start))
                {
                    var fromDate = new DateTime(evt.Start.Year, evt.Start.Month, evt.Start.Day);
                    var toDate = new DateTime(evt.End.Year, evt.End.Month, evt.End.Day);

                    if (!days.ContainsKey(fromDate))
                        days.Add(fromDate, 0);

                    if (!days.ContainsKey(toDate))
                        days.Add(toDate, 0);

                    TimeSpan ts;
                    var hh = 0.0;
                    if (fromDate == toDate)
                    {
                        ts = evt.End - evt.Start;
                        hh = ts.TotalHours;
                        days[fromDate] += hh;
                    }
                    else
                    {
                        var midnight = new DateTime(evt.Start.Year, evt.Start.Month, evt.Start.Day, 23, 59, 59);
                        ts = midnight - evt.Start;
                        hh = ts.TotalHours;
                        days[fromDate] += hh;

                        ts = evt.End - midnight;
                        hh = ts.TotalHours;
                        days[toDate] += hh;
                    }
                }

                var bCheck = days.Values.Where(x => x > maxDailyHours).Count() == 0;

                if (!bCheck)
                {
                    foreach (var key in days.Keys)
                    {
                        if (days[key] > maxDailyHours)
                        {
                            ValidationMessages.Add(new ValidationMessage()
                            {
                                EventId = 0,
                                Level = MessageLevel.Error,
                                //Message = $"Il dipendente [{context.EmployeeId}] ha un eccesso di ore assegnate [{days[key]}/{maxDailyHours}] per il giorno [{key.ToString("d")}]"
                                Message = $"Il dipendente [{context.MasterData.EmployeeName}] ha un eccesso di ore assegnate [{days[key]}/{maxDailyHours}] nel giorno [{key.ToString("d")}]"
                            });
                        }
                        else if (days[key] < minDailyHours)
                        {
                            ValidationMessages.Add(new ValidationMessage()
                            {
                                EventId = 0,
                                Level = MessageLevel.Error,
                                Message = $"Il dipendente [{context.MasterData.EmployeeName}] ha una carenza di ore assegnate [{days[key]}/{minDailyHours}] nel giorno [{key.ToString("d")}]"
                            });
                        }
                    }
                }

                return bCheck; 
            }
        }

        // -    Controlla che un tra un turno e quello successivo cia sia un numero minimo di ore stabilito.
        private class CheckGapBetweenShifts : Rule<EmployeeRulesContext>
        {
            public CheckGapBetweenShifts(string ruleName, int priority)
            {
                RuleName = ruleName;
                PriorityId = priority;
                ValidationMessages = new List<ValidationMessage>();
            }

            public override bool IsValid(EmployeeRulesContext context)
            {
                var minGap = context.MasterData != null && context.MasterData.MinNbHoursBetweenShift.HasValue ?
                             context.MasterData.MinNbHoursBetweenShift.Value : 0;

                var days = new Dictionary<DateTime, DateTime[]>();
                WorkShiftEvent? prevShift = null;

                foreach (var evt in context.Events.Where(x => x.CheckStatus != CheckStatusEnum.KO &&
                                                         x.stato.Trim().ToLower() != "riposo")
                                                  .OrderBy(o => o.Start))
                {
                    var fromDate = new DateTime(evt.Start.Year, evt.Start.Month, evt.Start.Day);
                    var toDate = new DateTime(evt.End.Year, evt.End.Month, evt.End.Day);

                    if (!days.ContainsKey(fromDate))
                        days.Add(fromDate, new DateTime[] { evt.Start, evt.End});

                    if (!days.ContainsKey(toDate))
                        days.Add(toDate, new DateTime[] { evt.Start, evt.End });

                    if (fromDate == toDate)
                        days[fromDate][1] = evt.End;
                    else
                    {
                        var midnight = new DateTime(evt.Start.Year, evt.Start.Month, evt.Start.Day, 23, 59, 59);
                        days[fromDate][1] = midnight;
                        days[toDate][0] = evt.Start;
                        days[toDate][1] = evt.End;
                    }
                }

                bool bCheck = true;
                var hh = 0.0;

                DateTime? prevDate = null;
                foreach (var key in days.Keys)
                {
                    if (prevDate != null)
                    {
                        TimeSpan ts = days[key][0] - prevDate.Value;
                        hh = ts.TotalHours;
                        if (hh < minGap)
                        {
                            bCheck = false;
                            ValidationMessages.Add(new ValidationMessage()
                            {
                                EventId = 0,
                                Level = MessageLevel.Error,
                                //Message = $"Il dipendente [{context.EmployeeId}] non usufruisce del minimo n.di ore [{minGap}] tra i giorni " +
                                //          $"[{prevDate.Value.ToString("d")}] e [{key.ToString("d")})"
                                Message = $"Il dipendente [{context.MasterData.EmployeeName}] non effettua il minimo n.di ore [{minGap}] di pausa tra i turni [{prevDate.Value.ToString("d")}] e [{key.ToString("d")})"
                            });
                        }
                    }

                    prevDate = days[key][1];
                }

                return bCheck;
            }
        }

        // -    Controlla che cia siano 24h di riposo consecutivo in 7 gg.
        private class Check1DayRest : Rule<EmployeeRulesContext>
        {
            public Check1DayRest(string ruleName, int priority)
            {
                RuleName = ruleName;
                PriorityId = priority;
                ValidationMessages = new List<ValidationMessage>();
            }

            public override bool IsValid(EmployeeRulesContext context)
            {
                var minRest = context.MasterData != null && context.MasterData.MinWeeklyRest.HasValue ?
                              context.MasterData.MinWeeklyRest.Value : 0;

                var days = new Dictionary<DateTime, DateTime[]>();

                foreach (var evt in context.Events.Where(x => x.CheckStatus != CheckStatusEnum.KO &&
                                                         x.stato.Trim().ToLower() != "riposo")
                                                  .OrderBy(o => o.Start))
                {
                    var fromDate = new DateTime(evt.Start.Year, evt.Start.Month, evt.Start.Day);
                    var toDate = new DateTime(evt.End.Year, evt.End.Month, evt.End.Day);

                    if (!days.ContainsKey(fromDate))
                        days.Add(fromDate, new DateTime[] { evt.Start, evt.End });

                    if (!days.ContainsKey(toDate))
                        days.Add(toDate, new DateTime[] { evt.Start, evt.End });

                    if (fromDate == toDate)
                        days[fromDate][1] = evt.End;
                    else
                    {
                        var midnight = new DateTime(evt.Start.Year, evt.Start.Month, evt.Start.Day, 23, 59, 59);
                        days[fromDate][1] = midnight;
                        days[toDate][0] = evt.Start;
                        days[toDate][1] = evt.End;
                    }
                }

                bool bCheck = days.Keys.Count() > 1 ? false: true;
                var hh = 0.0;

                DateTime? prevDate = null;
                foreach (var key in days.Keys)
                {
                    if (prevDate != null)
                    {
                        TimeSpan ts = days[key][0] - prevDate.Value;
                        hh = ts.TotalHours;
                        if (hh >= minRest)
                            bCheck = true;
                    }

                    prevDate = days[key][1];
                }

                if (!bCheck)
                {
                    ValidationMessages.Add(new ValidationMessage()
                    {
                        EventId = 0,
                        Level = MessageLevel.Error,
                        //Message = $"Il dipendente [{context.EmployeeId}] non usufruisce del minimo n.di ore [{minRest}] settimanali."
                        Message = $"Il dipendente [{context.MasterData.EmployeeName}] non effettua il minimo n. ore di riposo [{minRest}] settimanali."
                    });
                }

                return bCheck;
            }
        }
    }
}
