namespace Hmi
{
    partial class frmProductionGraph
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnPage1 = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnStopsDuration = new System.Windows.Forms.Button();
            this.btnHourlyStopped = new System.Windows.Forms.Button();
            this.btnHourlyAvg_Click = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.cartesianChart1 = new LiveCharts.WinForms.CartesianChart();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnPage1
            // 
            this.btnPage1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPage1.BackColor = System.Drawing.Color.White;
            this.btnPage1.FlatAppearance.BorderSize = 0;
            this.btnPage1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPage1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPage1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(121)))), ((int)(((byte)(126)))));
            this.btnPage1.Location = new System.Drawing.Point(0, 169);
            this.btnPage1.Name = "btnPage1";
            this.btnPage1.Size = new System.Drawing.Size(284, 45);
            this.btnPage1.TabIndex = 2;
            this.btnPage1.Text = "Velocidade da linha";
            this.btnPage1.UseVisualStyleBackColor = false;
            this.btnPage1.Click += new System.EventHandler(this.btnSpeedChart_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnStopsDuration);
            this.panel1.Controls.Add(this.btnHourlyStopped);
            this.panel1.Controls.Add(this.btnHourlyAvg_Click);
            this.panel1.Controls.Add(this.btnPage1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(282, 896);
            this.panel1.TabIndex = 3;
            // 
            // btnStopsDuration
            // 
            this.btnStopsDuration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnStopsDuration.BackColor = System.Drawing.Color.White;
            this.btnStopsDuration.FlatAppearance.BorderSize = 0;
            this.btnStopsDuration.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopsDuration.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStopsDuration.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(121)))), ((int)(((byte)(126)))));
            this.btnStopsDuration.Location = new System.Drawing.Point(0, 322);
            this.btnStopsDuration.Name = "btnStopsDuration";
            this.btnStopsDuration.Size = new System.Drawing.Size(284, 45);
            this.btnStopsDuration.TabIndex = 5;
            this.btnStopsDuration.Text = "Tempo de cada parada";
            this.btnStopsDuration.UseVisualStyleBackColor = false;
            this.btnStopsDuration.Click += new System.EventHandler(this.btnStopsDuration_Click);
            // 
            // btnHourlyStopped
            // 
            this.btnHourlyStopped.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnHourlyStopped.BackColor = System.Drawing.Color.White;
            this.btnHourlyStopped.FlatAppearance.BorderSize = 0;
            this.btnHourlyStopped.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHourlyStopped.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHourlyStopped.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(121)))), ((int)(((byte)(126)))));
            this.btnHourlyStopped.Location = new System.Drawing.Point(0, 271);
            this.btnHourlyStopped.Name = "btnHourlyStopped";
            this.btnHourlyStopped.Size = new System.Drawing.Size(284, 45);
            this.btnHourlyStopped.TabIndex = 4;
            this.btnHourlyStopped.Text = "Tempo de parada H/H";
            this.btnHourlyStopped.UseVisualStyleBackColor = false;
            this.btnHourlyStopped.Click += new System.EventHandler(this.btnHourlyStopped_Click);
            // 
            // btnHourlyAvg_Click
            // 
            this.btnHourlyAvg_Click.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnHourlyAvg_Click.BackColor = System.Drawing.Color.White;
            this.btnHourlyAvg_Click.FlatAppearance.BorderSize = 0;
            this.btnHourlyAvg_Click.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHourlyAvg_Click.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHourlyAvg_Click.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(121)))), ((int)(((byte)(126)))));
            this.btnHourlyAvg_Click.Location = new System.Drawing.Point(0, 220);
            this.btnHourlyAvg_Click.Name = "btnHourlyAvg_Click";
            this.btnHourlyAvg_Click.Size = new System.Drawing.Size(284, 45);
            this.btnHourlyAvg_Click.TabIndex = 3;
            this.btnHourlyAvg_Click.Text = "Velocidade media H/H";
            this.btnHourlyAvg_Click.UseVisualStyleBackColor = false;
            this.btnHourlyAvg_Click.Click += new System.EventHandler(this.btnHourlyAvg_Click_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.lblTitle);
            this.panel2.Controls.Add(this.cartesianChart1);
            this.panel2.Controls.Add(this.dateTimePicker1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(282, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1638, 896);
            this.panel2.TabIndex = 4;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblTitle.Location = new System.Drawing.Point(640, 28);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(336, 39);
            this.lblTitle.TabIndex = 4;
            this.lblTitle.Text = "Velocidade da linha";
            this.lblTitle.Click += new System.EventHandler(this.label1_Click);
            // 
            // cartesianChart1
            // 
            this.cartesianChart1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cartesianChart1.Location = new System.Drawing.Point(25, 148);
            this.cartesianChart1.Name = "cartesianChart1";
            this.cartesianChart1.Size = new System.Drawing.Size(1588, 711);
            this.cartesianChart1.TabIndex = 3;
            this.cartesianChart1.Text = "cartesianChart1";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker1.Location = new System.Drawing.Point(731, 81);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(164, 26);
            this.dateTimePicker1.TabIndex = 2;
            // 
            // frmProductionGraph
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(248)))));
            this.ClientSize = new System.Drawing.Size(1920, 896);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmProductionGraph";
            this.Text = "frmParameter";
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnPage1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lblTitle;
        private LiveCharts.WinForms.CartesianChart cartesianChart1;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.Button btnHourlyAvg_Click;
        private System.Windows.Forms.Button btnHourlyStopped;
        private System.Windows.Forms.Button btnStopsDuration;
    }
}