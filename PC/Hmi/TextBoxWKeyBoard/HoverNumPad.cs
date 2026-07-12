using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;


namespace TextBoxWKeyBoard
{
    class HoverNumPad : Form
    {

        public HoverNumPad()
        {
            this.Load += new EventHandler(HoverKeyboard_Load);

            this.DoubleBuffered = true;
            this.SuspendLayout();
            int x = 0;
            int y = 0;
            //foreach (string line in new string[] {
            //    "789", "456", "123", "0."})
            //{
            //    foreach (char cur in line)
            //    {
            //        Button button = new Button();
            //        button.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //        button.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(95)))), ((int)(((byte)(135)))));
            //        button.ForeColor = System.Drawing.Color.White;
            //        button.FlatAppearance.BorderSize = 0;
            //        button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            //        button.Location = new Point(x * 57, y * 37);
            //        button.Size = new Size(55, 35);
            //        button.Text = cur.ToString();
            //        button.Click += new EventHandler(Button_Click);
            //        Controls.Add(button);
            //        x++;
            //    }
            //    x = 0;
            //    y++;
            //}






            ////Botao clear
            //Button buttonCLR = new Button();
            //buttonCLR.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //buttonCLR.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(95)))), ((int)(((byte)(135)))));
            //buttonCLR.ForeColor = System.Drawing.Color.White;
            //buttonCLR.FlatAppearance.BorderSize = 0;
            //buttonCLR.FlatStyle = System.Windows.Forms.FlatStyle.Flat;

            //buttonCLR.Location = new Point(114, 111);
            //buttonCLR.Size = new Size(55, 35);
            //buttonCLR.Text = "CLR";
            //buttonCLR.Click += new EventHandler(Button_Click_Clr);
            //Controls.Add(buttonCLR);

            ////Botao Enter
            //Button buttonEnter = new Button();
            //buttonEnter.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //buttonEnter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(95)))), ((int)(((byte)(135)))));
            //buttonEnter.ForeColor = System.Drawing.Color.White;
            //buttonEnter.FlatAppearance.BorderSize = 0;
            //buttonEnter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;

            //buttonEnter.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //buttonEnter.Location = new Point(0, 148);
            //buttonEnter.Size = new Size(167, 35);
            //buttonEnter.Text = "Confirmar";
            //buttonEnter.Click += new EventHandler(Button_Click_Enter);
            //Controls.Add(buttonEnter);

            ////Botao Escape
            //Button buttonEscape = new Button();
            //buttonEscape.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //buttonEscape.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(95)))), ((int)(((byte)(135)))));
            //buttonEscape.ForeColor = System.Drawing.Color.White;
            //buttonEscape.FlatAppearance.BorderSize = 0;
            //buttonEscape.FlatStyle = System.Windows.Forms.FlatStyle.Flat;

            //buttonEscape.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //buttonEscape.Location = new Point(0, 185);
            //buttonEscape.Size = new Size(167, 35); //X - Y
            //buttonEscape.Text = "Fechar";
            //buttonEscape.Click += new EventHandler(Button_Click_Escape);
            //Controls.Add(buttonEscape);


            //this.ClientSize = new Size(55 * 3, 33 * 6);
            this.ClientSize = new Size( 194, 266);

            TopMost = true;

            InitializeComponent();
           this.ResumeLayout(true);
        }

        void HoverKeyboard_Load(object sender, EventArgs e)
        {
            Application.AddMessageFilter(new PopupWindowHelperMessageFilter(this, _textboxr));
        }

        private Button btn_1;
        private Button btn_2;
        private Button btn_3;
        private Button btn_6;
        private Button btn_5;
        private Button btn_4;
        private Button btn_9;
        private Button btn_8;
        private Button btn_7;
        private Button btn_0;
        private Button btn_Dec;
        private Button btn_CLR;
        private Button btn_Enter;
        private Button Close;
        private Control _textboxr;

        public Control TextBox
        {
            get
            {
                return _textboxr;
            }
            set
            {
                _textboxr = value;
            }
        }

        void Button_Click(object sender, EventArgs e)
        {
            SendKeys.Send(((Button)sender).Text);
        }

        void Button_Click_Clr(object sender, EventArgs e)
        {
            SendKeys.Send("{BACKSPACE}");
        }

        void Button_Click_Enter(object sender, EventArgs e)
        {
            SendKeys.Send("{ENTER}");
            this.Close();
        }

        void Button_Click_Escape(object sender, EventArgs e)
        {
            SendKeys.Send("{ESCAPE}");
            this.Close();
        }

        void Button_Click_Dec(object sender, EventArgs e)
        {
            SendKeys.Send(".");
            //SendKeys.Send(((Button)sender).Text);

        }

        private void btn_Minus_Click(object sender, EventArgs e)
        {
            SendKeys.Send("-");
            //SendKeys.Send(((Button)sender).Text);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams ret = base.CreateParams;
                ret.Style = (int)Flags.WindowStyles.WS_THICKFRAME | (int)Flags.WindowStyles.WS_CHILD;
                ret.ExStyle |= (int)Flags.WindowStyles.WS_EX_NOACTIVATE | (int)Flags.WindowStyles.WS_EX_TOOLWINDOW;
                ret.X = this.Location.X;
                ret.Y = this.Location.Y;
                return ret;
            }
        }

        private void InitializeComponent()
        {
            this.btn_1 = new System.Windows.Forms.Button();
            this.btn_2 = new System.Windows.Forms.Button();
            this.btn_3 = new System.Windows.Forms.Button();
            this.btn_6 = new System.Windows.Forms.Button();
            this.btn_5 = new System.Windows.Forms.Button();
            this.btn_4 = new System.Windows.Forms.Button();
            this.btn_9 = new System.Windows.Forms.Button();
            this.btn_8 = new System.Windows.Forms.Button();
            this.btn_7 = new System.Windows.Forms.Button();
            this.btn_0 = new System.Windows.Forms.Button();
            this.btn_Dec = new System.Windows.Forms.Button();
            this.btn_CLR = new System.Windows.Forms.Button();
            this.btn_Enter = new System.Windows.Forms.Button();
            this.Close = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_1
            // 
            this.btn_1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_1.FlatAppearance.BorderSize = 0;
            this.btn_1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_1.Location = new System.Drawing.Point(9, 8);
            this.btn_1.Margin = new System.Windows.Forms.Padding(0);
            this.btn_1.Name = "btn_1";
            this.btn_1.Size = new System.Drawing.Size(58, 51);
            this.btn_1.TabIndex = 2;
            this.btn_1.Text = "1";
            this.btn_1.UseVisualStyleBackColor = false;
            this.btn_1.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_2
            // 
            this.btn_2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_2.FlatAppearance.BorderSize = 0;
            this.btn_2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_2.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_2.Location = new System.Drawing.Point(68, 8);
            this.btn_2.Margin = new System.Windows.Forms.Padding(0);
            this.btn_2.Name = "btn_2";
            this.btn_2.Size = new System.Drawing.Size(58, 51);
            this.btn_2.TabIndex = 3;
            this.btn_2.Text = "2";
            this.btn_2.UseVisualStyleBackColor = false;
            this.btn_2.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_3
            // 
            this.btn_3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_3.FlatAppearance.BorderSize = 0;
            this.btn_3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_3.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_3.Location = new System.Drawing.Point(127, 8);
            this.btn_3.Margin = new System.Windows.Forms.Padding(0);
            this.btn_3.Name = "btn_3";
            this.btn_3.Size = new System.Drawing.Size(58, 51);
            this.btn_3.TabIndex = 3;
            this.btn_3.Text = "3";
            this.btn_3.UseVisualStyleBackColor = false;
            this.btn_3.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_6
            // 
            this.btn_6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_6.FlatAppearance.BorderSize = 0;
            this.btn_6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_6.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_6.Location = new System.Drawing.Point(127, 60);
            this.btn_6.Margin = new System.Windows.Forms.Padding(0);
            this.btn_6.Name = "btn_6";
            this.btn_6.Size = new System.Drawing.Size(58, 51);
            this.btn_6.TabIndex = 6;
            this.btn_6.Text = "6";
            this.btn_6.UseVisualStyleBackColor = false;
            this.btn_6.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_5
            // 
            this.btn_5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_5.FlatAppearance.BorderSize = 0;
            this.btn_5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_5.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_5.Location = new System.Drawing.Point(68, 60);
            this.btn_5.Margin = new System.Windows.Forms.Padding(0);
            this.btn_5.Name = "btn_5";
            this.btn_5.Size = new System.Drawing.Size(58, 51);
            this.btn_5.TabIndex = 5;
            this.btn_5.Text = "5";
            this.btn_5.UseVisualStyleBackColor = false;
            this.btn_5.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_4
            // 
            this.btn_4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_4.FlatAppearance.BorderSize = 0;
            this.btn_4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_4.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_4.Location = new System.Drawing.Point(9, 60);
            this.btn_4.Margin = new System.Windows.Forms.Padding(0);
            this.btn_4.Name = "btn_4";
            this.btn_4.Size = new System.Drawing.Size(58, 51);
            this.btn_4.TabIndex = 4;
            this.btn_4.Text = "4";
            this.btn_4.UseVisualStyleBackColor = false;
            this.btn_4.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_9
            // 
            this.btn_9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_9.FlatAppearance.BorderSize = 0;
            this.btn_9.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_9.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_9.Location = new System.Drawing.Point(127, 112);
            this.btn_9.Margin = new System.Windows.Forms.Padding(0);
            this.btn_9.Name = "btn_9";
            this.btn_9.Size = new System.Drawing.Size(58, 51);
            this.btn_9.TabIndex = 9;
            this.btn_9.Text = "9";
            this.btn_9.UseVisualStyleBackColor = false;
            this.btn_9.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_8
            // 
            this.btn_8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_8.FlatAppearance.BorderSize = 0;
            this.btn_8.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_8.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_8.Location = new System.Drawing.Point(68, 112);
            this.btn_8.Margin = new System.Windows.Forms.Padding(0);
            this.btn_8.Name = "btn_8";
            this.btn_8.Size = new System.Drawing.Size(58, 51);
            this.btn_8.TabIndex = 8;
            this.btn_8.Text = "8";
            this.btn_8.UseVisualStyleBackColor = false;
            this.btn_8.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_7
            // 
            this.btn_7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_7.FlatAppearance.BorderSize = 0;
            this.btn_7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_7.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_7.Location = new System.Drawing.Point(9, 112);
            this.btn_7.Margin = new System.Windows.Forms.Padding(0);
            this.btn_7.Name = "btn_7";
            this.btn_7.Size = new System.Drawing.Size(58, 51);
            this.btn_7.TabIndex = 7;
            this.btn_7.Text = "7";
            this.btn_7.UseVisualStyleBackColor = false;
            this.btn_7.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_0
            // 
            this.btn_0.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_0.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_0.FlatAppearance.BorderSize = 0;
            this.btn_0.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_0.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_0.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_0.Location = new System.Drawing.Point(9, 164);
            this.btn_0.Margin = new System.Windows.Forms.Padding(0);
            this.btn_0.Name = "btn_0";
            this.btn_0.Size = new System.Drawing.Size(58, 51);
            this.btn_0.TabIndex = 7;
            this.btn_0.Text = "0";
            this.btn_0.UseVisualStyleBackColor = false;
            this.btn_0.Click += new System.EventHandler(this.Button_Click);
            // 
            // btn_Dec
            // 
            this.btn_Dec.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_Dec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_Dec.FlatAppearance.BorderSize = 0;
            this.btn_Dec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Dec.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Dec.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_Dec.Location = new System.Drawing.Point(68, 164);
            this.btn_Dec.Margin = new System.Windows.Forms.Padding(0);
            this.btn_Dec.Name = "btn_Dec";
            this.btn_Dec.Size = new System.Drawing.Size(58, 51);
            this.btn_Dec.TabIndex = 8;
            this.btn_Dec.Text = ".";
            this.btn_Dec.UseVisualStyleBackColor = false;
            this.btn_Dec.Click += new System.EventHandler(this.Button_Click_Dec);
            // 
            // btn_CLR
            // 
            this.btn_CLR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_CLR.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_CLR.FlatAppearance.BorderSize = 0;
            this.btn_CLR.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_CLR.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_CLR.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_CLR.Location = new System.Drawing.Point(127, 164);
            this.btn_CLR.Margin = new System.Windows.Forms.Padding(0);
            this.btn_CLR.Name = "btn_CLR";
            this.btn_CLR.Size = new System.Drawing.Size(58, 51);
            this.btn_CLR.TabIndex = 9;
            this.btn_CLR.Text = "CLR";
            this.btn_CLR.UseVisualStyleBackColor = false;
            this.btn_CLR.Click += new System.EventHandler(this.Button_Click_Clr);
            // 
            // btn_Enter
            // 
            this.btn_Enter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_Enter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btn_Enter.FlatAppearance.BorderSize = 0;
            this.btn_Enter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Enter.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Enter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.btn_Enter.Location = new System.Drawing.Point(9, 216);
            this.btn_Enter.Margin = new System.Windows.Forms.Padding(0);
            this.btn_Enter.Name = "btn_Enter";
            this.btn_Enter.Size = new System.Drawing.Size(117, 51);
            this.btn_Enter.TabIndex = 9;
            this.btn_Enter.Text = "Enter";
            this.btn_Enter.UseVisualStyleBackColor = false;
            this.btn_Enter.Click += new System.EventHandler(this.Button_Click_Enter);
            // 
            // Close
            // 
            this.Close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Close.BackColor = System.Drawing.Color.Maroon;
            this.Close.FlatAppearance.BorderSize = 0;
            this.Close.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Close.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Close.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(234)))), ((int)(((byte)(227)))));
            this.Close.Location = new System.Drawing.Point(127, 216);
            this.Close.Margin = new System.Windows.Forms.Padding(0);
            this.Close.Name = "Close";
            this.Close.Size = new System.Drawing.Size(58, 51);
            this.Close.TabIndex = 9;
            this.Close.Text = "X";
            this.Close.UseVisualStyleBackColor = false;
            this.Close.Click += new System.EventHandler(this.Button_Click_Escape);
            // 
            // HoverNumPad
            // 
            this.ClientSize = new System.Drawing.Size(209, 274);
            this.Controls.Add(this.btn_Enter);
            this.Controls.Add(this.Close);
            this.Controls.Add(this.btn_CLR);
            this.Controls.Add(this.btn_Dec);
            this.Controls.Add(this.btn_9);
            this.Controls.Add(this.btn_0);
            this.Controls.Add(this.btn_8);
            this.Controls.Add(this.btn_7);
            this.Controls.Add(this.btn_6);
            this.Controls.Add(this.btn_5);
            this.Controls.Add(this.btn_4);
            this.Controls.Add(this.btn_3);
            this.Controls.Add(this.btn_2);
            this.Controls.Add(this.btn_1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "HoverNumPad";
            this.ResumeLayout(false);

        }


    }
}
