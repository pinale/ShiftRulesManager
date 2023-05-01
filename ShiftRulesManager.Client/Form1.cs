using ShiftRulesManager.BLL;
using System.Globalization;
using System.Text;

namespace ShiftRulesManager.FrontEnd
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region FORM EVENTS

        private void Form1_Load(object sender, EventArgs e)
        {
            dtpStart.Value = new DateTime(2023, 1, 2, 8, 0, 0, DateTimeKind.Utc);
            dtpEnd.Value = new DateTime(2023, 1, 9, 20, 0, 0, DateTimeKind.Utc);
            dtpTimeStart.Value = new DateTime(1900, 1, 1, 8, 0, 0);
            dtpTimeEnd.Value = new DateTime(1900, 1, 1, 20, 0, 0);
            txtPdvId.Text = "1";
            txtRepId.Text = "1";
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = @"C:\Devl Temp\ShiftRulesManager\DatiTest\",
                Title = "Browse CSV Files",

                CheckPathExists = true,

                DefaultExt = "csv",
                Filter = "Dati x Test (*.CSV;*.TXT)|*.CSV;*.TXT|All files (*.*)|*.*",
                FilterIndex = 1,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFileName.Text = openFileDialog1.FileName;
            }
        }

        private void btnCheckAll_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFileName.Text))
            {
                MessageBox.Show("Selezionare un file dati!");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPdvId.Text) || string.IsNullOrWhiteSpace(txtRepId.Text))
            {
                MessageBox.Show("PdV o Reparto non specificato!");
                return;
            }

            // imposta il periodo di analisi
            var refPeriod = new InputReferencePeriod()
            {
                StartPeriod = new DateTime(dtpStart.Value.Year,
                                            dtpStart.Value.Month,
                                            dtpStart.Value.Day,
                                            dtpTimeStart.Value.Hour,
                                            dtpTimeStart.Value.Minute,
                                            dtpTimeStart.Value.Second),           //  new DateTime(2023, 1, 2, 8, 0, 0),
                EndPeriod = new DateTime(dtpEnd.Value.Year,
                                            dtpEnd.Value.Month,
                                            dtpEnd.Value.Day,
                                            dtpTimeEnd.Value.Hour,
                                            dtpTimeEnd.Value.Minute,
                                            dtpTimeEnd.Value.Second),             // new DateTime(2023, 1, 9, 20, 0, 0),
                PuntoVenditaId = int.Parse(txtPdvId.Text),
                RepartoId = int.Parse(txtRepId.Text),
            };

            // crea gli eventi da monitorare
            var events = GetEventsFromFile();

            var empMasterData = GetEmployeesMasterData();

            // lancia l'esecuzione dei controlli
            PerformAllValidations(refPeriod, events, empMasterData);
        }

        #endregion

        #region SERVICE CALL

        /// <summary>
        /// Chiama la funzione che esegue le verifiche dei vincoli.
        /// </summary>
        /// <param name="refPeriod">A list of variables to extract the span from.</param>
        /// <param name="events">The start to the span.</param>
        /// <param name="employeesMasterData">The length of the span.</param>
        private void PerformAllValidations(InputReferencePeriod refPeriod, List<WorkShiftEvent> events, List<EmployeeMasterData> employeesMasterData)
        {
            txtResult.Text = "";

            // crea un'instanza della classe che implementa l'interfaccia
            IRulesValidation rulesValidation = new RulesValidation();

            // chiama la funzione che esegue le validazioni e riceve in risposta una lista di messaggi strutturati (livello + messaggio)
            var results = rulesValidation.GetAllValidationRules(refPeriod, events, employeesMasterData);
            
            var sb = new StringBuilder();
            foreach (var item in results)
            {
                sb.AppendLine("(" + item.Level.ToString() + ") " + item.Message);
                sb.AppendLine();
            }

            txtResult.Text = sb.ToString();
        }

        #endregion

        #region UTILS

        private List<WorkShiftEvent> GetEventsFromFile()
        {
            var events = new List<WorkShiftEvent>();

            var cultureInfo = new System.Globalization.CultureInfo("it-IT");

            var filePath = txtFileName.Text;
            if (File.Exists(filePath))
            {
                using (var sr = new StreamReader(File.OpenRead(filePath)))
                {
                    int id = 0;
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var values = line.Split(',');
                            events.Add(new WorkShiftEvent()
                            {
                                EventId = ++id,
                                Title = values[0].Replace("'","").Replace("null", ""),
                                Description = values[1].Replace("'", "").Replace("null",""),
                                Start = DateTime.ParseExact(values[2].Replace("'",""), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                                End = DateTime.ParseExact(values[3].Replace("'", ""), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                                dipendenteId = int.Parse(values[4]),
                                idPuntoVendita = int.Parse(values[5]),
                                idReparto = int.Parse(values[6]),
                                stato = values[7].Replace("'", "").Replace("null", "")
                            });
                        }
                    }
                }
            }
            else
                MessageBox.Show("File non trovato!");

            return events;
        }

        private List<WorkShiftEvent> CreateStaticEvents()
        {
            var events = new List<WorkShiftEvent>()
            {
                new WorkShiftEvent()
                {
                    EventId = 1,
                    Title = "Gastronomia 08:00-14:00",
                    Description = "",
                    Start = new DateTime(2023, 1, 2, 8, 0, 0),
                    End = new DateTime(2023, 1, 2, 14, 0, 0),
                    dipendenteId = 1,
                    idPuntoVendita = 1,
                    idReparto = 1,
                    stato = "bozza"
                },
                new WorkShiftEvent()
                {
                    EventId = 1,
                    Title = "Gastronomia 08:00-14:00",
                    Description = "",
                    Start = new DateTime(2023, 1, 2, 8, 0, 0),
                    End = new DateTime(2023, 1, 2, 14, 0, 0),
                    dipendenteId = 1,
                    idPuntoVendita = 1,
                    idReparto = 1,
                    stato = "bozza"
                },
            };

            return events;
        }

        private List<EmployeeMasterData> GetEmployeesMasterData()
        {
            var masterdata = new List<EmployeeMasterData>()
            {
                new EmployeeMasterData()
                {
                    EmployeeId = 1,
                    MaxDailyHours = 13,
                    MinDailyHours = 3,
                    MaxWeeklyHours = 40,
                    MinNbHoursBetweenShift = 11,
                    MinWeeklyRest = 24
                },
                new EmployeeMasterData()
                {
                    EmployeeId = 2,
                    MaxDailyHours = 5,
                    MinDailyHours = 3,
                    MaxWeeklyHours = 25,
                    MinNbHoursBetweenShift = 11,
                    MinWeeklyRest = 24
                }
            };

            return masterdata;
        }

        #endregion
    }
}