using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using Hmi.Data;

namespace Hmi
{
    public partial class frmHome : Form
    {
        public static frmHome instance;

        // ---------------------------------------------------------------------
        // CULTURE / FORMATS
        // ---------------------------------------------------------------------
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
        private const string PlcDateTimeFormat = "yyyy-MM-dd HH:mm:ss";      // PLC / DB style
        private const string UiDateTimeFormat = "dd/MM/yyyy HH:mm:ss";      // PT-BR display

        // ---------------------------------------------------------------------
        // UI references (legacy)
        // ---------------------------------------------------------------------
        public NumericUpDown CurrentOrderLinearMeters;
        public NumericUpDown CurrentOrderLinearMetersProduced;
        public NumericUpDown CurrentOrderLinearMetersRemaining;

        public NumericUpDown TopOrderNumberOfCutsProduced;
        public NumericUpDown TopOrderNumberOfCutsRemaing;
        public NumericUpDown TopOrderPileCounter;

        public NumericUpDown BottomOrderNumberOfCutsProduced;
        public NumericUpDown BottomOrderNumberOfCutsRemaing;
        public NumericUpDown BottomOrderPileCounter;

        // ---------------------------------------------------------------------
        // Bindings
        // ---------------------------------------------------------------------
        private readonly BindingSource _currentOrderBs = new BindingSource();

        public frmHome()
        {
            InitializeComponent();
            instance = this;

            // Legacy references (if frmMain still writes to these)
            CurrentOrderLinearMeters = currentOrder_linearMeters;
            CurrentOrderLinearMetersProduced = currentOrder_linearMetersProduced;
            CurrentOrderLinearMetersRemaining = currentOrder_linearMetersRemaining;

            TopOrderNumberOfCutsProduced = currentOrder_order1_numberOfCutsProduced;
            TopOrderNumberOfCutsRemaing = currentOrder_order1_numberOfCutsRemaining;
            TopOrderPileCounter = currentOrder_order1_pileQuantityProduced;

            BottomOrderNumberOfCutsProduced = currentOrder_order2_numberOfCutsProduced;
            BottomOrderNumberOfCutsRemaing = currentOrder_order2_numberOfCutsRemaining;
            BottomOrderPileCounter = currentOrder_order2_pileQuantityProduced;
        }

        // =====================================================================
        // FORM LIFECYCLE
        // =====================================================================
        private void frmHome_Load(object sender, EventArgs e)
        {
            // Subscribe events once
            if (frmMain.instance != null)
            {
                frmMain.instance.DatabaseItemsReloaded += OnDatabaseItemsReloaded;
                frmMain.instance.CurrentOrderUpdated += FrmMain_CurrentOrderUpdated;
            }

            InitializeUiDefaults();

            SetupUiLogicForLevelAndSheetType();

            SetupCurrentOrderBindings();

            LoadDatabaseItens();

            // Apply once (in case model already exists)
            ApplyAllVisibilityRules();
            UpdateElapsedTime();
            UpdateEstimatedTimeToFinishSafe();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (frmMain.instance != null)
            {
                frmMain.instance.CurrentOrderUpdated -= FrmMain_CurrentOrderUpdated;
                frmMain.instance.DatabaseItemsReloaded -= OnDatabaseItemsReloaded;
            }
            base.OnFormClosed(e);
        }

        private void InitializeUiDefaults()
        {
            if (currentOrder_fluteType != null && currentOrder_fluteType.Items.Count > 0)
                currentOrder_fluteType.SelectedIndex = 0;
        }

        // =====================================================================
        // DATABASE (NEXT ORDERS)
        // =====================================================================
        public void LoadDatabaseItens()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                {
                    if (db.State == ConnectionState.Closed)
                        db.Open();

                    var list = db.Query<ProductionListPlc>(
                        "SELECT TOP 100 * FROM dbo.ProductionList_Plc WHERE (ProductionState < 1) AND (ProductionSequence > 0) ORDER BY ProductionSequence ASC",
                        commandType: CommandType.Text).ToList();

                    nextOrderBindingSource.DataSource = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateOrderDescription();
        }

        private void OnDatabaseItemsReloaded()
        {
            LoadDatabaseItens();
        }

        // =====================================================================
        // PLC -> UI UPDATE
        // =====================================================================
        private void FrmMain_CurrentOrderUpdated(order_TypeStruct snapshot)
        {
            if (snapshot == null) return;

            SafeUi(() =>
            {
                _currentOrderBs.DataSource = snapshot;
                _currentOrderBs.ResetBindings(false);

                ApplyAllVisibilityRules();
                UpdateElapsedTime();
                UpdateEstimatedTimeToFinish(snapshot.linearMetersRemaining, snapshot.lineSpeed);
            });
        }

        // =====================================================================
        // UI LOGIC (LEVEL SELECTOR + SHEETTYPE)
        // =====================================================================
        private void SetupUiLogicForLevelAndSheetType()
        {
            SetupLevelSelectorCombo();
            SetupSheetTypeCombosIfNeeded();

            if (currentOrder_levelSelector != null)
            {
                currentOrder_levelSelector.SelectionChangeCommitted -= currentOrder_levelSelector_SelectionChangeCommitted;
                currentOrder_levelSelector.SelectionChangeCommitted += currentOrder_levelSelector_SelectionChangeCommitted;
            }

            if (currentOrder_order1_sheetType != null)
            {
                currentOrder_order1_sheetType.SelectionChangeCommitted -= Order_SheetType_SelectionChangeCommitted;
                currentOrder_order1_sheetType.SelectionChangeCommitted += Order_SheetType_SelectionChangeCommitted;
            }

            if (currentOrder_order2_sheetType != null)
            {
                currentOrder_order2_sheetType.SelectionChangeCommitted -= Order_SheetType_SelectionChangeCommitted;
                currentOrder_order2_sheetType.SelectionChangeCommitted += Order_SheetType_SelectionChangeCommitted;
            }
        }

        private void currentOrder_levelSelector_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ApplyAllVisibilityRules();
        }

        private void Order_SheetType_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ApplyAllVisibilityRules();
        }

        private void ApplyAllVisibilityRules()
        {
            SuspendLayout();
            try
            {
                int level = GetLevelSelectorValueSafe();

                ApplyLevelSelectorVisibility(level);

                ApplySheetTypeVisibilityForOrder(1, GetSheetTypeSafe(currentOrder_order1_sheetType));
                ApplySheetTypeVisibilityForOrder(2, GetSheetTypeSafe(currentOrder_order2_sheetType));
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        // -------------------------
        // LevelSelector
        // -------------------------
        private void SetupLevelSelectorCombo()
        {
            if (currentOrder_levelSelector == null) return;
            if (currentOrder_levelSelector.DataSource != null) return;

            currentOrder_levelSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            currentOrder_levelSelector.DisplayMember = "Text";
            currentOrder_levelSelector.ValueMember = "Value";

            currentOrder_levelSelector.DataSource = new[]
            {
                new { Text = "Pedido Superior", Value = (int)E_LevelSelector.Upper },
                new { Text = "Pedido Inferior", Value = (int)E_LevelSelector.Lower },
                new { Text = "Ambos",           Value = (int)E_LevelSelector.Both  },
            };
        }

        private int GetLevelSelectorValueSafe()
        {
            // 1) Preferred: SelectedValue (when combo is bound with ValueMember="Value")
            try
            {
                if (currentOrder_levelSelector != null)
                {
                    object sv = currentOrder_levelSelector.SelectedValue;

                    if (sv != null && sv != DBNull.Value)
                    {
                        // If the model/binding gives an enum directly
                        if (sv is E_LevelSelector)
                            return (int)(E_LevelSelector)sv;

                        // If it's already an int
                        if (sv is int)
                            return (int)sv;

                        // If it's string or other primitive, try parse
                        int v;
                        if (int.TryParse(Convert.ToString(sv), out v))
                            return ClampLevelSelector(v);
                    }
                }
            }
            catch
            {
                // ignore and fallback
            }

            // 2) Fallback: SelectedIndex mapping
            int idx = 0;
            try
            {
                if (currentOrder_levelSelector != null)
                    idx = currentOrder_levelSelector.SelectedIndex;
            }
            catch
            {
                idx = 0;
            }

            if (idx == 0) return (int)E_LevelSelector.Upper;
            if (idx == 1) return (int)E_LevelSelector.Lower;
            if (idx == 2) return (int)E_LevelSelector.Both;

            return (int)E_LevelSelector.Upper;
        }

        private int ClampLevelSelector(int v)
        {
            if (v < (int)E_LevelSelector.Upper) return (int)E_LevelSelector.Upper;
            if (v > (int)E_LevelSelector.Both) return (int)E_LevelSelector.Both;
            return v;
        }


        private void ApplyLevelSelectorVisibility(int levelSelectorValue)
        {
            // FIX: show order blocks correctly for Upper / Lower / Both
            bool showOrder1 = (levelSelectorValue == (int)E_LevelSelector.Upper) ||
                              (levelSelectorValue == (int)E_LevelSelector.Both);

            bool showOrder2 = (levelSelectorValue == (int)E_LevelSelector.Lower) ||
                              (levelSelectorValue == (int)E_LevelSelector.Both);

            SetOrder1ControlsVisible(showOrder1);
            SetOrder2ControlsVisible(showOrder2);

            if (!showOrder1) ResetOrder1Controls();
            if (!showOrder2) ResetOrder2Controls();
        }

        // -------------------------
        // SheetType
        // -------------------------
        private void SetupSheetTypeCombosIfNeeded()
        {
            // Keep as-is (designer already defines items/order).
        }

        private int GetSheetTypeSafe(ComboBox cb)
        {
            int idx = cb?.SelectedIndex ?? 0;
            if (idx < 0) idx = 0;
            if (idx > 2) idx = 2;
            return idx;
        }

        private void ApplySheetTypeVisibilityForOrder(int orderNumber, int sheetType)
        {
            bool orderVisible = orderNumber == 1 ? IsOrder1Visible() : IsOrder2Visible();
            if (!orderVisible) return;

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

                ResetHiddenMs(sheetType, currentOrder_order1_M1, currentOrder_order1_M2, currentOrder_order1_M3, currentOrder_order1_M4, currentOrder_order1_M5);
            }
            else
            {
                SetVisibleSafe(currentOrder_order2_M1, lbl_currentOrder_order2_M1, showM1);
                SetVisibleSafe(currentOrder_order2_M2, lbl_currentOrder_order2_M2, showM2);
                SetVisibleSafe(currentOrder_order2_M3, lbl_currentOrder_order2_M3, showM3);
                SetVisibleSafe(currentOrder_order2_M4, lbl_currentOrder_order2_M4, showM4);
                SetVisibleSafe(currentOrder_order2_M5, lbl_currentOrder_order2_M5, showM5);

                ResetHiddenMs(sheetType, currentOrder_order2_M1, currentOrder_order2_M2, currentOrder_order2_M3, currentOrder_order2_M4, currentOrder_order2_M5);
            }
        }

        private bool IsOrder1Visible() => currentOrder_order1_id?.Visible == true;
        private bool IsOrder2Visible() => currentOrder_order2_id?.Visible == true;

        private void ResetHiddenMs(int sheetType, NumericUpDown m1, NumericUpDown m2, NumericUpDown m3, NumericUpDown m4, NumericUpDown m5)
        {
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

        private static void SetVisibleSafe(Control c, Control label, bool visible)
        {
            if (c != null) c.Visible = visible;
            if (label != null) label.Visible = visible;
        }

        // =====================================================================
        // SHOW/HIDE + RESET (ORDER1 / ORDER2)
        // =====================================================================
        private void SetOrder1ControlsVisible(bool visible)
        {
            SetVisibleSafe(currentOrder_Order1_sheetQuantity, lbl_currentOrder_Order1_sheetQuantity, visible);
            SetVisibleSafe(currentOrder_order1_id, lbl_currentOrder_order1_id, visible);
            SetVisibleSafe(currentOrder_order1_product, lbl_currentOrder_order1_product, visible);
            SetVisibleSafe(currentOrder_order1_client, lbl_currentOrder_order1_client, visible);
            SetVisibleSafe(currentOrder_order1_sheetType, lbl_currentOrder_order1_sheetType, visible);

            if (currentOrder_order1_M1 != null) currentOrder_order1_M1.Visible = visible;
            if (currentOrder_order1_M2 != null) currentOrder_order1_M2.Visible = visible;
            if (currentOrder_order1_M3 != null) currentOrder_order1_M3.Visible = visible;
            if (currentOrder_order1_M4 != null) currentOrder_order1_M4.Visible = visible;
            if (currentOrder_order1_M5 != null) currentOrder_order1_M5.Visible = visible;

            if (lbl_currentOrder_order1_M1 != null) lbl_currentOrder_order1_M1.Visible = visible;
            if (lbl_currentOrder_order1_M2 != null) lbl_currentOrder_order1_M2.Visible = visible;
            if (lbl_currentOrder_order1_M3 != null) lbl_currentOrder_order1_M3.Visible = visible;
            if (lbl_currentOrder_order1_M4 != null) lbl_currentOrder_order1_M4.Visible = visible;
            if (lbl_currentOrder_order1_M5 != null) lbl_currentOrder_order1_M5.Visible = visible;

            if (currentOrder_order1_sheetLenght != null) currentOrder_order1_sheetLenght.Visible = visible;
            if (currentOrder_order1_numberOfCuts != null) currentOrder_order1_numberOfCuts.Visible = visible;
            if (currentOrder_order1_numberOfCutsProduced != null) currentOrder_order1_numberOfCutsProduced.Visible = visible;
            if (currentOrder_order1_numberOfCutsRemaining != null) currentOrder_order1_numberOfCutsRemaining.Visible = visible;

            if (currentOrder_order1_pileQuantity != null) currentOrder_order1_pileQuantity.Visible = visible;
            if (currentOrder_order1_pileQuantityProduced != null) currentOrder_order1_pileQuantityProduced.Visible = visible;
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
            SetVisibleSafe(currentOrder_order2_numberOfCutsProduced, lbl_currentOrder_order2_numberOfCutsProduced, visible);
            SetVisibleSafe(currentOrder_order2_numberOfCutsRemaining, lbl_currentOrder_order2_numberOfCutsRemaining, visible);

            // FIX: make sure BOTH pileQuantity and pileQuantityProduced are shown
            SetVisibleSafe(currentOrder_order2_pileQuantity, lbl_currentOrder_order2_pileQuantity, visible);
            SetVisibleSafe(currentOrder_order2_pileQuantityProduced, lbl_currentOrder_order2_pileQuantityProduced, visible);

            SetVisibleSafe(currentOrder_order2_scrapCounter, lbl_currentOrder_order2_scrapCounter, visible);

            SetVisibleSafe(lblBottomOrderX, null, visible);
        }

        private void ResetOrder1Controls()
        {
            if (currentOrder_order1_id != null) currentOrder_order1_id.Text = "";
            if (currentOrder_order1_product != null) currentOrder_order1_product.Text = "";
            if (currentOrder_order1_client != null) currentOrder_order1_client.Text = "";

            if (currentOrder_order1_sheetType != null && currentOrder_order1_sheetType.Items.Count > 0)
                currentOrder_order1_sheetType.SelectedIndex = 0;

            SetNudSafe(currentOrder_Order1_sheetQuantity, 0);
            SetNudSafe(currentOrder_order1_sheetLenght, 0);

            SetNudSafe(currentOrder_order1_M1, 0);
            SetNudSafe(currentOrder_order1_M2, 0);
            SetNudSafe(currentOrder_order1_M3, 0);
            SetNudSafe(currentOrder_order1_M4, 0);
            SetNudSafe(currentOrder_order1_M5, 0);

            SetNudSafe(currentOrder_order1_numberOfCuts, 0);
            SetNudSafe(currentOrder_order1_numberOfCutsProduced, 0);
            SetNudSafe(currentOrder_order1_numberOfCutsRemaining, 0);

            SetNudSafe(currentOrder_order1_pileQuantity, 0);
            SetNudSafe(currentOrder_order1_pileQuantityProduced, 0);
        }

        private void ResetOrder2Controls()
        {
            if (currentOrder_order2_id != null) currentOrder_order2_id.Text = "";
            if (currentOrder_order2_product != null) currentOrder_order2_product.Text = "";
            if (currentOrder_order2_client != null) currentOrder_order2_client.Text = "";

            if (currentOrder_order2_sheetType != null && currentOrder_order2_sheetType.Items.Count > 0)
                currentOrder_order2_sheetType.SelectedIndex = 0;

            SetNudSafe(currentOrder_Order2_sheetQuantity, 0);
            SetNudSafe(currentOrder_order2_sheetLenght, 0);

            SetNudSafe(currentOrder_order2_M1, 0);
            SetNudSafe(currentOrder_order2_M2, 0);
            SetNudSafe(currentOrder_order2_M3, 0);
            SetNudSafe(currentOrder_order2_M4, 0);
            SetNudSafe(currentOrder_order2_M5, 0);

            SetNudSafe(currentOrder_order2_numberOfCuts, 0);
            SetNudSafe(currentOrder_order2_numberOfCutsProduced, 0);
            SetNudSafe(currentOrder_order2_numberOfCutsRemaining, 0);

            // FIX: reset pileQuantity too
            SetNudSafe(currentOrder_order2_pileQuantity, 0);
            SetNudSafe(currentOrder_order2_pileQuantityProduced, 0);
        }

        private static void SetNudSafe(NumericUpDown nud, decimal value)
        {
            if (nud == null) return;

            if (value < nud.Minimum) value = nud.Minimum;
            if (value > nud.Maximum) value = nud.Maximum;

            nud.Value = value;
        }

        // =====================================================================
        // BINDINGS SETUP (ONE TIME)
        // =====================================================================
        private void SetupCurrentOrderBindings()
        {
            _currentOrderBs.DataSource = frmMain.instance?.CurrentOrderModel ?? new order_TypeStruct();

            // Text
            BindStartedAtTextPtBr(currentOrder_startedAt, "startedAt");
            BindText(currentOrder_paperComposition, "paperComposition");
            BindText(currentOrder_paper1, "paper1");
            BindText(currentOrder_paper2, "paper2");
            BindText(currentOrder_paper3, "paper3");
            BindText(currentOrder_paper4, "paper4");
            BindText(currentOrder_paper5, "paper5");
            BindText(currentOrder_paper1_productionListNumber, "productionListNumber");

            // Combos
            BindSelectedItem(currentOrder_fluteType, "fluteType");
            BindLevelSelectorSelectedValue(currentOrder_levelSelector, "levelSelector");
            BindSelectedIndex(currentOrder_order1_sheetType, "order1.sheetType");
            BindSelectedIndex(currentOrder_order2_sheetType, "order2.sheetType");

            // Numerics
            BindNumeric(currentOrder_paperWidth, "paperWidth");
            BindNumeric(currentOrder_linearMeters, "linearMeters");
            BindNumeric(currentOrder_linearMetersProduced, "linearMetersProduced");
            BindNumeric(currentOrder_linearMetersRemaining, "linearMetersRemaining");

            // Order1
            BindNumeric(currentOrder_Order1_sheetQuantity, "order1.sheetQuantity");
            BindNumeric(currentOrder_order1_sheetLenght, "order1.sheetLength");
            BindNumeric(currentOrder_order1_M1, "order1.sheetM1");
            BindNumeric(currentOrder_order1_M2, "order1.sheetM2");
            BindNumeric(currentOrder_order1_M3, "order1.sheetM3");
            BindNumeric(currentOrder_order1_M4, "order1.sheetM4");
            BindNumeric(currentOrder_order1_M5, "order1.sheetM5");

            BindNumeric(currentOrder_order1_numberOfCuts, "order1.numberOfCuts");
            BindNumeric(currentOrder_order1_numberOfCutsProduced, "order1.numberOfCutsProduced");
            BindNumeric(currentOrder_order1_numberOfCutsRemaining, "order1.numberOfCutsRemaining");

            BindNumeric(currentOrder_order1_pileQuantity, "order1.pileQuantity");
            BindNumeric(currentOrder_order1_pileQuantityProduced, "order1.pileQuantityProduced");

            BindText(currentOrder_order1_id, "order1.id");
            BindText(currentOrder_order1_client, "order1.client");
            BindText(currentOrder_order1_product, "order1.product");

            // Order2
            BindNumeric(currentOrder_Order2_sheetQuantity, "order2.sheetQuantity");
            BindNumeric(currentOrder_order2_sheetLenght, "order2.sheetLength");
            BindNumeric(currentOrder_order2_M1, "order2.sheetM1");
            BindNumeric(currentOrder_order2_M2, "order2.sheetM2");
            BindNumeric(currentOrder_order2_M3, "order2.sheetM3");
            BindNumeric(currentOrder_order2_M4, "order2.sheetM4");
            BindNumeric(currentOrder_order2_M5, "order2.sheetM5");

            BindNumeric(currentOrder_order2_numberOfCuts, "order2.numberOfCuts");
            BindNumeric(currentOrder_order2_numberOfCutsProduced, "order2.numberOfCutsProduced");
            BindNumeric(currentOrder_order2_numberOfCutsRemaining, "order2.numberOfCutsRemaining");

            BindNumeric(currentOrder_order2_pileQuantity, "order2.pileQuantity");
            BindNumeric(currentOrder_order2_pileQuantityProduced, "order2.pileQuantityProduced");

            BindText(currentOrder_order2_id, "order2.id");
            BindText(currentOrder_order2_client, "order2.client");
            BindText(currentOrder_order2_product, "order2.product");
        }

        // =====================================================================
        // BINDING HELPERS
        // =====================================================================
        private void BindText(TextBox tb, string dataMember)
        {
            if (tb == null) return;
            tb.DataBindings.Clear();
            tb.DataBindings.Add("Text", _currentOrderBs, dataMember, true, DataSourceUpdateMode.Never);
        }

        private void BindSelectedItem(ComboBox cb, string dataMember)
        {
            if (cb == null) return;
            cb.DataBindings.Clear();
            cb.DataBindings.Add("SelectedItem", _currentOrderBs, dataMember, true, DataSourceUpdateMode.Never);
        }

        private void BindSelectedIndex(ComboBox cb, string dataMember)
        {
            if (cb == null) return;
            cb.DataBindings.Clear();
            cb.DataBindings.Add("SelectedIndex", _currentOrderBs, dataMember, true, DataSourceUpdateMode.Never);
        }

        private void BindNumeric(NumericUpDown nud, string dataMember)
        {
            if (nud == null) return;
            nud.DataBindings.Clear();
            nud.DataBindings.Add("Value", _currentOrderBs, dataMember, true, DataSourceUpdateMode.Never);
        }

        private void BindLevelSelectorSelectedValue(ComboBox cb, string dataMember)
        {
            if (cb == null) return;

            cb.DataBindings.Clear();

            var b = new Binding("SelectedValue", _currentOrderBs, dataMember, true, DataSourceUpdateMode.Never);

            b.Format += (s, e) =>
            {
                try { e.Value = Convert.ToInt32(e.Value); }
                catch { e.Value = (int)E_LevelSelector.Upper; }
            };

            b.Parse += (s, e) =>
            {
                try
                {
                    int v = Convert.ToInt32(e.Value);
                    if (v < 1) v = 1;
                    if (v > 3) v = 3;
                    e.Value = (E_LevelSelector)v;
                }
                catch
                {
                    e.Value = E_LevelSelector.Upper;
                }
            };

            cb.DataBindings.Add(b);
        }

        private void BindStartedAtTextPtBr(TextBox tb, string dataMember)
        {
            if (tb == null) return;

            tb.DataBindings.Clear();

            var b = new Binding("Text", _currentOrderBs, dataMember, true, DataSourceUpdateMode.Never);

            b.Format += (s, e) =>
            {
                // incoming can be DateTime or string
                if (e.Value == null) { e.Value = ""; return; }

                if (e.Value is DateTime dt)
                {
                    e.Value = dt.ToString(UiDateTimeFormat, PtBr);
                    return;
                }

                string raw = e.Value.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(raw)) { e.Value = ""; return; }

                if (DateTime.TryParseExact(raw, PlcDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
                {
                    e.Value = d1.ToString(UiDateTimeFormat, PtBr);
                    return;
                }

                // fallback: keep raw
                e.Value = raw;
            };

            tb.DataBindings.Add(b);
        }

        // =====================================================================
        // BUTTONS
        // =====================================================================
        private void btnChangeOrder_Click(object sender, EventArgs e)
        {
            if (frmMain.instance == null || frmMain.instance.tcClient == null || !frmMain.instance.tcClient.IsConnected)
            {
                MessageBox.Show("PLC not connected.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dr = MessageBox.Show(
                "Deseja trocar o pedido?",
                "Confirmação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2
            );

            if (dr != DialogResult.Yes)
                return;

            try
            {
                frmMain.instance.writeSetButtonToPlc(frmMain.instance.symbols.currentOrder.changeOrderRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExecutePositioning_Click(object sender, EventArgs e) { /* unchanged */ }

        private void btnAutomaticOrderChange_Click(object sender, EventArgs e)
        {
            if (frmMain.instance == null || frmMain.instance.tcClient == null || !frmMain.instance.tcClient.IsConnected)
            {
                MessageBox.Show("PLC not connected.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dr = MessageBox.Show(
                "Deseja efetuar a troca automática do pedido?",
                "Confirmação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2
            );

            if (dr != DialogResult.Yes)
                return;

            try
            {
                frmMain.instance.writeSetButtonToPlc(frmMain.instance.symbols.currentOrder.aocRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmHome_Enter(object sender, EventArgs e)
        {
            LoadDatabaseItens();
        }

        // =====================================================================
        // TIME / ESTIMATES
        // =====================================================================
        private void UpdateElapsedTime()
        {
            try
            {
                string raw = currentOrder_startedAt?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    txtElapsedTime.Text = "";
                    return;
                }

                // best effort parse PT-BR display format
                if (!DateTime.TryParseExact(raw, UiDateTimeFormat, PtBr, DateTimeStyles.None, out DateTime startedAt))
                {
                    txtElapsedTime.Text = "";
                    return;
                }

                TimeSpan elapsed = DateTime.Now - startedAt;
                if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

                // clamp to avoid weird long-running issues
                if (elapsed > TimeSpan.FromDays(99))
                    elapsed = TimeSpan.FromDays(99);

                int hours = (int)elapsed.TotalHours;
                txtElapsedTime.Text = $"{hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            }
            catch
            {
                txtElapsedTime.Text = "";
            }
        }

        private void UpdateEstimatedTimeToFinishSafe()
        {
            var snap = _currentOrderBs.Current as order_TypeStruct;
            if (snap == null) { txtEstimatedTimeToFinish.Text = ""; return; }
            UpdateEstimatedTimeToFinish(snap.linearMetersRemaining, snap.lineSpeed);
        }

        private void UpdateEstimatedTimeToFinish(double linearMetersRemaining, double lineSpeedMpm)
        {
            try
            {
                if (txtEstimatedTimeToFinish == null)
                    return;

                if (lineSpeedMpm <= 0.0 || linearMetersRemaining <= 0.0)
                {
                    txtEstimatedTimeToFinish.Text = "";
                    return;
                }

                double minutesToFinish = linearMetersRemaining / lineSpeedMpm;
                if (minutesToFinish < 0) minutesToFinish = 0;

                // clamp: 0..999 hours (arbitrary safety)
                if (minutesToFinish > (999 * 60))
                    minutesToFinish = 999 * 60;

                TimeSpan ts = TimeSpan.FromMinutes(minutesToFinish);
                int hours = (int)ts.TotalHours;
                txtEstimatedTimeToFinish.Text = $"{hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            }
            catch
            {
                if (txtEstimatedTimeToFinish != null)
                    txtEstimatedTimeToFinish.Text = "";
            }
        }

        // =====================================================================
        // ORDER DESCRIPTION (YOUR ORIGINAL LOGIC KEPT)
        // =====================================================================
        private void UpdateOrderDescription()
        {
            try
            {
                var productionList = nextOrderBindingSource.DataSource as List<ProductionListPlc> ?? new List<ProductionListPlc>();

                foreach (var production in productionList)
                {
                    production.Order1Description = BuildOrder1Description(production);
                    production.Order2Description = BuildOrder2Description(production);
                    production.OrderDetails = BuildOrderDetailDescription(production);
                }

                nextOrderBindingSource.DataSource = productionList;
                nextOrderBindingSource.ResetBindings(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string BuildOrder1Description(ProductionListPlc production)
        {
            string order1Description =
                "CLIENTE: " + (production.Order1Client ?? "")
                + "\n"
                + "PRODUTO: " + (production.Order1Product ?? "")
                + "\n"
                + "Nº DA OF: " + (production.Order1Id?.ToString() ?? "")
                + "\n"
                + "MEDIDA: " + (production.Order1SheetQuantity?.ToString() ?? "0");

            int m1 = production.Order1M1 ?? 0;
            int m2 = production.Order1M2 ?? 0;
            int m3 = production.Order1M3 ?? 0;
            int m4 = production.Order1M4 ?? 0;
            int m5 = production.Order1M5 ?? 0;

            int width = m1 + m2 + m3 + m4; // keep your original rule
            int sheetType = production.Order1SheetType ?? 0;

            if (sheetType == 0)
            {
                order1Description += " x " + m1
                                   + "\n"
                                   + "LARGURA: " + width
                                   + "\n"
                                   + "COMPRIMENTO: " + (production.Order1SheetLength ?? 0)
                                   + "\n"
                                   + "Nº DE CORTES: " + (production.Order1NumberOfCuts ?? 0);
            }
            else if (sheetType == 1)
            {
                order1Description += " x " + m1 + "*" + m2 + "*" + m3
                                   + "\n"
                                   + "LARGURA: " + width
                                   + "\n"
                                   + "COMPRIMENTO: " + (production.Order1SheetLength ?? 0)
                                   + "\n"
                                   + "Nº DE CORTES: " + (production.Order1NumberOfCuts ?? 0);
            }
            else
            {
                order1Description += " x " + m1 + "*" + m2 + "*" + m3 + "*" + m4 + "*" + m5
                                   + "\n"
                                   + "LARGURA: " + width
                                   + "\n"
                                   + "COMPRIMENTO: " + (production.Order1SheetLength ?? 0)
                                   + "\n"
                                   + "Nº DE CORTES: " + (production.Order1NumberOfCuts ?? 0);
            }

            return order1Description;
        }

        private string BuildOrder2Description(ProductionListPlc production)
        {
            int levelSelector = production.LevelSelector ?? 1;
            bool order2Enabled = (levelSelector == (int)E_LevelSelector.Both) || (levelSelector == (int)E_LevelSelector.Lower);

            if (!order2Enabled)
                return "DESABILITADO";

            string order2Description =
                "CLIENTE: " + (production.Order2Client ?? "")
                + "\n"
                + "PRODUTO: " + (production.Order2Product ?? "")
                + "\n"
                + "Nº DA OF: " + (production.Order2Id?.ToString() ?? "")
                + "\n"
                + "MEDIDA: " + (production.Order2SheetQuantity?.ToString() ?? "0");

            int m1 = production.Order2M1 ?? 0;
            int m2 = production.Order2M2 ?? 0;
            int m3 = production.Order2M3 ?? 0;
            int m4 = production.Order2M4 ?? 0;
            int m5 = production.Order2M5 ?? 0;

            int width = m1 + m2 + m3 + m4; // keep your original rule
            int sheetType = production.Order2SheetType ?? 0;

            if (sheetType == 0)
            {
                order2Description += " x " + m1
                                   + "\n"
                                   + "LARGURA: " + width
                                   + "\n"
                                   + "COMPRIMENTO: " + (production.Order2SheetLength ?? 0)
                                   + "\n"
                                   + "Nº DE CORTES: " + (production.Order2NumberOfCuts ?? 0);
            }
            else if (sheetType == 1)
            {
                order2Description += " x " + m1 + "*" + m2 + "*" + m3
                                   + "\n"
                                   + "LARGURA: " + width
                                   + "\n"
                                   + "COMPRIMENTO: " + (production.Order2SheetLength ?? 0)
                                   + "\n"
                                   + "Nº DE CORTES: " + (production.Order2NumberOfCuts ?? 0);
            }
            else
            {
                order2Description += " x " + m1 + "*" + m2 + "*" + m3 + "*" + m4 + "*" + m5
                                   + "\n"
                                   + "LARGURA: " + width
                                   + "\n"
                                   + "COMPRIMENTO: " + (production.Order2SheetLength ?? 0)
                                   + "\n"
                                   + "Nº DE CORTES: " + (production.Order2NumberOfCuts ?? 0);
            }

            return order2Description;
        }

        private string BuildOrderDetailDescription(ProductionListPlc production)
        {
            int paperWidth = production.PaperWidth ?? 0;

            int order1SheetQty = production.Order1SheetQuantity ?? 0;
            int order2SheetQty = production.Order2SheetQuantity ?? 0;

            int order1M1 = production.Order1M1 ?? 0;
            int order1M2 = production.Order1M2 ?? 0;
            int order1M3 = production.Order1M3 ?? 0;
            int order1M4 = production.Order1M4 ?? 0;
            int order1M5 = production.Order1M5 ?? 0;

            int order2M1 = production.Order2M1 ?? 0;
            int order2M2 = production.Order2M2 ?? 0;
            int order2M3 = production.Order2M3 ?? 0;
            int order2M4 = production.Order2M4 ?? 0;
            int order2M5 = production.Order2M5 ?? 0;

            int order1Width = (order1M1 + order1M2 + order1M3 + order1M4 + order1M5) * order1SheetQty;
            int order2Width = (order2M1 + order2M2 + order2M3 + order2M4 + order2M5) * order2SheetQty;

            int totalWidth = order1Width + order2Width;
            int paperTrimSize = paperWidth - totalWidth;

            double trimPct = 0;
            if (paperWidth > 0)
                trimPct = Math.Round(((double)paperTrimSize / paperWidth) * 100.0, 2);

            double linearMeters = ((production.Order1SheetLength ?? 0) * (production.Order1NumberOfCuts ?? 0)) * 0.001;

            string orderDetails =
                "FORMATO: " + paperWidth + " MM" + " => ONDA: " + (production.FluteType ?? "")
                + "\n"
                + "LARGURA TOTAL: " + totalWidth + " MM"
                + "\n"
                + "REFILE LARGURA: " + "2 * " + (paperTrimSize / 2) + " MM"
                + "\n"
                + "REFILE PERCENTUAL: " + trimPct + "%"
                + "\n"
                + "COMPOSIÇÃO: " + (production.PaperComposition ?? "")
                + "\n"
                + "METRO LINEAR: " + linearMeters.ToString(CultureInfo.InvariantCulture);

            return orderDetails;
        }

        // =====================================================================
        // UTIL
        // =====================================================================
        private void SafeUi(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }
    }
}
