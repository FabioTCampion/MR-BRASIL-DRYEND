using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TCEVENTLOGGERLib;
using TcEventLogProxyLib;
using Dapper;
using Hmi.Data;
using LiveCharts;
using LiveCharts.Events;
using LiveCharts.Wpf;
using System.Data.SqlClient;
using System.Configuration;
// using callsigncharlie
namespace Hmi
{



    public partial class frmMachineSpeedGraph : Form
    {

        public static frmMachineSpeedGraph instance;



        public frmMachineSpeedGraph()
        {
            InitializeComponent();
            instance = this;

            // Assuming cartesianChart1 is your CartesianChart control
            cartesianChart1.Zoom = ZoomingOptions.X; // Enable zooming on the X-axis
            cartesianChart1.DisableAnimations = true; // Disable animations for smoother zooming
        }


        private List<MachineSpeedRecord> GetMachineSpeedRecords(DateTime selectedDate)
        {
            using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
            {
                if (db.State == ConnectionState.Closed)
                {
                    db.Open();

                    string query = "SELECT Date_Time, Machine_Speed FROM MachineSpeedRecords WHERE Date_Time >= @SelectedDate AND Date_Time < @NextDay";
                    var result = db.Query<MachineSpeedRecord>(query, new
                    {
                        SelectedDate = selectedDate.Date,
                        NextDay = selectedDate.Date.AddDays(1)
                    });

                    return result.ToList();
                }

                // If the database connection state is not closed, return an empty list or handle the situation accordingly.
                return new List<MachineSpeedRecord>();
            }
        }

        private void UpdateGraph(DateTime selectedDate)
        {
            try
            {
                var speedRecords = GetMachineSpeedRecords(selectedDate);

                // Assuming cartesianChart1 is a CartesianChart from LiveCharts
                cartesianChart1.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Velocidade da linha",
                    Values = new ChartValues<int>(speedRecords.Select(record => record.Machine_Speed)),
                    DataContext = this,
                // DataLabels = new ChartValuesl<String>(speedRecords.Select(record => record.Date_Time.ToString)),
                // PointGeometry =  // Optional: Use null to display only the line without points
            },
                new LineSeries
                {
                    Title = "Média",
                    Values = new ChartValues<double>(
                        Enumerable.Repeat(
                            Math.Floor(speedRecords.Average(record => record.Machine_Speed)), // Truncate to the integer part
                            speedRecords.Count
                        )
                    ),
                    PointGeometry = null, // Optional: Use null to display only the line without points
                    Stroke = System.Windows.Media.Brushes.Red, // Set the color of the average line
                    StrokeThickness = 2 // Set the thickness of the average line
                },
                new LineSeries
                {
                    Title = "Máximo",
                    Values = new ChartValues<int>(Enumerable.Repeat(speedRecords.Max(record => record.Machine_Speed), speedRecords.Count)),
                    PointGeometry = null, // Optional: Use null to display only the line without points
                    Stroke = System.Windows.Media.Brushes.Blue, // Set the color of the maximum speed line
                    StrokeThickness = 2 // Set the thickness of the maximum speed line
                }
            };

                // Configure the X-axis with scrollbar and increase label size
                cartesianChart1.AxisX.Clear();
                cartesianChart1.AxisX.Add(new Axis
                {
                    Title = "Data / Hora",
                    Labels = speedRecords.Select(record => record.Date_Time.ToString("HH:mm:ss")).ToArray(),
                    IsMerged = true, // This will allow the scrollbar to appear
                    Separator = new LiveCharts.Wpf.Separator(),
                    FontSize = 14, // Set the font size for the labels
                });

                // Optionally, you can customize the axes, labels, and other chart properties as needed.
            }
            catch (Exception ex)
            {
                // Handle the exception, you can show a message box or log the error
                MessageBox.Show($"Ocorreu um erro: {ex.Message}", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            // Call the method to update the graph when the DateTimePicker value changes
            UpdateGraph(dateTimePicker1.Value);
        }

        private void frmProductionGraph_Enter(object sender, EventArgs e)
        {
            UpdateGraph(DateTime.Now);
        }

        private void frmMachineSpeedGraph_Load(object sender, EventArgs e)
        {

        }
    }
}
