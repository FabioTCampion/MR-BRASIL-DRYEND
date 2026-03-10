using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Windows.Forms;

namespace Hmi.Data
{
    class MachineNotRunningTimePerHour
    {
        public int ID { get; set; }
        public DateTime Date_Time { get; set; }
        public int Hour_0 { get; set; }
        public int Hour_1 { get; set; }
        public int Hour_2 { get; set; }
        public int Hour_3 { get; set; }
        public int Hour_4 { get; set; }
        public int Hour_5 { get; set; }
        public int Hour_6 { get; set; }
        public int Hour_7 { get; set; }
        public int Hour_8 { get; set; }
        public int Hour_9 { get; set; }
        public int Hour_10 { get; set; }
        public int Hour_11 { get; set; }
        public int Hour_12 { get; set; }
        public int Hour_13 { get; set; }
        public int Hour_14 { get; set; }
        public int Hour_15 { get; set; }
        public int Hour_16 { get; set; }
        public int Hour_17 { get; set; }
        public int Hour_18 { get; set; }
        public int Hour_19 { get; set; }
        public int Hour_20 { get; set; }
        public int Hour_21 { get; set; }
        public int Hour_22 { get; set; }
        public int Hour_23 { get; set; }

        public void LogMachineNotRunningTimePerHour()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        db.Open();

                        string query = @"INSERT INTO MachineStopedTimePerHour 
                                        (Date_Time, Hour_0, Hour_1, Hour_2, Hour_3, Hour_4, Hour_5, Hour_6, Hour_7, Hour_8, 
                                         Hour_9, Hour_10, Hour_11, Hour_12, Hour_13, Hour_14, Hour_15, Hour_16, Hour_17, 
                                         Hour_18, Hour_19, Hour_20, Hour_21, Hour_22, Hour_23)
                                        VALUES 
                                        (@Date_Time, @Hour_0, @Hour_1, @Hour_2, @Hour_3, @Hour_4, @Hour_5, @Hour_6, @Hour_7, @Hour_8, 
                                         @Hour_9, @Hour_10, @Hour_11, @Hour_12, @Hour_13, @Hour_14, @Hour_15, @Hour_16, @Hour_17, 
                                         @Hour_18, @Hour_19, @Hour_20, @Hour_21, @Hour_22, @Hour_23)";

                        db.Execute(query, new
                        {
                            Date_Time = DateTime.Now,
                            Hour_0 = 00,
                            Hour_1 = this.Hour_1,
                            Hour_2 = this.Hour_2,
                            Hour_3 = this.Hour_3,
                            Hour_4 = this.Hour_4,
                            Hour_5 = this.Hour_5,
                            Hour_6 = this.Hour_6,
                            Hour_7 = this.Hour_7,
                            Hour_8 = this.Hour_8,
                            Hour_9 = this.Hour_9,
                            Hour_10 = this.Hour_10,
                            Hour_11 = this.Hour_11,
                            Hour_12 = this.Hour_12,
                            Hour_13 = this.Hour_13,
                            Hour_14 = this.Hour_14,
                            Hour_15 = this.Hour_15,
                            Hour_16 = this.Hour_16,
                            Hour_17 = this.Hour_17,
                            Hour_18 = this.Hour_18,
                            Hour_19 = this.Hour_19,
                            Hour_20 = this.Hour_20,
                            Hour_21 = this.Hour_21,
                            Hour_22 = this.Hour_22,
                            Hour_23 = this.Hour_23
                        });

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
