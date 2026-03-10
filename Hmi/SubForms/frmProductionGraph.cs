using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapper;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Media;

namespace Hmi
{
    public partial class frmProductionGraph : Form
    {
        // =========================================================================================
        //  CONFIG / STATE
        // =========================================================================================
        public static frmProductionGraph instance;

        private readonly string _cnString =
            ConfigurationManager.ConnectionStrings["cn"]?.ConnectionString;

        private readonly System.Windows.Forms.Timer _debounceTimer;
        private DateTime _pendingDate;

        private CancellationTokenSource _lineLoadCts;

        // Reduce X labels (1 point/min => 1440 points/day)
        private const int XLabelEveryMinutes = 10;

        // =========================================================================================
        //  LIVECHARTS CACHED SERIES (LINE MODE)
        // =========================================================================================
        private bool _lineObjectsCreated;

        private SeriesCollection _lineSeriesCollection;
        private LineSeries _speedSeries;
        private LineSeries _avgSeries;
        private LineSeries _maxSeries;

        // =========================================================================================
        //  CTOR
        // =========================================================================================
        public frmProductionGraph()
        {
            InitializeComponent();
            instance = this;

            // Debounce
            _debounceTimer = new System.Windows.Forms.Timer { Interval = 350 };
            _debounceTimer.Tick += DebounceTimer_Tick;

            // Performance defaults
            if (cartesianChart1 != null)
            {
                cartesianChart1.Zoom = ZoomingOptions.X;
                cartesianChart1.DisableAnimations = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CancelLineLoad();
            base.OnFormClosing(e);
        }

        // =========================================================================================
        //  FORM EVENTS
        // =========================================================================================
        private void frmProductionGraph_Load(object sender, EventArgs e)
        {
            ExecuteTask(LoadAndRenderLineChartAsync(DateTime.Now));
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            _pendingDate = dateTimePicker1.Value;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void DebounceTimer_Tick(object sender, EventArgs e)
        {
            _debounceTimer.Stop();
            ExecuteTask(LoadAndRenderLineChartAsync(_pendingDate));
        }

        // =========================================================================================
        //  BUTTON EVENTS (wire these in Designer)
        // =========================================================================================

        // IMPORTANT: ensure the button is wired to THIS handler
        private void btnSpeedChart_Click(object sender, EventArgs e)
            => ExecuteTask(LoadAndRenderLineChartAsync(dateTimePicker1.Value));

        private void btnHourlyAvg_Click_Click(object sender, EventArgs e)
            => ExecuteTask(LoadHourlyAverageSpeedBarChartAsync(dateTimePicker1.Value));

        private void btnHourlyStopped_Click(object sender, EventArgs e)
            => ExecuteTask(LoadHourlyStoppedMinutesBarChartAsync(dateTimePicker1.Value));

        private void btnStopsDuration_Click(object sender, EventArgs e)
            => ExecuteTask(LoadStopDurationsBarChartAsync(dateTimePicker1.Value));

        // =========================================================================================
        //  TASK HELPER (your style)
        // =========================================================================================
        private void ExecuteTask(Task task)
        {
            if (task == null) return;

            task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    this.BeginInvoke(new Action(() =>
                        MessageBox.Show(t.Exception.GetBaseException().Message, "Erro",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        // =========================================================================================
        //  SAFETY / INIT GUARDS
        // =========================================================================================
        private void EnsureUiReady()
        {
            if (cartesianChart1 == null)
                throw new NullReferenceException("cartesianChart1 is null. Check designer control name / InitializeComponent().");

            if (string.IsNullOrWhiteSpace(_cnString))
                throw new NullReferenceException("Connection string 'cn' not found/empty in app.config.");

            EnsureLineChartSeriesCreated();
        }

        private void EnsureLineChartSeriesCreated()
        {
            if (_lineObjectsCreated) return;

            _speedSeries = new LineSeries
            {
                Title = "Velocidade da linha",
                PointGeometry = null,
                LineSmoothness = 0,
                Fill = null
            };

            _avgSeries = new LineSeries
            {
                Title = "Média",
                PointGeometry = null,
                LineSmoothness = 0,
                Fill = null,
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };

            _maxSeries = new LineSeries
            {
                Title = "Máximo",
                PointGeometry = null,
                LineSmoothness = 0,
                Fill = null,
                Stroke = Brushes.Blue,
                StrokeThickness = 2
            };

            _lineSeriesCollection = new SeriesCollection { _speedSeries, _avgSeries, _maxSeries };
            _lineObjectsCreated = true;
        }

        // =========================================================================================
        //  UI LOADING
        // =========================================================================================
        private void SetUiLoading(bool isLoading)
        {
            if (dateTimePicker1 != null) dateTimePicker1.Enabled = !isLoading;
            Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;
        }

        // =========================================================================================
        //  DATA ACCESS
        // =========================================================================================
        private List<MachineSpeedRecord> GetMachineSpeedRecords(DateTime day)
        {
            DateTime d0 = day.Date;
            DateTime d1 = d0.AddDays(1);

            using (IDbConnection db = new SqlConnection(_cnString))
            {
                db.Open();

                const string sql = @"
SELECT Date_Time, Machine_Speed
FROM dbo.MachineSpeedRecords
WHERE Date_Time >= @Day AND Date_Time < @NextDay
ORDER BY Date_Time ASC;";

                return db.Query<MachineSpeedRecord>(sql, new { Day = d0, NextDay = d1 }).ToList();
            }
        }

        // =========================================================================================
        //  IMPORTANT LIVECHARTS WORKAROUND
        //  - Do NOT call AxisX.Clear()/AxisY.Clear()
        //  - Replace the AxesCollection with NEW Axis objects when switching charts
        // =========================================================================================
        private void ApplyAxesForLineChart(string[] labels)
        {
            cartesianChart1.AxisX = new AxesCollection
            {
                new Axis
                {
                    Title = "Hora",
                    FontSize = 14,
                    Labels = labels ?? Array.Empty<string>(),
                    Separator = new Separator()
                }
            };

            cartesianChart1.AxisY = new AxesCollection
            {
                new Axis { Title = "m/min" }
            };
        }

        private void ApplyAxesForBarChart(string titleX, string[] labelsX, string titleY)
        {
            cartesianChart1.AxisX = new AxesCollection
            {
                new Axis
                {
                    Title = titleX ?? "",
                    Labels = labelsX ?? Array.Empty<string>(),
                    Separator = new Separator()
                }
            };

            cartesianChart1.AxisY = new AxesCollection
            {
                new Axis { Title = titleY ?? "" }
            };
        }

        // =========================================================================================
        //  LINE CHART (SPEED / AVG / MAX)
        // =========================================================================================
        private void CancelLineLoad()
        {
            try
            {
                _lineLoadCts?.Cancel();
                _lineLoadCts?.Dispose();
            }
            catch { /* ignore */ }
            finally
            {
                _lineLoadCts = null;
            }
        }

        private async Task LoadAndRenderLineChartAsync(DateTime selectedDate)
        {
            lblTitle.Text = "Velocidade da linha";

            try
            {
                EnsureUiReady();

                CancelLineLoad();
                _lineLoadCts = new CancellationTokenSource();
                var token = _lineLoadCts.Token;

                SetUiLoading(true);

                ChartPayload payload = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    var records = GetMachineSpeedRecords(selectedDate.Date);

                    token.ThrowIfCancellationRequested();

                    return BuildLineChartPayload(records);
                }, token);

                token.ThrowIfCancellationRequested();

                ApplyLineChartPayload(payload);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erro ao carregar gráfico",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUiLoading(false);
            }
        }

        private ChartPayload BuildLineChartPayload(List<MachineSpeedRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                return new ChartPayload
                {
                    SpeedValues = new ChartValues<int>(),
                    AvgValues = new ChartValues<double>(),
                    MaxValues = new ChartValues<int>(),
                    Labels = Array.Empty<string>()
                };
            }

            int count = records.Count;
            double avg = records.Average(r => r.Machine_Speed);
            int max = records.Max(r => r.Machine_Speed);

            var labels = new string[count];
            for (int i = 0; i < count; i++)
            {
                var t = records[i].Date_Time;
                labels[i] = (t.Minute % XLabelEveryMinutes == 0) ? t.ToString("HH:mm") : "";
            }

            return new ChartPayload
            {
                SpeedValues = new ChartValues<int>(records.Select(r => r.Machine_Speed)),
                AvgValues = new ChartValues<double>(Enumerable.Repeat(Math.Floor(avg), count)),
                MaxValues = new ChartValues<int>(Enumerable.Repeat(max, count)),
                Labels = labels
            };
        }

        private void ApplyLineChartPayload(ChartPayload payload)
        {
            // Restore line series
            cartesianChart1.Series = _lineSeriesCollection;

            // Apply axes WITHOUT Clear()
            ApplyAxesForLineChart(payload.Labels);

            // Apply values
            _speedSeries.Values = payload.SpeedValues;
            _avgSeries.Values = payload.AvgValues;
            _maxSeries.Values = payload.MaxValues;

            // Force refresh (helps after switching from bar charts)
            cartesianChart1.Update(true, true);
        }

        // =========================================================================================
        //  BAR CHART 1: HOURLY AVERAGE SPEED
        // =========================================================================================
        private async Task LoadHourlyAverageSpeedBarChartAsync(DateTime selectedDate)
        {
            lblTitle.Text = "Velocidade média por hora (m/min)";

            try
            {
                EnsureUiReady();

                DateTime day = selectedDate.Date;
                var rows = await Task.Run(() => GetMachineSpeedRecords(day));

                double[] avgPerHour = new double[24];
                var grouped = rows.GroupBy(r => r.Date_Time.Hour)
                                  .ToDictionary(g => g.Key, g => g.Average(x => (double)x.Machine_Speed));

                for (int h = 0; h < 24; h++)
                    avgPerHour[h] = grouped.TryGetValue(h, out double avg) ? avg : 0.0;

                cartesianChart1.Series = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Velocidade média (m/min):",
                        Values = new ChartValues<double>(avgPerHour)
                    }
                };

                ApplyAxesForBarChart("Hora do dia",
                    Enumerable.Range(0, 24).Select(h => $"{h:00}:00").ToArray(),
                    "m/min");

                cartesianChart1.Update(true, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================================
        //  BAR CHART 2: STOPPED MINUTES PER HOUR
        // =========================================================================================
        private async Task LoadHourlyStoppedMinutesBarChartAsync(DateTime selectedDate)
        {
            lblTitle.Text = "Tempo de máquina parada por hora";


            const int StopSpeedThreshold = 10;

            try
            {
                EnsureUiReady();

                DateTime day = selectedDate.Date;
                var rows = await Task.Run(() => GetMachineSpeedRecords(day));

                int[] stoppedMinutesPerHour = new int[24];

                var grouped = rows.Where(r => r.Machine_Speed < StopSpeedThreshold)
                                  .GroupBy(r => r.Date_Time.Hour)
                                  .ToDictionary(g => g.Key, g => g.Count()); // 1 record = 1 minute

                for (int h = 0; h < 24; h++)
                    stoppedMinutesPerHour[h] = grouped.TryGetValue(h, out int mins) ? mins : 0;

                cartesianChart1.Series = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = $"Minutos parados:",
                        Values = new ChartValues<int>(stoppedMinutesPerHour)
                    }
                };

                ApplyAxesForBarChart("Hora do dia",
                    Enumerable.Range(0, 24).Select(h => $"{h:00}:00").ToArray(),
                    "Minutos");

                cartesianChart1.Update(true, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================================
        //  BAR CHART 3: STOP DURATIONS + TOOLTIP
        // =========================================================================================
        private async Task LoadStopDurationsBarChartAsync(DateTime selectedDate)
        {
            lblTitle.Text = "Paradas: quantidade e duração";

            const int StopSpeedThreshold = 10;
            const int MaxStopsToShow = 60;

            try
            {
                EnsureUiReady();

                var rows = await Task.Run(() => GetMachineSpeedRecords(selectedDate.Date));
                if (rows == null || rows.Count == 0)
                {
                    cartesianChart1.Series = new SeriesCollection();
                    ApplyAxesForBarChart("Paradas", Array.Empty<string>(), "Minutos");
                    cartesianChart1.Update(true, true);
                    return;
                }

                var stops = BuildStopEvents(rows, StopSpeedThreshold);

                if (MaxStopsToShow > 0 && stops.Count > MaxStopsToShow)
                {
                    stops = stops
                        .OrderByDescending(s => s.DurationMinutes)
                        .Take(MaxStopsToShow)
                        .OrderBy(s => s.Start)
                        .ToList();
                }

                RenderStopDurationChart(stops, StopSpeedThreshold);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<StopEvent> BuildStopEvents(List<MachineSpeedRecord> rows, int threshold)
        {
            var stops = new List<StopEvent>();

            bool inStop = false;
            DateTime start = default;
            DateTime end = default;
            int count = 0;

            foreach (var r in rows)
            {
                bool stopped = r.Machine_Speed < threshold;

                if (stopped)
                {
                    if (!inStop)
                    {
                        inStop = true;
                        start = r.Date_Time;
                        count = 1;
                    }
                    else
                    {
                        count++;
                    }

                    end = r.Date_Time;
                }
                else
                {
                    if (inStop)
                    {
                        stops.Add(new StopEvent { Start = start, End = end, DurationMinutes = count });
                        inStop = false;
                        count = 0;
                    }
                }
            }

            if (inStop)
                stops.Add(new StopEvent { Start = start, End = end, DurationMinutes = count });

            return stops;
        }

        private void RenderStopDurationChart(List<StopEvent> stops, int threshold)
        {
            // Empty chart but keep axes stable
            if (stops == null) stops = new List<StopEvent>();

            var values = new ChartValues<int>(stops.Select(s => s.DurationMinutes));
            var labels = stops.Select((s, idx) => $"Parada Nrº:{idx + 1:00}").ToArray();

            cartesianChart1.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = $"",
                    Values = values,
                    LabelPoint = cp =>
                    {
                        int i = (int)cp.X;
                        if (i < 0 || i >= stops.Count) return "";

                        var s = stops[i];
                        DateTime endDisplay = s.End.AddMinutes(1); // 1 record/min
                        return $"Inicio: {s.Start:HH:mm} \n"+
                        $"Fim: {endDisplay:HH:mm} \n" +
                        $"Duração: {s.DurationMinutes} min";

                    }
                }
            };

            ApplyAxesForBarChart("Paradas (ordem no dia)", labels, "Minutos");

            cartesianChart1.Update(true, true);
        }

        // =========================================================================================
        //  DTOs
        // =========================================================================================
        private sealed class ChartPayload
        {
            public ChartValues<int> SpeedValues { get; set; }
            public ChartValues<double> AvgValues { get; set; }
            public ChartValues<int> MaxValues { get; set; }
            public string[] Labels { get; set; }
        }

        private sealed class StopEvent
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public int DurationMinutes { get; set; }
        }

        public sealed class MachineSpeedRecord
        {
            public DateTime Date_Time { get; set; }
            public int Machine_Speed { get; set; }
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
