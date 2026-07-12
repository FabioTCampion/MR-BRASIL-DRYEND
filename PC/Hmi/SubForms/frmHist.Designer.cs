
namespace Hmi
{
    partial class frmHist
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.paperBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.paperCompositionBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.productBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gpbFilter = new System.Windows.Forms.GroupBox();
            this.txtM2 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtLinearMeters = new System.Windows.Forms.TextBox();
            this.lblLinearMeters = new System.Windows.Forms.Label();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.ckbSearchOF = new System.Windows.Forms.CheckBox();
            this.ckbSearchProduct = new System.Windows.Forms.CheckBox();
            this.ckbSearchComposition = new System.Windows.Forms.CheckBox();
            this.ckbSearchWidth = new System.Windows.Forms.CheckBox();
            this.ckbSearchByDate = new System.Windows.Forms.CheckBox();
            this.ckbSearchListNumber = new System.Windows.Forms.CheckBox();
            this.ckbSearchClientName = new System.Windows.Forms.CheckBox();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnClearFilter = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.dtgOrders = new System.Windows.Forms.DataGridView();
            this.Order1Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Order2Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DETALHES = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.idDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.productionSequenceDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.productionStateDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.startedAtDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.finishedAtDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.paperCompositionDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fluteTypeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.paperWidthDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.paper1DataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.paper2DataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.paper3DataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.paper4DataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.paper5DataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.productionListNumberDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.orderDetailsDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.order1DescriptionDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.order2DescriptionDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.productionListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.nextOrderBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.clientsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.paperBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.paperCompositionBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.productBindingSource)).BeginInit();
            this.gpbFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dtgOrders)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.productionListBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nextOrderBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.clientsBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // gpbFilter
            // 
            this.gpbFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gpbFilter.Controls.Add(this.txtM2);
            this.gpbFilter.Controls.Add(this.label1);
            this.gpbFilter.Controls.Add(this.txtLinearMeters);
            this.gpbFilter.Controls.Add(this.lblLinearMeters);
            this.gpbFilter.Controls.Add(this.dateTimePicker1);
            this.gpbFilter.Controls.Add(this.ckbSearchOF);
            this.gpbFilter.Controls.Add(this.ckbSearchProduct);
            this.gpbFilter.Controls.Add(this.ckbSearchComposition);
            this.gpbFilter.Controls.Add(this.ckbSearchWidth);
            this.gpbFilter.Controls.Add(this.ckbSearchByDate);
            this.gpbFilter.Controls.Add(this.ckbSearchListNumber);
            this.gpbFilter.Controls.Add(this.ckbSearchClientName);
            this.gpbFilter.Controls.Add(this.txtSearch);
            this.gpbFilter.Controls.Add(this.btnClearFilter);
            this.gpbFilter.Controls.Add(this.btnSearch);
            this.gpbFilter.Controls.Add(this.label2);
            this.gpbFilter.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gpbFilter.Location = new System.Drawing.Point(12, 12);
            this.gpbFilter.Name = "gpbFilter";
            this.gpbFilter.Size = new System.Drawing.Size(1896, 168);
            this.gpbFilter.TabIndex = 170;
            this.gpbFilter.TabStop = false;
            this.gpbFilter.Text = "Filtrar resultados";
            // 
            // txtM2
            // 
            this.txtM2.Enabled = false;
            this.txtM2.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtM2.Location = new System.Drawing.Point(1646, 56);
            this.txtM2.Name = "txtM2";
            this.txtM2.Size = new System.Drawing.Size(227, 47);
            this.txtM2.TabIndex = 168;
            this.txtM2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(1590, 64);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 33);
            this.label1.TabIndex = 167;
            this.label1.Text = "M²:";
            // 
            // txtLinearMeters
            // 
            this.txtLinearMeters.Enabled = false;
            this.txtLinearMeters.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLinearMeters.Location = new System.Drawing.Point(1646, 113);
            this.txtLinearMeters.Name = "txtLinearMeters";
            this.txtLinearMeters.Size = new System.Drawing.Size(227, 47);
            this.txtLinearMeters.TabIndex = 168;
            this.txtLinearMeters.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblLinearMeters
            // 
            this.lblLinearMeters.AutoSize = true;
            this.lblLinearMeters.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLinearMeters.Location = new System.Drawing.Point(1460, 121);
            this.lblLinearMeters.Name = "lblLinearMeters";
            this.lblLinearMeters.Size = new System.Drawing.Size(190, 33);
            this.lblLinearMeters.TabIndex = 167;
            this.lblLinearMeters.Text = "Metro linear:";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker1.Location = new System.Drawing.Point(237, 39);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(200, 20);
            this.dateTimePicker1.TabIndex = 166;
            // 
            // ckbSearchOF
            // 
            this.ckbSearchOF.AutoSize = true;
            this.ckbSearchOF.Location = new System.Drawing.Point(130, 63);
            this.ckbSearchOF.Name = "ckbSearchOF";
            this.ckbSearchOF.Size = new System.Drawing.Size(107, 17);
            this.ckbSearchOF.TabIndex = 128;
            this.ckbSearchOF.Text = "Numero da OF";
            this.ckbSearchOF.UseVisualStyleBackColor = true;
            this.ckbSearchOF.CheckedChanged += new System.EventHandler(this.ckbSearchCheckedChanged);
            // 
            // ckbSearchProduct
            // 
            this.ckbSearchProduct.AutoSize = true;
            this.ckbSearchProduct.Location = new System.Drawing.Point(131, 86);
            this.ckbSearchProduct.Name = "ckbSearchProduct";
            this.ckbSearchProduct.Size = new System.Drawing.Size(70, 17);
            this.ckbSearchProduct.TabIndex = 128;
            this.ckbSearchProduct.Text = "Produto";
            this.ckbSearchProduct.UseVisualStyleBackColor = true;
            this.ckbSearchProduct.CheckedChanged += new System.EventHandler(this.ckbSearchCheckedChanged);
            // 
            // ckbSearchComposition
            // 
            this.ckbSearchComposition.AutoSize = true;
            this.ckbSearchComposition.Location = new System.Drawing.Point(243, 63);
            this.ckbSearchComposition.Name = "ckbSearchComposition";
            this.ckbSearchComposition.Size = new System.Drawing.Size(94, 17);
            this.ckbSearchComposition.TabIndex = 128;
            this.ckbSearchComposition.Text = "Composição";
            this.ckbSearchComposition.UseVisualStyleBackColor = true;
            this.ckbSearchComposition.CheckedChanged += new System.EventHandler(this.ckbSearchCheckedChanged);
            // 
            // ckbSearchWidth
            // 
            this.ckbSearchWidth.AutoSize = true;
            this.ckbSearchWidth.Location = new System.Drawing.Point(243, 86);
            this.ckbSearchWidth.Name = "ckbSearchWidth";
            this.ckbSearchWidth.Size = new System.Drawing.Size(69, 17);
            this.ckbSearchWidth.TabIndex = 128;
            this.ckbSearchWidth.Text = "Largura";
            this.ckbSearchWidth.UseVisualStyleBackColor = true;
            this.ckbSearchWidth.CheckedChanged += new System.EventHandler(this.ckbSearchCheckedChanged);
            // 
            // ckbSearchByDate
            // 
            this.ckbSearchByDate.AutoSize = true;
            this.ckbSearchByDate.Checked = true;
            this.ckbSearchByDate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ckbSearchByDate.Location = new System.Drawing.Point(6, 113);
            this.ckbSearchByDate.Name = "ckbSearchByDate";
            this.ckbSearchByDate.Size = new System.Drawing.Size(53, 17);
            this.ckbSearchByDate.TabIndex = 128;
            this.ckbSearchByDate.Text = "Data";
            this.ckbSearchByDate.UseVisualStyleBackColor = true;
            this.ckbSearchByDate.CheckedChanged += new System.EventHandler(this.ckbSearchCheckedChanged);
            // 
            // ckbSearchListNumber
            // 
            this.ckbSearchListNumber.AutoSize = true;
            this.ckbSearchListNumber.Location = new System.Drawing.Point(6, 88);
            this.ckbSearchListNumber.Name = "ckbSearchListNumber";
            this.ckbSearchListNumber.Size = new System.Drawing.Size(114, 17);
            this.ckbSearchListNumber.TabIndex = 128;
            this.ckbSearchListNumber.Text = "Numero da lista";
            this.ckbSearchListNumber.UseVisualStyleBackColor = true;
            this.ckbSearchListNumber.CheckedChanged += new System.EventHandler(this.ckbSearchCheckedChanged);
            // 
            // ckbSearchClientName
            // 
            this.ckbSearchClientName.AutoSize = true;
            this.ckbSearchClientName.Location = new System.Drawing.Point(6, 65);
            this.ckbSearchClientName.Name = "ckbSearchClientName";
            this.ckbSearchClientName.Size = new System.Drawing.Size(118, 17);
            this.ckbSearchClientName.TabIndex = 128;
            this.ckbSearchClientName.Text = "Nome do cliente";
            this.ckbSearchClientName.UseVisualStyleBackColor = true;
            this.ckbSearchClientName.CheckedChanged += new System.EventHandler(this.ckbSearchCheckedChanged);
            // 
            // txtSearch
            // 
            this.txtSearch.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.txtSearch.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txtSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSearch.Location = new System.Drawing.Point(6, 39);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(219, 20);
            this.txtSearch.TabIndex = 0;
            // 
            // btnClearFilter
            // 
            this.btnClearFilter.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClearFilter.Location = new System.Drawing.Point(225, 136);
            this.btnClearFilter.Name = "btnClearFilter";
            this.btnClearFilter.Size = new System.Drawing.Size(212, 26);
            this.btnClearFilter.TabIndex = 165;
            this.btnClearFilter.Text = "Mostrar todos";
            this.btnClearFilter.UseVisualStyleBackColor = true;
            this.btnClearFilter.Click += new System.EventHandler(this.btnClearFilter_Click_1);
            // 
            // btnSearch
            // 
            this.btnSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSearch.Location = new System.Drawing.Point(6, 136);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(212, 26);
            this.btnSearch.TabIndex = 165;
            this.btnSearch.Text = "Buscar";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(6, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 127;
            this.label2.Text = "Buscar:";
            // 
            // dtgOrders
            // 
            this.dtgOrders.AllowUserToAddRows = false;
            this.dtgOrders.AllowUserToDeleteRows = false;
            this.dtgOrders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dtgOrders.AutoGenerateColumns = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dtgOrders.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dtgOrders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dtgOrders.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Order1Description,
            this.Order2Description,
            this.DETALHES,
            this.idDataGridViewTextBoxColumn,
            this.productionSequenceDataGridViewTextBoxColumn,
            this.productionStateDataGridViewTextBoxColumn,
            this.startedAtDataGridViewTextBoxColumn,
            this.finishedAtDataGridViewTextBoxColumn,
            this.paperCompositionDataGridViewTextBoxColumn,
            this.fluteTypeDataGridViewTextBoxColumn,
            this.paperWidthDataGridViewTextBoxColumn,
            this.paper1DataGridViewTextBoxColumn,
            this.paper2DataGridViewTextBoxColumn,
            this.paper3DataGridViewTextBoxColumn,
            this.paper4DataGridViewTextBoxColumn,
            this.paper5DataGridViewTextBoxColumn,
            this.productionListNumberDataGridViewTextBoxColumn,
            this.orderDetailsDataGridViewTextBoxColumn,
            this.order1DescriptionDataGridViewTextBoxColumn,
            this.order2DescriptionDataGridViewTextBoxColumn});
            this.dtgOrders.DataSource = this.productionListBindingSource;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dtgOrders.DefaultCellStyle = dataGridViewCellStyle5;
            this.dtgOrders.Location = new System.Drawing.Point(12, 186);
            this.dtgOrders.MultiSelect = false;
            this.dtgOrders.Name = "dtgOrders";
            this.dtgOrders.ReadOnly = true;
            this.dtgOrders.RowTemplate.Height = 130;
            this.dtgOrders.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dtgOrders.Size = new System.Drawing.Size(1896, 698);
            this.dtgOrders.TabIndex = 169;
            // 
            // Order1Description
            // 
            this.Order1Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Order1Description.DataPropertyName = "Order1Description";
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.Order1Description.DefaultCellStyle = dataGridViewCellStyle2;
            this.Order1Description.FillWeight = 250F;
            this.Order1Description.HeaderText = "Pedido - 01";
            this.Order1Description.MinimumWidth = 300;
            this.Order1Description.Name = "Order1Description";
            this.Order1Description.ReadOnly = true;
            // 
            // Order2Description
            // 
            this.Order2Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Order2Description.DataPropertyName = "Order2Description";
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.Order2Description.DefaultCellStyle = dataGridViewCellStyle3;
            this.Order2Description.FillWeight = 250F;
            this.Order2Description.HeaderText = "Pedido - 02";
            this.Order2Description.MinimumWidth = 300;
            this.Order2Description.Name = "Order2Description";
            this.Order2Description.ReadOnly = true;
            // 
            // DETALHES
            // 
            this.DETALHES.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DETALHES.DataPropertyName = "OrderDetails";
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DETALHES.DefaultCellStyle = dataGridViewCellStyle4;
            this.DETALHES.FillWeight = 250F;
            this.DETALHES.HeaderText = "Detalhes";
            this.DETALHES.MinimumWidth = 200;
            this.DETALHES.Name = "DETALHES";
            this.DETALHES.ReadOnly = true;
            // 
            // idDataGridViewTextBoxColumn
            // 
            this.idDataGridViewTextBoxColumn.DataPropertyName = "Id";
            this.idDataGridViewTextBoxColumn.HeaderText = "Id";
            this.idDataGridViewTextBoxColumn.Name = "idDataGridViewTextBoxColumn";
            this.idDataGridViewTextBoxColumn.ReadOnly = true;
            this.idDataGridViewTextBoxColumn.Visible = false;
            // 
            // productionSequenceDataGridViewTextBoxColumn
            // 
            this.productionSequenceDataGridViewTextBoxColumn.DataPropertyName = "ProductionSequence";
            this.productionSequenceDataGridViewTextBoxColumn.HeaderText = "ProductionSequence";
            this.productionSequenceDataGridViewTextBoxColumn.Name = "productionSequenceDataGridViewTextBoxColumn";
            this.productionSequenceDataGridViewTextBoxColumn.ReadOnly = true;
            this.productionSequenceDataGridViewTextBoxColumn.Visible = false;
            // 
            // productionStateDataGridViewTextBoxColumn
            // 
            this.productionStateDataGridViewTextBoxColumn.DataPropertyName = "ProductionState";
            this.productionStateDataGridViewTextBoxColumn.HeaderText = "ProductionState";
            this.productionStateDataGridViewTextBoxColumn.Name = "productionStateDataGridViewTextBoxColumn";
            this.productionStateDataGridViewTextBoxColumn.ReadOnly = true;
            this.productionStateDataGridViewTextBoxColumn.Visible = false;
            // 
            // startedAtDataGridViewTextBoxColumn
            // 
            this.startedAtDataGridViewTextBoxColumn.DataPropertyName = "StartedAt";
            this.startedAtDataGridViewTextBoxColumn.HeaderText = "StartedAt";
            this.startedAtDataGridViewTextBoxColumn.Name = "startedAtDataGridViewTextBoxColumn";
            this.startedAtDataGridViewTextBoxColumn.ReadOnly = true;
            this.startedAtDataGridViewTextBoxColumn.Visible = false;
            // 
            // finishedAtDataGridViewTextBoxColumn
            // 
            this.finishedAtDataGridViewTextBoxColumn.DataPropertyName = "FinishedAt";
            this.finishedAtDataGridViewTextBoxColumn.HeaderText = "FinishedAt";
            this.finishedAtDataGridViewTextBoxColumn.Name = "finishedAtDataGridViewTextBoxColumn";
            this.finishedAtDataGridViewTextBoxColumn.ReadOnly = true;
            this.finishedAtDataGridViewTextBoxColumn.Visible = false;
            // 
            // paperCompositionDataGridViewTextBoxColumn
            // 
            this.paperCompositionDataGridViewTextBoxColumn.DataPropertyName = "PaperComposition";
            this.paperCompositionDataGridViewTextBoxColumn.HeaderText = "PaperComposition";
            this.paperCompositionDataGridViewTextBoxColumn.Name = "paperCompositionDataGridViewTextBoxColumn";
            this.paperCompositionDataGridViewTextBoxColumn.ReadOnly = true;
            this.paperCompositionDataGridViewTextBoxColumn.Visible = false;
            // 
            // fluteTypeDataGridViewTextBoxColumn
            // 
            this.fluteTypeDataGridViewTextBoxColumn.DataPropertyName = "FluteType";
            this.fluteTypeDataGridViewTextBoxColumn.HeaderText = "FluteType";
            this.fluteTypeDataGridViewTextBoxColumn.Name = "fluteTypeDataGridViewTextBoxColumn";
            this.fluteTypeDataGridViewTextBoxColumn.ReadOnly = true;
            this.fluteTypeDataGridViewTextBoxColumn.Visible = false;
            // 
            // paperWidthDataGridViewTextBoxColumn
            // 
            this.paperWidthDataGridViewTextBoxColumn.DataPropertyName = "PaperWidth";
            this.paperWidthDataGridViewTextBoxColumn.HeaderText = "PaperWidth";
            this.paperWidthDataGridViewTextBoxColumn.Name = "paperWidthDataGridViewTextBoxColumn";
            this.paperWidthDataGridViewTextBoxColumn.ReadOnly = true;
            this.paperWidthDataGridViewTextBoxColumn.Visible = false;
            // 
            // paper1DataGridViewTextBoxColumn
            // 
            this.paper1DataGridViewTextBoxColumn.DataPropertyName = "Paper1";
            this.paper1DataGridViewTextBoxColumn.HeaderText = "Paper1";
            this.paper1DataGridViewTextBoxColumn.Name = "paper1DataGridViewTextBoxColumn";
            this.paper1DataGridViewTextBoxColumn.ReadOnly = true;
            this.paper1DataGridViewTextBoxColumn.Visible = false;
            // 
            // paper2DataGridViewTextBoxColumn
            // 
            this.paper2DataGridViewTextBoxColumn.DataPropertyName = "Paper2";
            this.paper2DataGridViewTextBoxColumn.HeaderText = "Paper2";
            this.paper2DataGridViewTextBoxColumn.Name = "paper2DataGridViewTextBoxColumn";
            this.paper2DataGridViewTextBoxColumn.ReadOnly = true;
            this.paper2DataGridViewTextBoxColumn.Visible = false;
            // 
            // paper3DataGridViewTextBoxColumn
            // 
            this.paper3DataGridViewTextBoxColumn.DataPropertyName = "Paper3";
            this.paper3DataGridViewTextBoxColumn.HeaderText = "Paper3";
            this.paper3DataGridViewTextBoxColumn.Name = "paper3DataGridViewTextBoxColumn";
            this.paper3DataGridViewTextBoxColumn.ReadOnly = true;
            this.paper3DataGridViewTextBoxColumn.Visible = false;
            // 
            // paper4DataGridViewTextBoxColumn
            // 
            this.paper4DataGridViewTextBoxColumn.DataPropertyName = "Paper4";
            this.paper4DataGridViewTextBoxColumn.HeaderText = "Paper4";
            this.paper4DataGridViewTextBoxColumn.Name = "paper4DataGridViewTextBoxColumn";
            this.paper4DataGridViewTextBoxColumn.ReadOnly = true;
            this.paper4DataGridViewTextBoxColumn.Visible = false;
            // 
            // paper5DataGridViewTextBoxColumn
            // 
            this.paper5DataGridViewTextBoxColumn.DataPropertyName = "Paper5";
            this.paper5DataGridViewTextBoxColumn.HeaderText = "Paper5";
            this.paper5DataGridViewTextBoxColumn.Name = "paper5DataGridViewTextBoxColumn";
            this.paper5DataGridViewTextBoxColumn.ReadOnly = true;
            this.paper5DataGridViewTextBoxColumn.Visible = false;
            // 
            // productionListNumberDataGridViewTextBoxColumn
            // 
            this.productionListNumberDataGridViewTextBoxColumn.DataPropertyName = "ProductionListNumber";
            this.productionListNumberDataGridViewTextBoxColumn.HeaderText = "ProductionListNumber";
            this.productionListNumberDataGridViewTextBoxColumn.Name = "productionListNumberDataGridViewTextBoxColumn";
            this.productionListNumberDataGridViewTextBoxColumn.ReadOnly = true;
            this.productionListNumberDataGridViewTextBoxColumn.Visible = false;
            // 
            // orderDetailsDataGridViewTextBoxColumn
            // 
            this.orderDetailsDataGridViewTextBoxColumn.DataPropertyName = "OrderDetails";
            this.orderDetailsDataGridViewTextBoxColumn.HeaderText = "OrderDetails";
            this.orderDetailsDataGridViewTextBoxColumn.Name = "orderDetailsDataGridViewTextBoxColumn";
            this.orderDetailsDataGridViewTextBoxColumn.ReadOnly = true;
            this.orderDetailsDataGridViewTextBoxColumn.Visible = false;
            // 
            // order1DescriptionDataGridViewTextBoxColumn
            // 
            this.order1DescriptionDataGridViewTextBoxColumn.DataPropertyName = "Order1Description";
            this.order1DescriptionDataGridViewTextBoxColumn.HeaderText = "Order1Description";
            this.order1DescriptionDataGridViewTextBoxColumn.Name = "order1DescriptionDataGridViewTextBoxColumn";
            this.order1DescriptionDataGridViewTextBoxColumn.ReadOnly = true;
            this.order1DescriptionDataGridViewTextBoxColumn.Visible = false;
            // 
            // order2DescriptionDataGridViewTextBoxColumn
            // 
            this.order2DescriptionDataGridViewTextBoxColumn.DataPropertyName = "Order2Description";
            this.order2DescriptionDataGridViewTextBoxColumn.HeaderText = "Order2Description";
            this.order2DescriptionDataGridViewTextBoxColumn.Name = "order2DescriptionDataGridViewTextBoxColumn";
            this.order2DescriptionDataGridViewTextBoxColumn.ReadOnly = true;
            this.order2DescriptionDataGridViewTextBoxColumn.Visible = false;
            // 
            // productionListBindingSource
            // 
            this.productionListBindingSource.DataSource = typeof(Hmi.Data.ProductionListPlc);
            // 
            // nextOrderBindingSource
            // 
            this.nextOrderBindingSource.DataSource = typeof(Hmi.Data.ProductionListPlc);
            // 
            // clientsBindingSource
            // 
            this.clientsBindingSource.DataSource = typeof(Hmi.Data.ClientsList);
            // 
            // frmHist
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(248)))));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(1920, 896);
            this.Controls.Add(this.gpbFilter);
            this.Controls.Add(this.dtgOrders);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmHist";
            this.Text = "frmOrders";
            this.Load += new System.EventHandler(this.frmOrders_Load);
            this.Enter += new System.EventHandler(this.frmHist_Enter);
            ((System.ComponentModel.ISupportInitialize)(this.paperBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.paperCompositionBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.productBindingSource)).EndInit();
            this.gpbFilter.ResumeLayout(false);
            this.gpbFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dtgOrders)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.productionListBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nextOrderBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.clientsBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.BindingSource productionListBindingSource;
        private System.Windows.Forms.BindingSource nextOrderBindingSource;
        private System.Windows.Forms.BindingSource clientsBindingSource;
        private System.Windows.Forms.BindingSource paperBindingSource;
        private System.Windows.Forms.BindingSource paperCompositionBindingSource;
        private System.Windows.Forms.BindingSource productBindingSource;
        private System.Windows.Forms.GroupBox gpbFilter;
        private System.Windows.Forms.CheckBox ckbSearchOF;
        private System.Windows.Forms.CheckBox ckbSearchProduct;
        private System.Windows.Forms.CheckBox ckbSearchComposition;
        private System.Windows.Forms.CheckBox ckbSearchWidth;
        private System.Windows.Forms.CheckBox ckbSearchListNumber;
        private System.Windows.Forms.CheckBox ckbSearchClientName;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnClearFilter;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridView dtgOrders;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.CheckBox ckbSearchByDate;
        private System.Windows.Forms.Label lblLinearMeters;
        private System.Windows.Forms.TextBox txtLinearMeters;
        private System.Windows.Forms.TextBox txtM2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Order1Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn Order2Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn DETALHES;
        private System.Windows.Forms.DataGridViewTextBoxColumn idDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn productionSequenceDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn productionStateDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn startedAtDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn finishedAtDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn paperCompositionDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fluteTypeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn paperWidthDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn paper1DataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn paper2DataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn paper3DataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn paper4DataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn paper5DataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn productionListNumberDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn orderDetailsDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn order1DescriptionDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn order2DescriptionDataGridViewTextBoxColumn;
    }
}