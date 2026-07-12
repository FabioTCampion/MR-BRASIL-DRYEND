using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hmi.Data;
using System.Data.SqlClient;
using System.Configuration;
using Dapper;

namespace Hmi
{
    public partial class frmOrders : Form
    {
        public static frmOrders instance;
        EntityState objState = EntityState.Unchanged;
        // Avoid recursive event handling when we update controls programmatically
        private bool _applyingVisibilityRules = false;

        public frmOrders()
        {
            InitializeComponent();
            instance = this;

        }

        private void frmOrders_Load(object sender, EventArgs e)
        {
            if (frmMain.instance != null)
                frmMain.instance.DatabaseItemsReloaded += OnDatabaseItemsReloaded;

            // -----------------------------
            // Combo setup (your original code)
            // -----------------------------
            currentOrder_order1_sheetType.Items.Clear();
            currentOrder_order1_sheetType.Items.Add("CAIXA SEM VINCOS");        // index 0
            currentOrder_order1_sheetType.Items.Add("CAIXA COM DOIS VINCOS");   // index 1
            currentOrder_order1_sheetType.Items.Add("CAIXA COM QUATRO VINCOS"); // index 2

            currentOrder_order1_sheetType.DataBindings.Clear();
            currentOrder_order1_sheetType.DataBindings.Add(
                new Binding("SelectedIndex", productionListBindingSource, "Order1SheetType", true)
            );

            currentOrder_order2_sheetType.Items.Clear();
            currentOrder_order2_sheetType.Items.Add("CAIXA SEM VINCOS");        // index 0
            currentOrder_order2_sheetType.Items.Add("CAIXA COM DOIS VINCOS");   // index 1
            currentOrder_order2_sheetType.Items.Add("CAIXA COM QUATRO VINCOS"); // index 2

            currentOrder_order2_sheetType.DataBindings.Clear();
            currentOrder_order2_sheetType.DataBindings.Add(
                new Binding("SelectedIndex", productionListBindingSource, "Order2SheetType", true)
            );

            currentOrder_levelSelector.Items.Clear();
            currentOrder_levelSelector.Items.Add("");           // index 0 (blank)
            currentOrder_levelSelector.Items.Add("SUPERIOR");   // index 1
            currentOrder_levelSelector.Items.Add("INFERIOR");   // index 2
            currentOrder_levelSelector.Items.Add("AMBOS");      // index 3

            currentOrder_levelSelector.DataBindings.Clear();
            currentOrder_levelSelector.DataBindings.Add(
                new Binding("SelectedIndex", productionListBindingSource, "LevelSelector", true)
            );

            UpdateValues();
            LoadDatabaseItens();
            UpdateAutoCompleteList();

            gpbProductionList.Enabled = false;

            // -----------------------------
            // Visibility logic (NEW)
            // -----------------------------
            SetupVisibilityLogic();
            ApplyAllVisibilityRules(); // initial apply after load + bindings
        }


        private void OnDatabaseItemsReloaded()
        {
            // Escolha 1: somente refresh do grid
            // dtgOrders.Refresh();

            // Escolha 2 (mais comum): recarregar dados desta tela
            LoadDatabaseItens(); // seu método local que popula binds/grids desta tela
        }

        private void cbSecondOrderEnabled_SelectedIndexChanged(object sender, EventArgs e)
        {
            //// Cast sender to ComboBox
            //ComboBox comboBox = (ComboBox)sender;

            //// Access the Text property of the ComboBox
            //string selectedText = comboBox.Text;

            //if (selectedText == "NÍVEL SUPERIOR")
            //{
            //    DeactivateControls(this, "lowerLevelControls");
            //    ActivateControls(this, "upperLevelControls");
            //}

            //else if (selectedText == "NÍVEL INFERIOR")
            //{
            //    DeactivateControls(this, "upperLevelControls");
            //    ActivateControls(this, "lowerLevelControls");
            //}

            //else if (selectedText == "AMBOS")
            //{
            //    ActivateControls(this, "lowerLevelControls");
            //    ActivateControls(this, "upperLevelControls");
            //}
        }

        private void DeactivateControls(Control parentControl, string targetTag)
        {
            //foreach (Control control in parentControl.Controls)
            //{
            //    if (control.Tag != null && control.Tag.ToString() == targetTag)
            //    {
            //        control.Enabled = false;
            //        control.Text = "";
            //        control.BackColor = Color.FromArgb(255, 192, 192);
            //        control.Refresh();

            //    }

            //    if (control.HasChildren)
            //    {
            //        DeactivateControls(control, targetTag);
            //    }

            //}
        }

        private void ActivateControls(Control parentControl, string targetTag)
        {
            //foreach (Control control in parentControl.Controls)
            //{
            //    if (control.Tag != null && control.Tag.ToString() == targetTag)
            //    {
            //        control.Enabled = true;
            //        control.BackColor = Color.FromArgb(192, 255, 255);
            //    }

            //    if (control.HasChildren)
            //    {
            //        ActivateControls(control, targetTag);
            //    }

            //    // Clear the text for both activated and deactivated controls
            //    //control.Text = "";
            //}
        }

        private void UpdateValues()
        {
            //cbSecondOrderEnabled.SelectedItem = 1;
            //cbSecondOrderEnabled.SelectedText = "NÃO";
            //cbSecondOrderEnabled.Refresh();

            //cbFluteType.SelectedIndex = 0;
            //cbFluteType.SelectedText = "B";
            //cbFluteType.Refresh();

            //cbTopOrderSheetType.SelectedIndex = 0;
            //cbFluteType.SelectedText = "CAIXA SEM VINCOS";
            //cbTopOrderSheetType.Refresh();

            //cbBottomOrderSheetType.SelectedIndex = 0;
            //cbBottomOrderSheetType.SelectedText = "CAIXA SEM VINCOS";
            //cbBottomOrderSheetType.Refresh();



        }

        private void LoadDatabaseItens()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                {
                    db.Open();

                    var list = db.Query<ProductionListPlc>(
                        "SELECT TOP 100 * FROM dbo.ProductionList_Plc WHERE ProductionState < 1 ORDER BY CASE WHEN ProductionSequence > 0 THEN 0 ELSE 1 END, ProductionSequence ASC",
                        commandType: CommandType.Text
                    ).ToList();

                    productionListBindingSource.DataSource = list;
                    productionListBindingSource.ResetBindings(false);

                    UpdateOrderDescription();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            var cur = productionListBindingSource.Current as ProductionListPlc;

        }

        private void TopOrderBoxType(int BoxType)
        {
            if (BoxType == 0)
            {

                currentOrder_order1_M1.Visible = true;
                currentOrder_order1_M2.Visible = false;
                currentOrder_order1_M3.Visible = false;
                currentOrder_order1_M4.Visible = false;
                currentOrder_order1_M5.Visible = false;

                lbl_currentOrder_order1_M1.Visible = true;
                lbl_currentOrder_order1_M2.Visible = false;
                lbl_currentOrder_order1_M3.Visible = false;
                lbl_currentOrder_order1_M4.Visible = false;
                lbl_currentOrder_order1_M5.Visible = false;

            }
            else if (BoxType == 1)
            {
                currentOrder_order1_M1.Visible = true;
                currentOrder_order1_M2.Visible = true;
                currentOrder_order1_M3.Visible = true;
                currentOrder_order1_M4.Visible = false;
                currentOrder_order1_M5.Visible = false;

                lbl_currentOrder_order1_M1.Visible = true;
                lbl_currentOrder_order1_M2.Visible = true;
                lbl_currentOrder_order1_M3.Visible = true;
                lbl_currentOrder_order1_M4.Visible = false;
                lbl_currentOrder_order1_M5.Visible = false;
            }
            else if (BoxType == 2)
            {
                currentOrder_order1_M1.Visible = true;
                currentOrder_order1_M2.Visible = true;
                currentOrder_order1_M3.Visible = true;
                currentOrder_order1_M4.Visible = true;
                currentOrder_order1_M5.Visible = true;

                lbl_currentOrder_order1_M1.Visible = true;
                lbl_currentOrder_order1_M2.Visible = true;
                lbl_currentOrder_order1_M3.Visible = true;
                lbl_currentOrder_order1_M4.Visible = true;
                lbl_currentOrder_order1_M5.Visible = true;

            }


        }

        private void BottomOrderBoxType(int BoxType)
        {
            if (BoxType == 0)
            {

                currentOrder_order2_M1.Visible = true;
                currentOrder_order2_M2.Visible = false;
                currentOrder_order2_M3.Visible = false;
                currentOrder_order2_M4.Visible = false;
                currentOrder_order2_M5.Visible = false;

                lbl_currentOrder_order2_M1.Visible = true;
                lbl_currentOrder_order2_M2.Visible = false;
                lbl_currentOrder_order2_M3.Visible = false;
                lbl_currentOrder_order2_M4.Visible = false;
                lbl_currentOrder_order2_M5.Visible = false;

            }
            else if (BoxType == 1)
            {
                currentOrder_order2_M1.Visible = true;
                currentOrder_order2_M2.Visible = true;
                currentOrder_order2_M3.Visible = true;
                currentOrder_order2_M4.Visible = false;
                currentOrder_order2_M5.Visible = false;

                lbl_currentOrder_order2_M1.Visible = true;
                lbl_currentOrder_order2_M2.Visible = true;
                lbl_currentOrder_order2_M3.Visible = true;
                lbl_currentOrder_order2_M4.Visible = false;
                lbl_currentOrder_order2_M5.Visible = false;
            }
            else if (BoxType == 2)
            {
                currentOrder_order2_M1.Visible = true;
                currentOrder_order2_M2.Visible = true;
                currentOrder_order2_M3.Visible = true;
                currentOrder_order2_M4.Visible = true;
                currentOrder_order2_M5.Visible = true;

                lbl_currentOrder_order2_M1.Visible = true;
                lbl_currentOrder_order2_M2.Visible = true;
                lbl_currentOrder_order2_M3.Visible = true;
                lbl_currentOrder_order2_M4.Visible = true;
                lbl_currentOrder_order2_M5.Visible = true;

            }
        }

        private void SetVisibility(NumericUpDown control, Label label, bool isVisible)
        {
            control.Visible = isVisible;
            label.Visible = isVisible;
        }

        public enum BoxType
        {
            BoxWithNoScorer = 0,
            BoxWithTwoScorer = 1,
            BoxWithFourScorer = 2
        }

        private void BottomOrderBoxType(BoxType boxType)
        {
            SetVisibility(currentOrder_order2_M1, lbl_currentOrder_order2_M1, boxType >= BoxType.BoxWithNoScorer);
            SetVisibility(currentOrder_order2_M2, lbl_currentOrder_order2_M2, boxType >= BoxType.BoxWithTwoScorer);
            SetVisibility(currentOrder_order2_M3, lbl_currentOrder_order2_M3, boxType >= BoxType.BoxWithFourScorer);
            SetVisibility(currentOrder_order2_M4, lbl_currentOrder_order2_M4, boxType >= BoxType.BoxWithFourScorer);
            SetVisibility(currentOrder_order2_M5, lbl_currentOrder_order2_M5, boxType >= BoxType.BoxWithFourScorer);
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            // Create + select the new row via BindingSource
            productionListBindingSource.AddNew();
            productionListBindingSource.MoveLast();

            objState = EntityState.Added;
            gpbProductionList.Enabled = true;

            var obj = productionListBindingSource.Current as ProductionListPlc;
            if (obj == null) return;

            // -----------------------------
            // Defaults go into the MODEL
            // -----------------------------
            obj.LevelSelector = 1;             // SUPERIOR (Order1 only)
            obj.FluteType = "B";               // adjust to your real default
            obj.PaperComposition = "N/A";
            obj.PaperWidth = 1800;

            obj.Paper1 = "N/A";
            obj.Paper2 = "N/A";
            obj.Paper3 = "N/A";
            obj.Paper4 = "N/A";
            obj.Paper5 = "N/A";

            obj.Order1Product = "CX";
            obj.Order1SheetQuantity = 1;
            obj.Order1SheetType = 0;
            obj.Order1PileQuantity = 500;

            // Order2 must only exist when AMBOS (LevelSelector == 3)
            if (obj.LevelSelector == 3)
            {
                obj.Order2SheetType = 0;
                obj.Order2PileQuantity = 500;
                obj.Order2SheetQuantity = 1;
            }
            else
            {
                // keep it NULL/cleared to avoid showing/storing stale data
                obj.Order2SheetType = null;
                obj.Order2PileQuantity = null;
                obj.Order2SheetQuantity = null;
            }

            // Push object -> UI
            productionListBindingSource.ResetCurrentItem();

            // Apply your visibility rule (Order2 only in AMBOS)
            ApplyLevelSelectorVisibility(currentOrder_levelSelector.SelectedIndex);

            currentOrder_order1_sheetType.Focus();
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            bool parameterOk;

            parameterOk = checkOrderParameters();

            if (parameterOk == true)
            {
                //Save the order
                saveNewOrder();

                //Update autocomplete list with the new values
                UpdateAutoCompleteList();
            }
        }
        private bool checkOrderParameters()
        {
            bool paperCompositionOK = false;
            bool topOrderParametersOK = true;
            bool bottomOrderParametersOK = true;

            productionListBindingSource.EndEdit();

            paperCompositionOK = checkPaperComposition();

            if (paperCompositionOK & topOrderParametersOK & bottomOrderParametersOK)
            {
                return true;
            }

            return false;
        }
        private bool checkPaperComposition()
        {
            if (currentOrder_paperComposition.Text == "" | currentOrder_paperComposition.Text == null)
            {
                DialogResult dialogResult = MessageBox.Show("Por favor, preencher a composição que será utilizada.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_paperComposition.Focus();

                }
            }
            else if (currentOrder_fluteType.SelectedItem == null)
            {
                DialogResult dialogResult = MessageBox.Show("Por favor, preencher o tipo de onda que será utilizado.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_fluteType.Focus();
                }
            }
            else if (currentOrder_paperWidth.Value < 900)
            {
                DialogResult dialogResult = MessageBox.Show("Por favor, verificar a largura do papel que será utilizado.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_paperWidth.Focus();

                }
            }

            else if (currentOrder_productionListNumber.Text == "" | currentOrder_productionListNumber.Text == null)
            {
                DialogResult dialogResult = MessageBox.Show("Por favor, preencher o número da lista.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_productionListNumber.Focus();

                }
            }

            else
            {
                return true;
            }


            return false;


        }
        private bool checkTopOrderParameters()
        {


            if (currentOrder_levelSelector.SelectedIndex == null | currentOrder_levelSelector.Text == "")
            {
                DialogResult dialogResult = MessageBox.Show("Por favor, selecione o tipo de conjugação.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_levelSelector.Focus();

                }
            }
            else if (currentOrder_order1_product.Text == "" | currentOrder_order1_product.Text == null)
            {
                DialogResult dialogResult = MessageBox.Show("Por favor, preencher o nome do cliente.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_order1_product.Focus();

                }
            }

            else if (currentOrder_order1_sheetType.SelectedItem == null)
            {
                DialogResult dialogResult = MessageBox.Show("Por favor, selecione o tipo de caixa desejado.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_order1_sheetType.Focus();

                }
            }

            else if (currentOrder_Order1_sheetQuantity.Value == 0)
            {
                DialogResult dialogResult = MessageBox.Show("O numero de caixas do pedido deve ser maior que zero.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_Order1_sheetQuantity.Focus();
                }
            }


            else if (currentOrder_order1_numberOfCuts.Value <= 10)
            {
                DialogResult dialogResult = MessageBox.Show("Por favor, inserir o numero de cortes do pedido.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_order1_numberOfCuts.Focus();
                }
            }

            else if (currentOrder_order1_pileQuantity.Value <= 50)
            {
                DialogResult dialogResult = MessageBox.Show("O numero de cortes da pilha deve ser maior que 50.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_order1_pileQuantity.Focus();
                }
            }

            else if (currentOrder_order1_sheetLenght.Value < 450)
            {
                DialogResult dialogResult = MessageBox.Show("O comprimento da caixa deve ser maior que 450 Milímetros.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_order1_sheetLenght.Focus();
                }
            }

            else if (currentOrder_order1_sheetType.SelectedIndex == 0 & currentOrder_order1_M1.Value < 200)
            {
                DialogResult dialogResult = MessageBox.Show("A medida da caixa está abaixo do mínimo permitido.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_order1_M1.Focus();

                }
            }

            else if (currentOrder_order1_sheetType.SelectedIndex == 1
                & (currentOrder_order1_M1.Value + currentOrder_order1_M2.Value + currentOrder_order1_M3.Value) < 200)
            {
                DialogResult dialogResult = MessageBox.Show("A medida da caixa está abaixo do mínimo permitido.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_order1_M1.Focus();

                }
            }

            else if ((currentOrder_order1_sheetType.SelectedIndex == 2 & currentOrder_order1_M3.Value < 10) |
                        (currentOrder_order1_sheetType.SelectedIndex == 2 & currentOrder_order1_M4.Value < 10))
            {
                DialogResult dialogResult = MessageBox.Show("A medida da caixa está abaixo do mínimo permitido.", "Mensagem", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    currentOrder_order1_M3.Focus();

                }
            }



            else
            {
                return true;
            }

            return false;
        }
        private bool checkBottomOrderParameters()
        {
            if (currentOrder_levelSelector.SelectedIndex == 0)
            {


                if (currentOrder_order2_client.Text == "" | currentOrder_order2_client.Text == null)
                {
                    DialogResult dialogResult = MessageBox.Show("Por favor, preencher o nome do cliente.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_client.Focus();
                    }
                }

                else if (currentOrder_order2_product.Text == "" | currentOrder_order2_product.Text == null)
                {
                    DialogResult dialogResult = MessageBox.Show("Por favor, preencher o nome do produto.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_product.Focus();
                    }
                }

                else if (currentOrder_order2_sheetType.SelectedItem == null)
                {
                    DialogResult dialogResult = MessageBox.Show("Por favor, selecione o tipo de caixa desejado.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_sheetType.Focus();
                    }
                }

                else if (currentOrder_Order2_sheetQuantity.Value == 0)
                {
                    DialogResult dialogResult = MessageBox.Show("O numero de caixas do pedido deve ser maior que zero.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_Order2_sheetQuantity.Focus();
                    }
                }

                else if (currentOrder_order2_numberOfCuts.Value <= 10)
                {
                    DialogResult dialogResult = MessageBox.Show("Por favor, inserir o numero de cortes do pedido.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_numberOfCuts.Focus();
                    }
                }

                else if (currentOrder_order2_pileQuantity.Value <= 50)
                {
                    DialogResult dialogResult = MessageBox.Show("O numero de cortes da pilha deve ser maior que 50.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_pileQuantity.Focus();
                    }
                }

                else if (currentOrder_order2_sheetLenght.Value < 450)
                {
                    DialogResult dialogResult = MessageBox.Show("O comprimento da caixa deve ser maior que 450 Milímetros.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_sheetLenght.Focus();
                    }
                }

                else if (currentOrder_order2_sheetLenght.Value > 2800)
                {
                    DialogResult dialogResult = MessageBox.Show("O comprimento da caixa deve ser menor que 2800 Milímetros.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_sheetLenght.Focus();
                    }
                }

                else if (currentOrder_order2_sheetType.SelectedIndex == 0 & currentOrder_order2_M1.Value < 200)
                {
                    DialogResult dialogResult = MessageBox.Show("A medida da caixa está abaixo do mínimo permitido.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_M1.Focus();
                    }
                }

                else if (currentOrder_order2_sheetType.SelectedIndex == 1
                    & (currentOrder_order2_M1.Value + currentOrder_order2_M2.Value + currentOrder_order2_M3.Value) < 200)
                {
                    DialogResult dialogResult = MessageBox.Show("A medida da caixa está abaixo do mínimo permitido.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_M1.Focus();
                    }
                }

                else if ((currentOrder_order2_sheetType.SelectedIndex == 2 & currentOrder_order2_M3.Value < 10) |
                            (currentOrder_order2_sheetType.SelectedIndex == 2 & currentOrder_order2_M4.Value < 10))
                {
                    DialogResult dialogResult = MessageBox.Show("A medida da caixa está abaixo do mínimo permitido.", "Mensagem", MessageBoxButtons.OK);
                    if (dialogResult == DialogResult.OK)
                    {
                        currentOrder_order2_M3.Focus();
                    }
                }

                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }
        private void saveNewOrder()
        {
            try
            {
                productionListBindingSource.EndEdit();
                productionListBindingSource.ResetBindings(false);

                ProductionListPlc obj = productionListBindingSource.Current as ProductionListPlc;
                if (obj == null) return;

                // -----------------------------
                // Helpers (minimum + robust)
                // -----------------------------
                int ToInt(decimal v) => Convert.ToInt32(v);


                int MapSheetType(ComboBox cb)
                {
                    // PLC enum: 0=None, 1=Two Scorers, 2=Four Scorers
                    // Your combobox indices already match 0/1/2.
                    if (cb == null) return 0;
                    if (cb.SelectedIndex < 0) return 0;
                    if (cb.SelectedIndex > 2) return 2;
                    return cb.SelectedIndex;
                }

                int levelSelector = currentOrder_levelSelector.SelectedIndex;
                bool order2Enabled = (levelSelector == 3);

                using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                {
                    db.Open();

                    // -----------------------------------------
                    // Build parameters for dbo.ProductionList_Plc
                    // -----------------------------------------
                    //currentOrder_order1_id
                    var p = new DynamicParameters();

                    // Common / header
                    p.Add("@ProductionSequence", ToInt(nudProductionSequence.Value), DbType.Int32);
                    p.Add("@ProductionState", 0, DbType.Int32);
                    p.Add("@MachineNotRunningTime", 0, DbType.Int32);

                    // Use NULL for dates at creation (avoid "" string)
                    p.Add("@StartedAt", (DateTime?)null, DbType.DateTime);
                    p.Add("@FinishedAt", (DateTime?)null, DbType.DateTime);

                    p.Add("@PaperComposition", currentOrder_paperComposition.Text ?? "", DbType.String);
                    p.Add("@FluteType", currentOrder_fluteType.Text ?? "", DbType.String);
                    p.Add("@PaperWidth", ToInt(currentOrder_paperWidth.Value), DbType.Int32);

                    p.Add("@Paper1", currentOrder_paper1.Text ?? "", DbType.String);
                    p.Add("@Paper2", currentOrder_paper2.Text ?? "", DbType.String);
                    p.Add("@Paper3", currentOrder_paper3.Text ?? "", DbType.String);
                    p.Add("@Paper4", currentOrder_paper4.Text ?? "", DbType.String);
                    p.Add("@Paper5", currentOrder_paper5.Text ?? "", DbType.String);

                    p.Add("@ProductionListNumber", currentOrder_productionListNumber.Text ?? "", DbType.String);


                    // Order1
                    p.Add("@Order1Id", ToInt(currentOrder_order1_id.Value), DbType.Int32);
                    p.Add("@Order1Product", currentOrder_order1_product.Text ?? "", DbType.String);
                    p.Add("@Order1Client", currentOrder_order1_client.Text ?? "", DbType.String);

                    p.Add("@Order1SheetQuantity", ToInt(currentOrder_Order1_sheetQuantity.Value), DbType.Int32);
                    p.Add("@Order1SheetType", MapSheetType(currentOrder_order1_sheetType), DbType.Int32);

                    p.Add("@Order1M1", ToInt(currentOrder_order1_M1.Value), DbType.Int32);
                    p.Add("@Order1M2", ToInt(currentOrder_order1_M2.Value), DbType.Int32);
                    p.Add("@Order1M3", ToInt(currentOrder_order1_M3.Value), DbType.Int32);
                    p.Add("@Order1M4", ToInt(currentOrder_order1_M4.Value), DbType.Int32);
                    p.Add("@Order1M5", ToInt(currentOrder_order1_M5.Value), DbType.Int32);

                    p.Add("@Order1SheetLength", ToInt(currentOrder_order1_sheetLenght.Value), DbType.Int32);
                    p.Add("@Order1NumberOfCuts", ToInt(currentOrder_order1_numberOfCuts.Value), DbType.Int32);
                    p.Add("@Order1NumberOfCutsProduced", 0, DbType.Int32);
                    p.Add("@Order1PileQuantity", ToInt(currentOrder_order1_pileQuantity.Value), DbType.Int32);

                    // Selector
                    p.Add("@LevelSelector", ToInt(currentOrder_levelSelector.SelectedIndex), DbType.Int32);

                    // Order2 (only meaningful if LevelSelector == 3)
                    if (order2Enabled)
                    {
                        p.Add("@Order2Id", ToInt(currentOrder_order2_id.Value), DbType.Int32);
                        p.Add("@Order2Product", currentOrder_order2_product.Text ?? "", DbType.String);
                        p.Add("@Order2Client", currentOrder_order2_client.Text ?? "", DbType.String);

                        p.Add("@Order2SheetQuantity", ToInt(currentOrder_Order2_sheetQuantity.Value), DbType.Int32);
                        p.Add("@Order2SheetType", MapSheetType(currentOrder_order2_sheetType), DbType.Int32);

                        p.Add("@Order2M1", ToInt(currentOrder_order2_M1.Value), DbType.Int32);
                        p.Add("@Order2M2", ToInt(currentOrder_order2_M2.Value), DbType.Int32);
                        p.Add("@Order2M3", ToInt(currentOrder_order2_M3.Value), DbType.Int32);
                        p.Add("@Order2M4", ToInt(currentOrder_order2_M4.Value), DbType.Int32);
                        p.Add("@Order2M5", ToInt(currentOrder_order2_M5.Value), DbType.Int32);

                        p.Add("@Order2SheetLength", ToInt(currentOrder_order2_sheetLenght.Value), DbType.Int32);
                        p.Add("@Order2NumberOfCuts", ToInt(currentOrder_order2_numberOfCuts.Value), DbType.Int32);
                        p.Add("@Order2NumberOfCutsProduced", 0, DbType.Int32);
                        p.Add("@Order2PileQuantity", ToInt(currentOrder_order2_pileQuantity.Value), DbType.Int32);
                    }
                    else
                    {
                        // Clear Order2 to avoid stale data in DB when not enabled
                        p.Add("@Order2Id", (int?)null, DbType.Int32);
                        p.Add("@Order2Product", null, DbType.String);
                        p.Add("@Order2Client", null, DbType.String);

                        p.Add("@Order2SheetQuantity", (int?)null, DbType.Int32);
                        p.Add("@Order2SheetType", (int?)null, DbType.Int32);

                        p.Add("@Order2M1", (int?)null, DbType.Int32);
                        p.Add("@Order2M2", (int?)null, DbType.Int32);
                        p.Add("@Order2M3", (int?)null, DbType.Int32);
                        p.Add("@Order2M4", (int?)null, DbType.Int32);
                        p.Add("@Order2M5", (int?)null, DbType.Int32);

                        p.Add("@Order2SheetLength", (int?)null, DbType.Int32);
                        p.Add("@Order2NumberOfCuts", (int?)null, DbType.Int32);
                        p.Add("@Order2NumberOfCutsProduced", (int?)null, DbType.Int32);
                        p.Add("@Order2PileQuantity", (int?)null, DbType.Int32);
                    }

                    // -----------------------------------------
                    // Insert / Update
                    // -----------------------------------------
                    if (objState == EntityState.Added)
                    {
                        const string sqlInsert = @"
                        INSERT INTO dbo.ProductionList_Plc
                        (
                            ProductionSequence, ProductionState, MachineNotRunningTime,
                            StartedAt, FinishedAt,
                            PaperComposition, FluteType, PaperWidth,
                            Paper1, Paper2, Paper3, Paper4, Paper5,
                            ProductionListNumber,
                            Order1Id, Order1Product, Order1Client,
                            Order1SheetQuantity, Order1SheetType,
                            Order1M1, Order1M2, Order1M3, Order1M4, Order1M5,
                            Order1SheetLength, Order1NumberOfCuts, Order1NumberOfCutsProduced, Order1PileQuantity,
                            LevelSelector,
                            Order2Id, Order2Product, Order2Client,
                            Order2SheetQuantity, Order2SheetType,
                            Order2M1, Order2M2, Order2M3, Order2M4, Order2M5,
                            Order2SheetLength, Order2NumberOfCuts, Order2NumberOfCutsProduced, Order2PileQuantity
                        )
                        OUTPUT INSERTED.Id
                        VALUES
                        (
                            @ProductionSequence, @ProductionState, @MachineNotRunningTime,
                            @StartedAt, @FinishedAt,
                            @PaperComposition, @FluteType, @PaperWidth,
                            @Paper1, @Paper2, @Paper3, @Paper4, @Paper5,
                            @ProductionListNumber,
                            @Order1Id, @Order1Product, @Order1Client,
                            @Order1SheetQuantity, @Order1SheetType,
                            @Order1M1, @Order1M2, @Order1M3, @Order1M4, @Order1M5,
                            @Order1SheetLength, @Order1NumberOfCuts, @Order1NumberOfCutsProduced, @Order1PileQuantity,
                            @LevelSelector,
                            @Order2Id, @Order2Product, @Order2Client,
                            @Order2SheetQuantity, @Order2SheetType,
                            @Order2M1, @Order2M2, @Order2M3, @Order2M4, @Order2M5,
                            @Order2SheetLength, @Order2NumberOfCuts, @Order2NumberOfCutsProduced, @Order2PileQuantity
                        );";

                        int newId = db.ExecuteScalar<int>(sqlInsert, p);
                        obj.Id = newId;
                    }
                    else if (objState == EntityState.Changed)
                    {
                        p.Add("@Id", obj.Id, DbType.Int32);

                        const string sqlUpdate = @"
                        UPDATE dbo.ProductionList_Plc
                        SET
                            ProductionSequence = @ProductionSequence,
                            ProductionState = @ProductionState,
                            MachineNotRunningTime = @MachineNotRunningTime,
                            StartedAt = @StartedAt,
                            FinishedAt = @FinishedAt,
                            PaperComposition = @PaperComposition,
                            FluteType = @FluteType,
                            PaperWidth = @PaperWidth,
                            Paper1 = @Paper1,
                            Paper2 = @Paper2,
                            Paper3 = @Paper3,
                            Paper4 = @Paper4,
                            Paper5 = @Paper5,
                            ProductionListNumber = @ProductionListNumber,

                            Order1Id = @Order1Id,
                            Order1Product = @Order1Product,
                            Order1Client = @Order1Client,
                            Order1SheetQuantity = @Order1SheetQuantity,
                            Order1SheetType = @Order1SheetType,
                            Order1M1 = @Order1M1,
                            Order1M2 = @Order1M2,
                            Order1M3 = @Order1M3,
                            Order1M4 = @Order1M4,
                            Order1M5 = @Order1M5,
                            Order1SheetLength = @Order1SheetLength,
                            Order1NumberOfCuts = @Order1NumberOfCuts,
                            Order1NumberOfCutsProduced = @Order1NumberOfCutsProduced,
                            Order1PileQuantity = @Order1PileQuantity,

                            LevelSelector = @LevelSelector,

                            Order2Id = @Order2Id,
                            Order2Product = @Order2Product,
                            Order2Client = @Order2Client,
                            Order2SheetQuantity = @Order2SheetQuantity,
                            Order2SheetType = @Order2SheetType,
                            Order2M1 = @Order2M1,
                            Order2M2 = @Order2M2,
                            Order2M3 = @Order2M3,
                            Order2M4 = @Order2M4,
                            Order2M5 = @Order2M5,
                            Order2SheetLength = @Order2SheetLength,
                            Order2NumberOfCuts = @Order2NumberOfCuts,
                            Order2NumberOfCutsProduced = @Order2NumberOfCutsProduced,
                            Order2PileQuantity = @Order2PileQuantity
                        WHERE Id = @Id;";

                        db.Execute(sqlUpdate, p);
                    }

                    dtgOrders.Refresh();
                    objState = EntityState.Unchanged;

                    // Reload list from NEW table
                    productionListBindingSource.DataSource = db.Query<ProductionListPlc>(
                        "SELECT TOP 100 * FROM dbo.ProductionList_Plc WHERE ProductionState < 2 ORDER BY CASE WHEN ProductionSequence > 0 THEN 0 ELSE 1 END, ProductionSequence ASC",
                        commandType: CommandType.Text
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            gpbProductionList.Enabled = false;
            productionListBindingSource.ResetBindings(false);
            LoadDatabaseItens();
        }
        private void btnDelete_Click(object sender, EventArgs e)
        {
            objState = EntityState.Deleted;


            DialogResult dialogResult = MessageBox.Show("Deseja excluir este item?", "Mensagem", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {

                try

                {
                    ProductionListPlc obj = productionListBindingSource.Current as ProductionListPlc;
                    if (obj != null)
                    {

                        using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                        {
                            if (db.State == ConnectionState.Closed)
                            {
                                db.Open();
                                int result = db.Execute("delete from dbo.ProductionList_Plc where ID = @Id ", new { ID = obj.Id }, commandType: CommandType.Text);
                                if (result != 0)
                                {
                                    productionListBindingSource.RemoveCurrent();
                                    objState = EntityState.Unchanged;
                                    productionListBindingSource.DataSource = db.Query<ProductionListPlc>("select top 100 * from dbo.ProductionList_Plc", commandType: CommandType.Text);
                                }
                            }

                            if (db.State == ConnectionState.Closed)
                            {
                                db.Close();
                            }
                        }


                    }

                    LoadDatabaseItens();
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void cbTopOrderSheetType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            TopOrderBoxType(comboBox.SelectedIndex);

            //Reset the values if the combobox changes only when a new order is added
            if (objState == EntityState.Added | objState == EntityState.Changed)
            {
                currentOrder_order1_M1.Value = 0;
                currentOrder_order1_M2.Value = 0;
                currentOrder_order1_M3.Value = 0;
                currentOrder_order1_M4.Value = 0;
                currentOrder_order1_M5.Value = 0;
                productionListBindingSource.EndEdit();
            }
        }
        private void cbBottomOrderSheetType_SelectedIndexChanged(object sender, EventArgs e)
        {
            //ComboBox comboBox = sender as ComboBox;

            //if (cbSecondOrderEnabled.SelectedIndex == 0)
            //{
            //    BottomOrderBoxType(comboBox.SelectedIndex);
            //}
            //else
            //{
            //    //SecondOderNotEnabled();
            //}

            ////Reset the values if the combobox changes
            //if (objState == EntityState.Added | objState == EntityState.Changed)
            //{
            //    nudBottomOrderSheetM1.Value = 0;
            //    nudBottomOrderSheetM2.Value = 0;
            //    nudBottomOrderSheetM3.Value = 0;
            //    nudBottomOrderSheetM4.Value = 0;
            //    nudBottomOrderSheetM5.Value = 0;
            //    productionListBindingSource.EndEdit();
            //}

            BottomOrderBoxType(currentOrder_order2_sheetType.SelectedIndex);
        }
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            objState = EntityState.Changed;
            gpbProductionList.Enabled = true;
        }
        private void UpdateAutoCompleteList()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        db.Open();

                        // NOTE: New table/columns: dbo.ProductionList_Plc + Order1/Order2 naming
                        clientsBindingSource.DataSource = db.Query<ClientsList>(
                            "(SELECT Order1Client AS TopOrderClient FROM dbo.ProductionList_Plc WHERE Order1Client IS NOT NULL) " +
                            "UNION " +
                            "(SELECT Order2Client AS TopOrderClient FROM dbo.ProductionList_Plc WHERE Order2Client IS NOT NULL)",
                            commandType: CommandType.Text);

                        paperBindingSource.DataSource = db.Query<PaperList>(
                            "(SELECT Paper1 FROM dbo.ProductionList_Plc WHERE Paper1 IS NOT NULL) " +
                            "UNION (SELECT Paper2 FROM dbo.ProductionList_Plc WHERE Paper2 IS NOT NULL) " +
                            "UNION (SELECT Paper3 FROM dbo.ProductionList_Plc WHERE Paper3 IS NOT NULL) " +
                            "UNION (SELECT Paper4 FROM dbo.ProductionList_Plc WHERE Paper4 IS NOT NULL) " +
                            "UNION (SELECT Paper5 FROM dbo.ProductionList_Plc WHERE Paper5 IS NOT NULL)",
                            commandType: CommandType.Text);

                        paperCompositionBindingSource.DataSource = db.Query<PaperCompositionList>(
                            "SELECT PaperComposition FROM dbo.ProductionList_Plc WHERE PaperComposition IS NOT NULL",
                            commandType: CommandType.Text);

                        productBindingSource.DataSource = db.Query<ProductList>(
                            "(SELECT Order1Product AS TopOrderProduct FROM dbo.ProductionList_Plc WHERE Order1Product IS NOT NULL) " +
                            "UNION " +
                            "(SELECT Order2Product AS TopOrderProduct FROM dbo.ProductionList_Plc WHERE Order2Product IS NOT NULL)",
                            commandType: CommandType.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var autoCompleteClients = new AutoCompleteStringCollection();
                var autoCompleteProducts = new AutoCompleteStringCollection();
                var autoCompletePaperComposition = new AutoCompleteStringCollection();
                var autoCompletePaper = new AutoCompleteStringCollection();

                // Clients
                for (int i = 0; i < clientsBindingSource.List.Count; ++i)
                {
                    ClientsList obj = clientsBindingSource.List[i] as ClientsList;
                    if (!string.IsNullOrWhiteSpace(obj?.TopOrderClient))
                        autoCompleteClients.Add(obj.TopOrderClient);
                }

                // Products
                for (int i = 0; i < productBindingSource.List.Count; ++i)
                {
                    ProductList obj = productBindingSource.List[i] as ProductList;
                    if (!string.IsNullOrWhiteSpace(obj?.TopOrderProduct))
                        autoCompleteProducts.Add(obj.TopOrderProduct);
                }

                // Paper
                for (int i = 0; i < paperBindingSource.List.Count; ++i)
                {
                    PaperList obj = paperBindingSource.List[i] as PaperList;
                    if (!string.IsNullOrWhiteSpace(obj?.Paper1))
                        autoCompletePaper.Add(obj.Paper1);
                }

                // Composition
                for (int i = 0; i < paperCompositionBindingSource.List.Count; ++i)
                {
                    PaperCompositionList obj = paperCompositionBindingSource.List[i] as PaperCompositionList;
                    if (!string.IsNullOrWhiteSpace(obj?.PaperComposition))
                        autoCompletePaperComposition.Add(obj.PaperComposition);
                }

                // Apply to UI
                currentOrder_order1_client.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_order1_client.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_order1_client.AutoCompleteCustomSource = autoCompleteClients;

                currentOrder_order2_client.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_order2_client.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_order2_client.AutoCompleteCustomSource = autoCompleteClients;

                currentOrder_paper1.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_paper1.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_paper1.AutoCompleteCustomSource = autoCompletePaper;

                currentOrder_paper2.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_paper2.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_paper2.AutoCompleteCustomSource = autoCompletePaper;

                currentOrder_paper3.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_paper3.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_paper3.AutoCompleteCustomSource = autoCompletePaper;

                currentOrder_paper4.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_paper4.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_paper4.AutoCompleteCustomSource = autoCompletePaper;

                currentOrder_paper5.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_paper5.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_paper5.AutoCompleteCustomSource = autoCompletePaper;

                currentOrder_paperComposition.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_paperComposition.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_paperComposition.AutoCompleteCustomSource = autoCompletePaperComposition;

                currentOrder_order1_product.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_order1_product.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_order1_product.AutoCompleteCustomSource = autoCompleteProducts;

                currentOrder_order2_product.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                currentOrder_order2_product.AutoCompleteSource = AutoCompleteSource.CustomSource;
                currentOrder_order2_product.AutoCompleteCustomSource = autoCompleteProducts;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            objState = EntityState.Unchanged;
            gpbProductionList.Enabled = false;
            productionListBindingSource.ResetBindings(false);
            LoadDatabaseItens();
        }
        private void nudEnter(object sender, EventArgs e)
        {
            (sender as NumericUpDown).Select(0, Text.Length);

        }

        private void nudLeave(object sender, EventArgs e)
        {
            SendKeys.Send("{ENTER}");
        }

        private void nudKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void nudKeyPress(object sender, KeyPressEventArgs e)
        {
            //Go to the next control when enter key is pressed
            if (e.KeyChar == (char)13)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void texboxKeyDown(object sender, KeyEventArgs e)
        {
            //Go to the next control when enter key is pressed
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }

        }

        private void comboBoxKeyDown(object sender, KeyEventArgs e)
        {
            //Go to the next control when enter key is pressed
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private static bool Order1Enabled(int? levelSelector)
        {
            // Per your rule: 1 or 2 = Order1 only; 3 = Order2 enabled (Order1 still active)
            // Therefore Order1 is always enabled (fallback included).
            return true;
        }

        private static bool Order2Enabled(int? levelSelector)
        {
            return levelSelector.GetValueOrDefault(1) == 3;
        }

        private void UpdateOrderDescription()
        {
            try
            {
                // Step 1: Retrieve the current data from the productionListBindingSource.DataSource
                List<ProductionListPlc> productionList = (List<ProductionListPlc>)productionListBindingSource.DataSource;

                // Step 2: Loop through the list and update each item
                foreach (ProductionListPlc production in productionList)
                {
                    production.Order1Description = BuildOrder1Description(production);
                    production.Order2Description = BuildOrder2Description(production);
                    production.OrderDetails = BuildOrderDetailDescription(production);
                }

                // Step 3: Update the productionListBindingSource.DataSource with the modified data
                productionListBindingSource.DataSource = productionList;

                // Step 4: Notify any UI components to refresh their data display
                productionListBindingSource.ResetBindings(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string BuildOrder1Description(ProductionListPlc production)
        {
            string order1Description = "";

            // Upper should be Order 1
            if (!Order1Enabled(production.LevelSelector))
            {
                order1Description = "DESABILITADO";
            }
            else
            {
                order1Description = "CLIENTE: " + (production.Order1Client ?? "")
                                  + "\n"
                                  + "PRODUTO: " + (production.Order1Product ?? "")
                                  + "\n"
                                  + "Nº DA OF: " + production.Order1Id.GetValueOrDefault(0).ToString()
                                  + "\n"
                                  + "MEDIDA: " + production.Order1SheetQuantity.GetValueOrDefault(0).ToString();

                // Keep your current text-based rule with minimal changes
                // NOTE: if Order1SheetType is INT in your new table, this will need to be updated.
                if ((production.Order1SheetType ?? 0) == 0) // assume 0 = sem vincos (placeholder if int)
                {
                    order1Description += " x " + production.Order1M1.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "LARGURA: " + (production.Order1M1.GetValueOrDefault(0)
                                                     + production.Order1M2.GetValueOrDefault(0)
                                                     + production.Order1M3.GetValueOrDefault(0)
                                                     + production.Order1M4.GetValueOrDefault(0)).ToString()
                                       + "\n"
                                       + "COMPRIMENTO: " + production.Order1SheetLength.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "Nº DE CORTES: " + production.Order1NumberOfCuts.GetValueOrDefault(0).ToString();
                }
                else if ((production.Order1SheetType ?? 0) == 1) // assume 1 = dois vincos
                {
                    order1Description += " x " + production.Order1M1.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order1M2.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order1M3.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "LARGURA: " + (production.Order1M1.GetValueOrDefault(0)
                                                     + production.Order1M2.GetValueOrDefault(0)
                                                     + production.Order1M3.GetValueOrDefault(0)
                                                     + production.Order1M4.GetValueOrDefault(0)).ToString()
                                       + "\n"
                                       + "COMPRIMENTO: " + production.Order1SheetLength.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "Nº DE CORTES: " + production.Order1NumberOfCuts.GetValueOrDefault(0).ToString();
                }
                else if ((production.Order1SheetType ?? 0) == 2) // assume 2 = quatro vincos
                {
                    order1Description += " x " + production.Order1M1.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order1M2.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order1M3.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order1M4.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order1M5.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "LARGURA: " + (production.Order1M1.GetValueOrDefault(0)
                                                     + production.Order1M2.GetValueOrDefault(0)
                                                     + production.Order1M3.GetValueOrDefault(0)
                                                     + production.Order1M4.GetValueOrDefault(0)).ToString()
                                       + "\n"
                                       + "COMPRIMENTO: " + production.Order1SheetLength.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "Nº DE CORTES: " + production.Order1NumberOfCuts.GetValueOrDefault(0).ToString();
                }
            }

            return order1Description;
        }

        private string BuildOrder2Description(ProductionListPlc production)
        {
            string order2Description;

            // Lower should be Order 2
            if (!Order2Enabled(production.LevelSelector))
            {
                order2Description = "DESABILITADO";
            }
            else
            {
                order2Description = "CLIENTE: " + (production.Order2Client ?? "")
                                  + "\n"
                                  + "PRODUTO: " + (production.Order2Product ?? "")
                                  + "\n"
                                  + "Nº DA OF: " + production.Order2Id.GetValueOrDefault(0).ToString()
                                  + "\n"
                                  + "MEDIDA: " + production.Order2SheetQuantity.GetValueOrDefault(0).ToString();

                if ((production.Order2SheetType ?? 0) == 0) // 0 = sem vincos (placeholder if int)
                {
                    order2Description += " x " + production.Order2M1.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "LARGURA: " + (production.Order2M1.GetValueOrDefault(0)
                                                     + production.Order2M2.GetValueOrDefault(0)
                                                     + production.Order2M3.GetValueOrDefault(0)
                                                     + production.Order2M4.GetValueOrDefault(0)).ToString()
                                       + "\n"
                                       + "COMPRIMENTO: " + production.Order2SheetLength.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "Nº DE CORTES: " + production.Order2NumberOfCuts.GetValueOrDefault(0).ToString();
                }
                else if ((production.Order2SheetType ?? 0) == 1) // 1 = dois vincos
                {
                    order2Description += " x " + production.Order2M1.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order2M2.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order2M3.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "LARGURA: " + (production.Order2M1.GetValueOrDefault(0)
                                                     + production.Order2M2.GetValueOrDefault(0)
                                                     + production.Order2M3.GetValueOrDefault(0)
                                                     + production.Order2M4.GetValueOrDefault(0)).ToString()
                                       + "\n"
                                       + "COMPRIMENTO: " + production.Order2SheetLength.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "Nº DE CORTES: " + production.Order2NumberOfCuts.GetValueOrDefault(0).ToString();
                }
                else if ((production.Order2SheetType ?? 0) == 2) // 2 = quatro vincos
                {
                    order2Description += " x " + production.Order2M1.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order2M2.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order2M3.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order2M4.GetValueOrDefault(0).ToString()
                                       + "*" + production.Order2M5.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "LARGURA: " + (production.Order2M1.GetValueOrDefault(0)
                                                     + production.Order2M2.GetValueOrDefault(0)
                                                     + production.Order2M3.GetValueOrDefault(0)
                                                     + production.Order2M4.GetValueOrDefault(0)).ToString()
                                       + "\n"
                                       + "COMPRIMENTO: " + production.Order2SheetLength.GetValueOrDefault(0).ToString()
                                       + "\n"
                                       + "Nº DE CORTES: " + production.Order2NumberOfCuts.GetValueOrDefault(0).ToString();
                }
            }

            return order2Description;
        }

        private string BuildOrderDetailDescription(ProductionListPlc production)
        {
            string orderDetails;
            int order1Width;
            int order2Width;
            int paperTrimSize;

            order1Width = (production.Order1M1.GetValueOrDefault(0)
                         + production.Order1M2.GetValueOrDefault(0)
                         + production.Order1M3.GetValueOrDefault(0)
                         + production.Order1M4.GetValueOrDefault(0)
                         + production.Order1M5.GetValueOrDefault(0)) * production.Order1SheetQuantity.GetValueOrDefault(0);

            order2Width = 0;
            if (Order2Enabled(production.LevelSelector))
            {
                order2Width = (production.Order2M1.GetValueOrDefault(0)
                             + production.Order2M2.GetValueOrDefault(0)
                             + production.Order2M3.GetValueOrDefault(0)
                             + production.Order2M4.GetValueOrDefault(0)
                             + production.Order2M5.GetValueOrDefault(0)) * production.Order2SheetQuantity.GetValueOrDefault(0);
            }

            int paperWidth = production.PaperWidth.GetValueOrDefault(0);
            paperTrimSize = paperWidth - (order1Width + order2Width);

            double paperTrimPercentage = (paperWidth > 0)
                ? ((double)paperTrimSize / paperWidth) * 100.0
                : 0.0;

            paperTrimPercentage = Math.Round(paperTrimPercentage, 2);

            orderDetails = "FORMATO: " + paperWidth + " MM" + " => ONDA: " + (production.FluteType ?? "")
                         + "\n"
                         + "LARGURA TOTAL: " + (order1Width + order2Width) + " MM"
                         + "\n"
                         + "REFILE LARGURA: " + "2 * " + (paperTrimSize / 2) + " MM"
                         + "\n"
                         + "REFILE PERCENTUAL: " + paperTrimPercentage + "%"
                         + "\n"
                         + "COMPOSIÇÃO: " + (production.PaperComposition ?? "")
                         + "\n"
                         + "METRO LINEAR: " + ((production.Order1SheetLength.GetValueOrDefault(0) * production.Order1NumberOfCuts.GetValueOrDefault(0)) * 0.001).ToString();

            return orderDetails;
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                DataGridView dataGridView = (DataGridView)sender;
                int productionSequenceColumnIndex = dataGridView.Columns["ProductionSequence"].Index;

                if (productionSequenceColumnIndex == e.ColumnIndex)
                {
                    // Check the value of ProductionSequence for the current row
                    int productionSequenceValue = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);

                    // Check if the value is not equal to 0
                    if (productionSequenceValue != 0)
                    {
                        // Set the row's background color to light green
                        //dataGridView.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
                        //dataGridView.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = Color.LightYellow;
                    }
                }
            }
        }

        private void frmOrders_Enter(object sender, EventArgs e)
        {
            LoadDatabaseItens();

        }

        private void productionListBindingSource_CurrentChanged(object sender, EventArgs e)
        {

        }

        // =====================================================================
        // VISIBILITY ENGINE (LevelSelector + SheetType => show/hide + reset)
        // =====================================================================
        private void SetupVisibilityLogic()
        {
            // Prevent double subscription
            currentOrder_levelSelector.SelectionChangeCommitted -= currentOrder_levelSelector_SelectionChangeCommitted;
            currentOrder_levelSelector.SelectionChangeCommitted += currentOrder_levelSelector_SelectionChangeCommitted;

            currentOrder_order1_sheetType.SelectionChangeCommitted -= currentOrder_order_sheetType_SelectionChangeCommitted;
            currentOrder_order1_sheetType.SelectionChangeCommitted += currentOrder_order_sheetType_SelectionChangeCommitted;

            currentOrder_order2_sheetType.SelectionChangeCommitted -= currentOrder_order_sheetType_SelectionChangeCommitted;
            currentOrder_order2_sheetType.SelectionChangeCommitted += currentOrder_order_sheetType_SelectionChangeCommitted;

            productionListBindingSource.CurrentChanged -= productionListBindingSource_CurrentChanged_Visibility;
            productionListBindingSource.CurrentChanged += productionListBindingSource_CurrentChanged_Visibility;
        }

        private void productionListBindingSource_CurrentChanged_Visibility(object sender, EventArgs e)
        {
            // When navigating rows in the grid, the bindings update SelectedIndex etc.
            // Apply rules so UI matches the selected record.
            ApplyAllVisibilityRules();
        }

        private void currentOrder_levelSelector_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ApplyAllVisibilityRules();
        }

        private void currentOrder_order_sheetType_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ApplyAllVisibilityRules();
        }

        private void ApplyAllVisibilityRules()
        {
            if (_applyingVisibilityRules) return;

            _applyingVisibilityRules = true;
            SuspendLayout();

            try
            {
                int levelSelectorIdx = GetLevelSelectorIndexSafe();

                // 1) Show/hide entire Order blocks
                ApplyLevelSelectorVisibility(levelSelectorIdx);

                // 2) Apply sheetType rules to M fields for the orders that are visible
                ApplySheetTypeVisibilityForOrder(1, GetSheetTypeSafe(currentOrder_order1_sheetType));
                ApplySheetTypeVisibilityForOrder(2, GetSheetTypeSafe(currentOrder_order2_sheetType));
            }
            finally
            {
                ResumeLayout(true);
                _applyingVisibilityRules = false;
            }
        }

        /// <summary>
        /// Your LevelSelector is bound to SelectedIndex:
        /// 0 = blank, 1 = SUPERIOR, 2 = INFERIOR, 3 = AMBOS
        /// </summary>
        private int GetLevelSelectorIndexSafe()
        {
            int idx = currentOrder_levelSelector?.SelectedIndex ?? 0;
            if (idx < 0) idx = 0;
            if (idx > 3) idx = 3;
            return idx;
        }

        private int GetSheetTypeSafe(ComboBox cb)
        {
            int idx = cb?.SelectedIndex ?? 0;
            if (idx < 0) idx = 0;
            if (idx > 2) idx = 2;
            return idx;
        }

        private void ApplyLevelSelectorVisibility(int levelSelectorIndex)
        {
            // 0 blank => treat as single order (Order1 only)
            if (levelSelectorIndex < 0) levelSelectorIndex = 0;

            bool showOrder2 = (levelSelectorIndex == 3); // ONLY when AMBOS
            bool showOrder1 = true;                      // Always show Order1

            SetOrder1ControlsVisible(showOrder1);
            SetOrder2ControlsVisible(showOrder2);

            if (!showOrder2) ResetOrder2Controls();
        }


        private void ApplySheetTypeVisibilityForOrder(int orderNumber, int sheetType)
        {
            // If order is hidden, do nothing
            if (orderNumber == 1 && !IsOrder1Visible()) return;
            if (orderNumber == 2 && !IsOrder2Visible()) return;

            // sheetType mapping:
            // 0 => M1 only
            // 1 => M1..M3
            // 2 => M1..M5
            bool showM1 = true;
            bool showM2 = sheetType >= 1;
            bool showM3 = sheetType >= 1;
            bool showM4 = sheetType >= 2;
            bool showM5 = sheetType >= 2;

            if (orderNumber == 1)
            {
                SetVisibleSafe(currentOrder_order1_M1, lbl_currentOrder_order1_M1, showM1);
                SetVisibleSafe(currentOrder_order1_M2, lbl_currentOrder_order1_M2, showM2);
                SetVisibleSafe(currentOrder_order1_M3, lbl_currentOrder_order1_M3, showM3);
                SetVisibleSafe(currentOrder_order1_M4, lbl_currentOrder_order1_M4, showM4);
                SetVisibleSafe(currentOrder_order1_M5, lbl_currentOrder_order1_M5, showM5);

                ResetHiddenMs(sheetType,
                    currentOrder_order1_M1, currentOrder_order1_M2, currentOrder_order1_M3, currentOrder_order1_M4, currentOrder_order1_M5);
            }
            else
            {
                SetVisibleSafe(currentOrder_order2_M1, lbl_currentOrder_order2_M1, showM1);
                SetVisibleSafe(currentOrder_order2_M2, lbl_currentOrder_order2_M2, showM2);
                SetVisibleSafe(currentOrder_order2_M3, lbl_currentOrder_order2_M3, showM3);
                SetVisibleSafe(currentOrder_order2_M4, lbl_currentOrder_order2_M4, showM4);
                SetVisibleSafe(currentOrder_order2_M5, lbl_currentOrder_order2_M5, showM5);

                ResetHiddenMs(sheetType,
                    currentOrder_order2_M1, currentOrder_order2_M2, currentOrder_order2_M3, currentOrder_order2_M4, currentOrder_order2_M5);
            }
        }

        private bool IsOrder1Visible()
        {
            // pick a representative control that always exists in the block
            return currentOrder_order1_product?.Visible == true;
        }

        private bool IsOrder2Visible()
        {
            return currentOrder_order2_product?.Visible == true;
        }

        private void ResetHiddenMs(int sheetType, NumericUpDown m1, NumericUpDown m2, NumericUpDown m3, NumericUpDown m4, NumericUpDown m5)
        {
            // IMPORTANT:
            // In frmOrders, when browsing existing DB rows, you may NOT want to overwrite values.
            // However, your requirement for "visibility control methods" included clearing hidden fields.
            // If you prefer not to clear when browsing, gate this with (objState == Added || Changed).
            bool canReset = (objState == EntityState.Added) || (objState == EntityState.Changed);
            if (!canReset) return;

            if (sheetType <= 0)
            {
                SetNudSafe(m2, 0);
                SetNudSafe(m3, 0);
                SetNudSafe(m4, 0);
                SetNudSafe(m5, 0);
            }
            else if (sheetType == 1)
            {
                SetNudSafe(m4, 0);
                SetNudSafe(m5, 0);
            }
        }

        // Helper: safe visible setter
        private static void SetVisibleSafe(Control c, Control label, bool visible)
        {
            if (c != null) c.Visible = visible;
            if (label != null) label.Visible = visible;
        }

        private static void SetNudSafe(NumericUpDown nud, decimal value)
        {
            if (nud == null) return;

            if (value < nud.Minimum) value = nud.Minimum;
            if (value > nud.Maximum) value = nud.Maximum;

            nud.Value = value;
        }


        private void SetOrder1ControlsVisible(bool visible)
        {
            SetVisibleSafe(currentOrder_Order1_sheetQuantity, lbl_currentOrder_Order1_sheetQuantity, visible);
            SetVisibleSafe(currentOrder_order1_id, lbl_currentOrder_order1_id, visible);
            SetVisibleSafe(currentOrder_order1_product, lbl_currentOrder_order1_product, visible);
            SetVisibleSafe(currentOrder_order1_client, lbl_currentOrder_order1_client, visible);
            SetVisibleSafe(currentOrder_order1_sheetType, lbl_currentOrder_order1_sheetType, visible);

            // M fields handled again by sheetType rules (but keep visible baseline here)
            SetVisibleSafe(currentOrder_order1_M1, lbl_currentOrder_order1_M1, visible);
            SetVisibleSafe(currentOrder_order1_M2, lbl_currentOrder_order1_M2, visible);
            SetVisibleSafe(currentOrder_order1_M3, lbl_currentOrder_order1_M3, visible);
            SetVisibleSafe(currentOrder_order1_M4, lbl_currentOrder_order1_M4, visible);
            SetVisibleSafe(currentOrder_order1_M5, lbl_currentOrder_order1_M5, visible);

            SetVisibleSafe(currentOrder_order1_sheetLenght, lbl_currentOrder_order1_sheetLenght, visible);
            SetVisibleSafe(currentOrder_order1_numberOfCuts, lbl_currentOrder_order1_numberOfCuts, visible);
            SetVisibleSafe(currentOrder_order1_pileQuantity, lbl_currentOrder_order1_pileQuantity, visible);
        }

        private void SetOrder2ControlsVisible(bool visible)
        {
            SetVisibleSafe(currentOrder_Order2_sheetQuantity, lbl_currentOrder_Order2_sheetQuantity, visible);
            SetVisibleSafe(currentOrder_order2_id, lbl_currentOrder_order2_id, visible);
            SetVisibleSafe(currentOrder_order2_product, lbl_currentOrder_order2_product, visible);
            SetVisibleSafe(currentOrder_order2_client, lbl_currentOrder_order2_client, visible);
            SetVisibleSafe(currentOrder_order2_sheetType, lbl_currentOrder_order2_sheetType, visible);

            SetVisibleSafe(currentOrder_order2_M1, lbl_currentOrder_order2_M1, visible);
            SetVisibleSafe(currentOrder_order2_M2, lbl_currentOrder_order2_M2, visible);
            SetVisibleSafe(currentOrder_order2_M3, lbl_currentOrder_order2_M3, visible);
            SetVisibleSafe(currentOrder_order2_M4, lbl_currentOrder_order2_M4, visible);
            SetVisibleSafe(currentOrder_order2_M5, lbl_currentOrder_order2_M5, visible);

            SetVisibleSafe(currentOrder_order2_sheetLenght, lbl_currentOrder_order2_sheetLenght, visible);
            SetVisibleSafe(currentOrder_order2_numberOfCuts, lbl_currentOrder_order2_numberOfCuts, visible);

            // Missing ones you requested previously
            SetVisibleSafe(currentOrder_order2_pileQuantity, lbl_currentOrder_order2_pileQuantity, visible);

        }

        private void ResetOrder1Controls()
        {
            if (currentOrder_order1_product != null) currentOrder_order1_product.Text = "";
            if (currentOrder_order1_client != null) currentOrder_order1_client.Text = "";

            SetNudSafe(currentOrder_Order1_sheetQuantity, 0);
            SetNudSafe(currentOrder_order1_sheetLenght, 0);

            SetNudSafe(currentOrder_order1_M1, 0);
            SetNudSafe(currentOrder_order1_M2, 0);
            SetNudSafe(currentOrder_order1_M3, 0);
            SetNudSafe(currentOrder_order1_M4, 0);
            SetNudSafe(currentOrder_order1_M5, 0);

            SetNudSafe(currentOrder_order1_numberOfCuts, 0);

            SetNudSafe(currentOrder_order1_pileQuantity, 0);
        }

        private void ResetOrder2Controls()
        {
            if (currentOrder_order2_product != null) currentOrder_order2_product.Text = "";
            if (currentOrder_order2_client != null) currentOrder_order2_client.Text = "";

            SetNudSafe(currentOrder_Order2_sheetQuantity, 0);
            SetNudSafe(currentOrder_order2_sheetLenght, 0);

            SetNudSafe(currentOrder_order2_M1, 0);
            SetNudSafe(currentOrder_order2_M2, 0);
            SetNudSafe(currentOrder_order2_M3, 0);
            SetNudSafe(currentOrder_order2_M4, 0);
            SetNudSafe(currentOrder_order2_M5, 0);
            SetNudSafe(currentOrder_order2_numberOfCuts, 0);
            SetNudSafe(currentOrder_order2_pileQuantity, 0);
        }


    }
}

