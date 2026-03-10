namespace Hmi
{
    partial class frmMain
    {
        /// <summary>
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">true se for necessário descartar os recursos gerenciados; caso contrário, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Windows Form Designer

        /// <summary>
        /// Método necessário para suporte ao Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.pContainer = new System.Windows.Forms.Panel();
            this.pMain = new System.Windows.Forms.Panel();
            this.pStatus = new System.Windows.Forms.Panel();
            this.pBottonMenu = new System.Windows.Forms.Panel();
            this.btnPage6 = new System.Windows.Forms.Button();
            this.btnPage5 = new System.Windows.Forms.Button();
            this.btnPage4 = new System.Windows.Forms.Button();
            this.btnPage1 = new System.Windows.Forms.Button();
            this.pInformation = new System.Windows.Forms.Panel();
            this.lblConnectionState = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox6 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.lbl_currentOrder_lineSpeed = new System.Windows.Forms.Label();
            this.lblDateTimeAct = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.lblDateTime = new System.Windows.Forms.Label();
            this.btnLogin = new System.Windows.Forms.Button();
            this.lblLoggedUser = new System.Windows.Forms.Label();
            this.lblUserName = new System.Windows.Forms.Label();
            this.imgUserLogin = new System.Windows.Forms.PictureBox();
            this.pTitle = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.tmrDateTime = new System.Windows.Forms.Timer(this.components);
            this.tmrUpdateNextOrder = new System.Windows.Forms.Timer(this.components);
            this.tmrLogMachineSpeed = new System.Windows.Forms.Timer(this.components);
            this.tmrUpdateInterface = new System.Windows.Forms.Timer(this.components);
            this.nextOrderBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.currentOrderBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.pContainer.SuspendLayout();
            this.pBottonMenu.SuspendLayout();
            this.pInformation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imgUserLogin)).BeginInit();
            this.pTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nextOrderBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.currentOrderBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // pContainer
            // 
            this.pContainer.Controls.Add(this.pMain);
            this.pContainer.Controls.Add(this.pStatus);
            this.pContainer.Controls.Add(this.pBottonMenu);
            this.pContainer.Controls.Add(this.pInformation);
            this.pContainer.Controls.Add(this.pTitle);
            this.pContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pContainer.Location = new System.Drawing.Point(0, 0);
            this.pContainer.Name = "pContainer";
            this.pContainer.Size = new System.Drawing.Size(1920, 1080);
            this.pContainer.TabIndex = 9;
            // 
            // pMain
            // 
            this.pMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(248)))));
            this.pMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pMain.Location = new System.Drawing.Point(0, 120);
            this.pMain.Name = "pMain";
            this.pMain.Size = new System.Drawing.Size(1920, 896);
            this.pMain.TabIndex = 15;
            this.pMain.Paint += new System.Windows.Forms.PaintEventHandler(this.pMain_Paint);
            // 
            // pStatus
            // 
            this.pStatus.BackColor = System.Drawing.Color.Lime;
            this.pStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.pStatus.Location = new System.Drawing.Point(0, 110);
            this.pStatus.Name = "pStatus";
            this.pStatus.Size = new System.Drawing.Size(1920, 10);
            this.pStatus.TabIndex = 16;
            // 
            // pBottonMenu
            // 
            this.pBottonMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.pBottonMenu.Controls.Add(this.btnPage6);
            this.pBottonMenu.Controls.Add(this.btnPage5);
            this.pBottonMenu.Controls.Add(this.btnPage4);
            this.pBottonMenu.Controls.Add(this.btnPage1);
            this.pBottonMenu.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pBottonMenu.Location = new System.Drawing.Point(0, 1016);
            this.pBottonMenu.Name = "pBottonMenu";
            this.pBottonMenu.Size = new System.Drawing.Size(1920, 64);
            this.pBottonMenu.TabIndex = 14;
            // 
            // btnPage6
            // 
            this.btnPage6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPage6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btnPage6.FlatAppearance.BorderSize = 0;
            this.btnPage6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPage6.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPage6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(121)))), ((int)(((byte)(126)))));
            this.btnPage6.Location = new System.Drawing.Point(388, 0);
            this.btnPage6.Margin = new System.Windows.Forms.Padding(0);
            this.btnPage6.Name = "btnPage6";
            this.btnPage6.Size = new System.Drawing.Size(128, 64);
            this.btnPage6.TabIndex = 2;
            this.btnPage6.Text = "Gráfico";
            this.btnPage6.UseVisualStyleBackColor = false;
            this.btnPage6.Click += new System.EventHandler(this.btnPage6_Click);
            // 
            // btnPage5
            // 
            this.btnPage5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPage5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btnPage5.FlatAppearance.BorderSize = 0;
            this.btnPage5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPage5.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPage5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(121)))), ((int)(((byte)(126)))));
            this.btnPage5.Location = new System.Drawing.Point(259, 0);
            this.btnPage5.Margin = new System.Windows.Forms.Padding(0);
            this.btnPage5.Name = "btnPage5";
            this.btnPage5.Size = new System.Drawing.Size(128, 64);
            this.btnPage5.TabIndex = 1;
            this.btnPage5.Text = "Histórico";
            this.btnPage5.UseVisualStyleBackColor = false;
            this.btnPage5.Click += new System.EventHandler(this.btnPage5_Click);
            // 
            // btnPage4
            // 
            this.btnPage4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPage4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btnPage4.FlatAppearance.BorderSize = 0;
            this.btnPage4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPage4.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPage4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(121)))), ((int)(((byte)(126)))));
            this.btnPage4.Location = new System.Drawing.Point(130, 0);
            this.btnPage4.Margin = new System.Windows.Forms.Padding(0);
            this.btnPage4.Name = "btnPage4";
            this.btnPage4.Size = new System.Drawing.Size(128, 64);
            this.btnPage4.TabIndex = 1;
            this.btnPage4.Text = "Pedidos";
            this.btnPage4.UseVisualStyleBackColor = false;
            this.btnPage4.Click += new System.EventHandler(this.btnPage4_Click);
            // 
            // btnPage1
            // 
            this.btnPage1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPage1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(230)))));
            this.btnPage1.FlatAppearance.BorderSize = 0;
            this.btnPage1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPage1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPage1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(121)))), ((int)(((byte)(126)))));
            this.btnPage1.Location = new System.Drawing.Point(-1, 0);
            this.btnPage1.Name = "btnPage1";
            this.btnPage1.Size = new System.Drawing.Size(128, 64);
            this.btnPage1.TabIndex = 0;
            this.btnPage1.Text = "Principal";
            this.btnPage1.UseVisualStyleBackColor = false;
            this.btnPage1.Click += new System.EventHandler(this.btnPage1_Click);
            // 
            // pInformation
            // 
            this.pInformation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(49)))), ((int)(((byte)(57)))));
            this.pInformation.Controls.Add(this.lblConnectionState);
            this.pInformation.Controls.Add(this.label3);
            this.pInformation.Controls.Add(this.pictureBox6);
            this.pInformation.Controls.Add(this.pictureBox1);
            this.pInformation.Controls.Add(this.pictureBox3);
            this.pInformation.Controls.Add(this.lbl_currentOrder_lineSpeed);
            this.pInformation.Controls.Add(this.lblDateTimeAct);
            this.pInformation.Controls.Add(this.label10);
            this.pInformation.Controls.Add(this.lblDateTime);
            this.pInformation.Controls.Add(this.btnLogin);
            this.pInformation.Controls.Add(this.lblLoggedUser);
            this.pInformation.Controls.Add(this.lblUserName);
            this.pInformation.Controls.Add(this.imgUserLogin);
            this.pInformation.Dock = System.Windows.Forms.DockStyle.Top;
            this.pInformation.Location = new System.Drawing.Point(0, 52);
            this.pInformation.Name = "pInformation";
            this.pInformation.Size = new System.Drawing.Size(1920, 58);
            this.pInformation.TabIndex = 1;
            // 
            // lblConnectionState
            // 
            this.lblConnectionState.AutoSize = true;
            this.lblConnectionState.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblConnectionState.ForeColor = System.Drawing.Color.White;
            this.lblConnectionState.Location = new System.Drawing.Point(1755, 30);
            this.lblConnectionState.Name = "lblConnectionState";
            this.lblConnectionState.Size = new System.Drawing.Size(153, 20);
            this.lblConnectionState.TabIndex = 32;
            this.lblConnectionState.Text = "XXXXXXXXXXXX";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(61)))), ((int)(((byte)(74)))), ((int)(((byte)(78)))));
            this.label3.Location = new System.Drawing.Point(1755, 4);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 16);
            this.label3.TabIndex = 31;
            this.label3.Text = "Conexão:";
            // 
            // pictureBox6
            // 
            this.pictureBox6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(72)))), ((int)(((byte)(77)))));
            this.pictureBox6.Location = new System.Drawing.Point(597, 6);
            this.pictureBox6.Name = "pictureBox6";
            this.pictureBox6.Size = new System.Drawing.Size(2, 50);
            this.pictureBox6.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox6.TabIndex = 29;
            this.pictureBox6.TabStop = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(72)))), ((int)(((byte)(77)))));
            this.pictureBox1.Location = new System.Drawing.Point(257, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(2, 50);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 26;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(72)))), ((int)(((byte)(77)))));
            this.pictureBox3.Location = new System.Drawing.Point(496, 6);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(2, 50);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox3.TabIndex = 26;
            this.pictureBox3.TabStop = false;
            // 
            // lbl_currentOrder_lineSpeed
            // 
            this.lbl_currentOrder_lineSpeed.AutoSize = true;
            this.lbl_currentOrder_lineSpeed.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_currentOrder_lineSpeed.ForeColor = System.Drawing.Color.White;
            this.lbl_currentOrder_lineSpeed.Location = new System.Drawing.Point(501, 30);
            this.lbl_currentOrder_lineSpeed.Name = "lbl_currentOrder_lineSpeed";
            this.lbl_currentOrder_lineSpeed.Size = new System.Drawing.Size(67, 29);
            this.lbl_currentOrder_lineSpeed.TabIndex = 21;
            this.lbl_currentOrder_lineSpeed.Text = "XXX";
            // 
            // lblDateTimeAct
            // 
            this.lblDateTimeAct.AutoSize = true;
            this.lblDateTimeAct.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDateTimeAct.ForeColor = System.Drawing.Color.White;
            this.lblDateTimeAct.Location = new System.Drawing.Point(287, 33);
            this.lblDateTimeAct.Name = "lblDateTimeAct";
            this.lblDateTimeAct.Size = new System.Drawing.Size(86, 20);
            this.lblDateTimeAct.TabIndex = 22;
            this.lblDateTimeAct.Text = "DateTime";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(61)))), ((int)(((byte)(74)))), ((int)(((byte)(78)))));
            this.label10.Location = new System.Drawing.Point(501, 7);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(92, 16);
            this.label10.TabIndex = 19;
            this.label10.Text = "Velocidade:";
            this.label10.Click += new System.EventHandler(this.label10_Click);
            // 
            // lblDateTime
            // 
            this.lblDateTime.AutoSize = true;
            this.lblDateTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDateTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(61)))), ((int)(((byte)(74)))), ((int)(((byte)(78)))));
            this.lblDateTime.Location = new System.Drawing.Point(287, 4);
            this.lblDateTime.Name = "lblDateTime";
            this.lblDateTime.Size = new System.Drawing.Size(84, 16);
            this.lblDateTime.TabIndex = 20;
            this.lblDateTime.Text = "Data/Hora:";
            // 
            // btnLogin
            // 
            this.btnLogin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(72)))), ((int)(((byte)(77)))));
            this.btnLogin.FlatAppearance.BorderSize = 0;
            this.btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogin.Image = ((System.Drawing.Image)(resources.GetObject("btnLogin.Image")));
            this.btnLogin.Location = new System.Drawing.Point(169, 4);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(60, 48);
            this.btnLogin.TabIndex = 15;
            this.btnLogin.UseVisualStyleBackColor = false;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // lblLoggedUser
            // 
            this.lblLoggedUser.AutoSize = true;
            this.lblLoggedUser.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLoggedUser.ForeColor = System.Drawing.Color.White;
            this.lblLoggedUser.Location = new System.Drawing.Point(69, 33);
            this.lblLoggedUser.Name = "lblLoggedUser";
            this.lblLoggedUser.Size = new System.Drawing.Size(68, 20);
            this.lblLoggedUser.TabIndex = 18;
            this.lblLoggedUser.Text = "Service";
            // 
            // lblUserName
            // 
            this.lblUserName.AutoSize = true;
            this.lblUserName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUserName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(61)))), ((int)(((byte)(74)))), ((int)(((byte)(78)))));
            this.lblUserName.Location = new System.Drawing.Point(69, 7);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(66, 16);
            this.lblUserName.TabIndex = 17;
            this.lblUserName.Text = "Usuario:";
            // 
            // imgUserLogin
            // 
            this.imgUserLogin.BackColor = System.Drawing.Color.Transparent;
            this.imgUserLogin.Image = ((System.Drawing.Image)(resources.GetObject("imgUserLogin.Image")));
            this.imgUserLogin.Location = new System.Drawing.Point(3, 5);
            this.imgUserLogin.Name = "imgUserLogin";
            this.imgUserLogin.Size = new System.Drawing.Size(60, 48);
            this.imgUserLogin.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.imgUserLogin.TabIndex = 16;
            this.imgUserLogin.TabStop = false;
            // 
            // pTitle
            // 
            this.pTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(95)))), ((int)(((byte)(135)))));
            this.pTitle.Controls.Add(this.label1);
            this.pTitle.Controls.Add(this.lblTitle);
            this.pTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.pTitle.Location = new System.Drawing.Point(0, 0);
            this.pTitle.Name = "pTitle";
            this.pTitle.Size = new System.Drawing.Size(1920, 52);
            this.pTitle.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(1555, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(353, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "CPNTeck - Automação Industrial";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(3, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(533, 25);
            this.lblTitle.TabIndex = 2;
            this.lblTitle.Text = "SISTEMA DE CONTROLE DRY-END // MRBRASIL";
            // 
            // tmrDateTime
            // 
            this.tmrDateTime.Enabled = true;
            this.tmrDateTime.Tick += new System.EventHandler(this.tmrDateTime_Tick);
            // 
            // tmrUpdateNextOrder
            // 
            this.tmrUpdateNextOrder.Enabled = true;
            this.tmrUpdateNextOrder.Interval = 5000;
            this.tmrUpdateNextOrder.Tick += new System.EventHandler(this.tmrUpdateNextOrder_Tick);
            // 
            // tmrLogMachineSpeed
            // 
            this.tmrLogMachineSpeed.Enabled = true;
            this.tmrLogMachineSpeed.Interval = 60000;
            this.tmrLogMachineSpeed.Tick += new System.EventHandler(this.tmrLogMachineSpeed_Tick);
            // 
            // tmrUpdateInterface
            // 
            this.tmrUpdateInterface.Enabled = true;
            this.tmrUpdateInterface.Interval = 1000;
            this.tmrUpdateInterface.Tick += new System.EventHandler(this.tmrUpdateInterface_Tick);
            // 
            // nextOrderBindingSource
            // 
            this.nextOrderBindingSource.DataSource = typeof(Hmi.Data.ProductionList);
            // 
            // currentOrderBindingSource
            // 
            this.currentOrderBindingSource.DataSource = typeof(Hmi.Data.ProductionList);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1920, 1080);
            this.Controls.Add(this.pContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DryEnd - HMI";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.pContainer.ResumeLayout(false);
            this.pBottonMenu.ResumeLayout(false);
            this.pInformation.ResumeLayout(false);
            this.pInformation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imgUserLogin)).EndInit();
            this.pTitle.ResumeLayout(false);
            this.pTitle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nextOrderBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.currentOrderBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel pContainer;
        private System.Windows.Forms.Panel pInformation;
        private System.Windows.Forms.Panel pTitle;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pBottonMenu;
        private System.Windows.Forms.Button btnPage1;
        private System.Windows.Forms.PictureBox pictureBox6;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.Label lbl_currentOrder_lineSpeed;
        private System.Windows.Forms.Label lblDateTimeAct;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label lblDateTime;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label lblLoggedUser;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.PictureBox imgUserLogin;
        private System.Windows.Forms.Panel pStatus;
        private System.Windows.Forms.Timer tmrDateTime;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnPage4;
        private System.Windows.Forms.BindingSource nextOrderBindingSource;
        private System.Windows.Forms.BindingSource currentOrderBindingSource;
        private System.Windows.Forms.Timer tmrUpdateNextOrder;
        private System.Windows.Forms.Button btnPage5;
        private System.Windows.Forms.Label lblConnectionState;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel pMain;
        private System.Windows.Forms.Timer tmrLogMachineSpeed;
        private System.Windows.Forms.Button btnPage6;
        private System.Windows.Forms.Timer tmrUpdateInterface;
    }
}

