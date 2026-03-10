using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Dapper;

using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

using Hmi.Data;

namespace Hmi
{
    public partial class frmMain : Form
    {
        // ============================================================
        // CONFIG / SINGLETON
        // ============================================================
        public static frmMain instance;

         public string adsAddres = "192.168.30.79.1.1";
        //    public string adsAddres = "169.254.44.92.1.1";
        // public string adsAddres = "127.0.0.1.1.1";

        private const int PlcPort = 851;
        private const int SystemPort = 10000;

        // ============================================================
        // UI / CHILD FORMS
        // ============================================================
        private Form currentChildForm;
        private readonly Form formBackground = new Form();

        private readonly frmHome frmHome = new frmHome();
        private readonly frmOrders frmOrders = new frmOrders();
        private readonly frmHist frmHist = new frmHist();
        private readonly frmProductionGraph frmProductionGraph = new frmProductionGraph();

        // ============================================================
        // ADS / SYMBOLS
        // ============================================================
        public TcAdsClient tcClient;
        public TcAdsClient tcSystemClient;
        private AdsSession _session;

        public dynamic symbols;
        private dynamic currentOrder;
        private dynamic nextOrder;

        private DynamicSymbol _currentOrderSymbol;
        private DynamicSymbol _nextOrderSymbol;

        private int _lastNextOrderSignature = 0;
        private readonly object _nextOrderWriteLock = new object();
        // ============================================================
        // MODELS / THREADING
        // ============================================================
        private readonly object _orderLock = new object();
        public order_TypeStruct CurrentOrderModel { get; private set; } = new order_TypeStruct();
        public order_TypeStruct NextOrderModel { get; private set; } = new order_TypeStruct();

        public event Action<order_TypeStruct> CurrentOrderUpdated;

        // Rising edge / re-entrancy
        private bool _changeOrderRequestOld = false;
        private bool _changeOrderBusy = false;

        // ============================================================
        // MACHINE STOPPED TIME LOGGER
        // ============================================================
        private readonly MachineNotRunningTimePerHour machineStoppedTimeInstance = new MachineNotRunningTimePerHour();
        public event Action DatabaseItemsReloaded;

        // ============================================================
        // CTOR
        // ============================================================
        public frmMain()
        {
            InitializeComponent();
            instance = this;
        }

        // ============================================================
        // FORM LIFECYCLE
        // ============================================================
        private void frmMain_Load(object sender, EventArgs e)
        {
            OpenChildForm(frmHome);
            LoadDatabaseItems();

            try
            {
                AdsConnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ADS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            SetupCurrentOrderBindings_Main();
            WireCurrentOrderEdgeHandler();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisposeAds();
        }

        // ============================================================
        // FORM NAVIGATION
        // ============================================================
        private void OpenChildForm(Form childForm)
        {
            childForm.TopLevel = false;
            pMain.Controls.Add(childForm);
            pMain.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }

        private void btnPage1_Click(object sender, EventArgs e)
        {
            btnPage1.BackColor = Color.FromArgb(220, 220, 230);
            btnPage4.BackColor = Color.FromArgb(39, 51, 56);
            btnPage5.BackColor = Color.FromArgb(39, 51, 56);
            btnPage6.BackColor = Color.FromArgb(39, 51, 56);

            OpenChildForm(frmHome);
            frmHome.LoadDatabaseItens();
        }

        private void btnPage2_Click(object sender, EventArgs e)
        {
            btnPage1.BackColor = Color.FromArgb(39, 51, 56);
            btnPage4.BackColor = Color.FromArgb(39, 51, 56);
            btnPage5.BackColor = Color.FromArgb(39, 51, 56);
            btnPage6.BackColor = Color.FromArgb(39, 51, 56);
        }

        private void btnPage3_Click(object sender, EventArgs e)
        {
            btnPage1.BackColor = Color.FromArgb(39, 51, 56);
            btnPage4.BackColor = Color.FromArgb(39, 51, 56);
            btnPage5.BackColor = Color.FromArgb(39, 51, 56);
            btnPage6.BackColor = Color.FromArgb(39, 51, 56);
        }

        private void btnPage4_Click(object sender, EventArgs e)
        {
            btnPage1.BackColor = Color.FromArgb(39, 51, 56);
            btnPage4.BackColor = Color.FromArgb(220, 220, 230);
            btnPage5.BackColor = Color.FromArgb(39, 51, 56);
            btnPage6.BackColor = Color.FromArgb(39, 51, 56);

            OpenChildForm(frmOrders);
        }

        private void btnPage5_Click(object sender, EventArgs e)
        {
            btnPage1.BackColor = Color.FromArgb(39, 51, 56);
            btnPage4.BackColor = Color.FromArgb(39, 51, 56);
            btnPage5.BackColor = Color.FromArgb(220, 220, 230);
            btnPage6.BackColor = Color.FromArgb(39, 51, 56);

            OpenChildForm(frmHist);
        }

        private void btnPage6_Click(object sender, EventArgs e)
        {
            btnPage1.BackColor = Color.FromArgb(39, 51, 56);
            btnPage4.BackColor = Color.FromArgb(39, 51, 56);
            btnPage5.BackColor = Color.FromArgb(39, 51, 56);
            btnPage6.BackColor = Color.FromArgb(220, 220, 230);

            OpenChildForm(frmProductionGraph);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // frmLogin.Show();
        }

        // ============================================================
        // ADS CONNECT / SYMBOL SUBSCRIBE
        // ============================================================
        private void AdsConnect()
        {
            tcClient = new TcAdsClient();
            tcSystemClient = new TcAdsClient();

            _session = new AdsSession(AmsNetId.Parse(adsAddres), SystemPort);
            IConnection connection = _session.Connect();
            lblConnectionState.Text = connection.ConnectionState.ToString();

            tcSystemClient.Connect(adsAddres, SystemPort);

            EnsurePlcIsRunning();

            tcClient.Connect(adsAddres, PlcPort);

            if (!tcClient.IsConnected)
                throw new Exception("Failed to connect to PLC ADS port 851.");

            tcClient.Synchronize = true;

            IDynamicSymbolLoader loader =
                (IDynamicSymbolLoader)SymbolLoaderFactory.Create(tcClient, SymbolLoaderSettings.DefaultDynamic);

            symbols = loader.SymbolsDynamic;

            currentOrder = symbols.currentOrder;
            nextOrder = symbols.nextOrder;

            _currentOrderSymbol = symbols.currentOrder;
            _nextOrderSymbol = symbols.nextOrder;

            _session.ConnectionStateChanged += _session_ConnectionStateChanged;

            _currentOrderSymbol.ValueChanged += currentOrder_ValueChanged;
            _nextOrderSymbol.ValueChanged += nextOrder_ValueChanged;
        }

        private void EnsurePlcIsRunning()
        {
            if (tcSystemClient.ReadState().AdsState == AdsState.Run)
                return;

            tcSystemClient.WriteControl(new StateInfo(AdsState.Reset, tcSystemClient.ReadState().DeviceState));

            if (!WaitForState(AdsState.Run, 6000))
                throw new Exception("PLC did not reach RUN state within timeout.");
        }

        private bool WaitForState(AdsState state, int timeOutInMilliSeconds)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <= timeOutInMilliSeconds)
            {
                try
                {
                    if (tcSystemClient.ReadState().AdsState == state)
                        return true;
                }
                catch (AdsErrorException)
                {
                    // transient while ADS changes state
                }
                finally
                {
                    Thread.Sleep(20);
                }
            }

            return false;
        }

        private void DisposeAds()
        {
            try
            {
                if (_currentOrderSymbol != null) _currentOrderSymbol.ValueChanged -= currentOrder_ValueChanged;
                if (_nextOrderSymbol != null) _nextOrderSymbol.ValueChanged -= nextOrder_ValueChanged;
            }
            catch { /* ignore */ }

            try { tcClient?.Dispose(); } catch { }
            try { tcSystemClient?.Dispose(); } catch { }
            try { _session?.Dispose(); } catch { }
        }

        private void _session_ConnectionStateChanged(object sender, TwinCAT.ConnectionStateChangedEventArgs e)
        {
            lblConnectionState.Text = e.NewState.ToString();
        }

        // ============================================================
        // ADS VALUE CHANGED -> MODEL
        // ============================================================
        private void currentOrder_ValueChanged(object sender, ValueChangedArgs e)
        {
            var snapshot = PlcOrderMapper.FromPlc(e.Value);

            lock (_orderLock)
            {
                CurrentOrderModel = snapshot;
            }

            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    // Update main-form bindings
                    _currentOrderBsMain.DataSource = snapshot;
                    _currentOrderBsMain.ResetBindings(false);

                    // Keep your existing event for other forms
                    CurrentOrderUpdated?.Invoke(snapshot);
                }));
            }
        }

        private void nextOrder_ValueChanged(object sender, ValueChangedArgs e)
        {
            var snapshot = PlcOrderMapper.FromPlc(e.Value);

            lock (_orderLock)
            {
                NextOrderModel = snapshot;
            }
        }

        // ============================================================
        // DB: LOAD ITEMS
        // ============================================================
        public void LoadDatabaseItems()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                {
                    if (db.State == System.Data.ConnectionState.Closed)
                        db.Open();

                    nextOrderBindingSource.DataSource =
                        db.Query<ProductionListPlc>(
                            "SELECT TOP 1 * FROM dbo.ProductionList_Plc WHERE (ProductionState < 1) AND (ProductionSequence > 0) ORDER BY ProductionSequence ASC",
                            commandType: CommandType.Text);

                    if (db.State == System.Data.ConnectionState.Open)
                        db.Close();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // ADS WRITE HELPERS (keep your public helpers)
        // ============================================================
        public void writeBoolToPlc(dynamic writeSymbol, bool value)
        {
            try { ((DynamicSymbol)writeSymbol).WriteValue(value); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        public void writeMomentaryButtonToPlc(dynamic writeSymbol)
        {
            try
            {
                DynamicSymbol write = writeSymbol;
                write.WriteValue(true);
                Thread.Sleep(10);
                write.WriteValue(false);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        public void writeResetButtonToPlc(dynamic writeSymbol)
        {
            try { ((DynamicSymbol)writeSymbol).WriteValue(false); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        public void writeSetButtonToPlc(dynamic writeSymbol)
        {
            try { ((DynamicSymbol)writeSymbol).WriteValue(true); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        public void writeIntToPlc(dynamic writeSymbol, int value)
        {
            try { ((DynamicSymbol)writeSymbol).WriteValue(value); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        public void writeFloatToPlc(dynamic writeSymbol, float value)
        {
            try { ((DynamicSymbol)writeSymbol).WriteValue(value); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ============================================================
        // CENTRALIZED ADS SAFE READ/WRITE (for internal logic)
        // ============================================================
        private static object ReadPlc(DynamicSymbol sym) => sym?.ReadValue();

        private static int ToIntSafe(object value)
        {
            if (value == null) return 0;
            try { return Convert.ToInt32(value); } catch { return 0; }
        }

        private static bool ToBoolSafe(object value)
        {
            if (value == null) return false;
            try { return Convert.ToBoolean(value); } catch { return false; }
        }

        private static void WriteToPlc(DynamicSymbol variable, object value)
        {
            if (variable == null) return;
            variable.WriteValue(value ?? 0); // hard rule: never write null
        }

        private static int ReadInt(DynamicSymbol sym) => ToIntSafe(ReadPlc(sym));

        // ============================================================
        // NEXT ORDER -> PLC (your logic preserved, organized)
        // ============================================================

        private int ComputeNextOrderSignature(ProductionListPlc obj)
        {
            unchecked
            {
                int h = 17;

                // local helpers
                void AddInt(int v) { h = (h * 31) + v; }
                void AddStr(string s) { h = (h * 31) + (s == null ? 0 : s.GetHashCode()); }

                int levelSelector = obj.LevelSelector ?? 0;
                bool order2Enabled = (levelSelector == 3);

                // HEADER / COMPOSITION
                AddInt(obj.Id);
                AddStr(obj.PaperComposition ?? "");
                AddStr(obj.FluteType ?? "");
                AddInt(obj.PaperWidth ?? 0);
                AddStr(obj.Paper1 ?? "");
                AddStr(obj.Paper2 ?? "");
                AddStr(obj.Paper3 ?? "");
                AddStr(obj.Paper4 ?? "");
                AddStr(obj.Paper5 ?? "");
                AddStr(obj.ProductionListNumber ?? "");
                AddInt(levelSelector);

                // ORDER 1
                AddInt(obj.Order1Id ?? 0);
                AddStr(obj.Order1Client ?? "");
                AddStr(obj.Order1Product ?? "");
                AddInt(obj.Order1SheetQuantity ?? 0);
                AddInt(ClampSheetType(obj.Order1SheetType));
                AddInt(obj.Order1M1 ?? 0);
                AddInt(obj.Order1M2 ?? 0);
                AddInt(obj.Order1M3 ?? 0);
                AddInt(obj.Order1M4 ?? 0);
                AddInt(obj.Order1M5 ?? 0);
                AddInt(obj.Order1SheetLength ?? 0);
                AddInt(obj.Order1NumberOfCuts ?? 0);
                AddInt(obj.Order1PileQuantity ?? 0);

                // ORDER 2 (enabled => include real values; disabled => include zeros/blanks)
                if (order2Enabled)
                {
                    AddInt(obj.Order2Id ?? 0);
                    AddStr(obj.Order2Client ?? "");
                    AddStr(obj.Order2Product ?? "");
                    AddInt(obj.Order2SheetQuantity ?? 0);
                    AddInt(ClampSheetType(obj.Order2SheetType));
                    AddInt(obj.Order2M1 ?? 0);
                    AddInt(obj.Order2M2 ?? 0);
                    AddInt(obj.Order2M3 ?? 0);
                    AddInt(obj.Order2M4 ?? 0);
                    AddInt(obj.Order2M5 ?? 0);
                    AddInt(obj.Order2SheetLength ?? 0);
                    AddInt(obj.Order2NumberOfCuts ?? 0);
                    AddInt(obj.Order2PileQuantity ?? 0);
                }
                else
                {
                    AddInt(0); AddStr(""); AddStr("");
                    AddInt(0); AddInt(0);
                    AddInt(0); AddInt(0); AddInt(0); AddInt(0); AddInt(0);
                    AddInt(0); AddInt(0); AddInt(0);
                }

                return h;
            }
        }

        public void WriteNextOrderToPlc()
        {
            LoadDatabaseItems();

            var obj = nextOrderBindingSource.Current as ProductionListPlc;
            if (obj == null) return;

            // Build signature of everything we write
            int sig = ComputeNextOrderSignature(obj);

            lock (_nextOrderWriteLock)
            {
                // If NOTHING changed, skip writing
                if (_lastNextOrderSignature == sig)
                    return;

                try
                {
                    // HEADER / COMPOSITION
                    WriteToPlc(nextOrder.tableID, obj.Id);
                    WriteToPlc(nextOrder.paperComposition, obj.PaperComposition ?? "");
                    WriteToPlc(nextOrder.fluteType, obj.FluteType ?? "");
                    WriteToPlc(nextOrder.paperWidth, obj.PaperWidth ?? 0);
                    WriteToPlc(nextOrder.paper1, obj.Paper1 ?? "");
                    WriteToPlc(nextOrder.paper2, obj.Paper2 ?? "");
                    WriteToPlc(nextOrder.paper3, obj.Paper3 ?? "");
                    WriteToPlc(nextOrder.paper4, obj.Paper4 ?? "");
                    WriteToPlc(nextOrder.paper5, obj.Paper5 ?? "");
                    WriteToPlc(nextOrder.productionListNumber, obj.ProductionListNumber ?? "");

                    int levelSelector = obj.LevelSelector ?? 0;
                    WriteToPlc(nextOrder.levelSelector, levelSelector);

                    // ORDER 1
                    WriteToPlc(nextOrder.order1.id, obj.Order1Id ?? 0);
                    WriteToPlc(nextOrder.order1.client, obj.Order1Client ?? "");
                    WriteToPlc(nextOrder.order1.product, obj.Order1Product ?? "");
                    WriteToPlc(nextOrder.order1.sheetQuantity, obj.Order1SheetQuantity ?? 0);
                    WriteToPlc(nextOrder.order1.sheetType, ClampSheetType(obj.Order1SheetType));
                    WriteToPlc(nextOrder.order1.sheetM1, obj.Order1M1 ?? 0);
                    WriteToPlc(nextOrder.order1.sheetM2, obj.Order1M2 ?? 0);
                    WriteToPlc(nextOrder.order1.sheetM3, obj.Order1M3 ?? 0);
                    WriteToPlc(nextOrder.order1.sheetM4, obj.Order1M4 ?? 0);
                    WriteToPlc(nextOrder.order1.sheetM5, obj.Order1M5 ?? 0);
                    WriteToPlc(nextOrder.order1.sheetLength, obj.Order1SheetLength ?? 0);
                    WriteToPlc(nextOrder.order1.numberOfCuts, obj.Order1NumberOfCuts ?? 0);

                    // reset runtime counters on write
                    WriteToPlc(nextOrder.order1.numberOfCutsProduced, 0);
                    WriteToPlc(nextOrder.order1.numberOfCutsRemaining, 0);
                    WriteToPlc(nextOrder.order1.pileQuantity, obj.Order1PileQuantity ?? 0);
                    WriteToPlc(nextOrder.order1.pileQuantityRemaining, 0);
                    WriteToPlc(nextOrder.order1.pileCounter, 0);
                    WriteToPlc(nextOrder.order1.scrapCounter, 0);
                    WriteToPlc(nextOrder.order1.counterReset, false);

                    // ORDER 2
                    bool order2Enabled = (levelSelector == 3);

                    if (order2Enabled)
                    {
                        WriteToPlc(nextOrder.order2.id, obj.Order2Id ?? 0);
                        WriteToPlc(nextOrder.order2.client, obj.Order2Client ?? "");
                        WriteToPlc(nextOrder.order2.product, obj.Order2Product ?? "");
                        WriteToPlc(nextOrder.order2.sheetQuantity, obj.Order2SheetQuantity ?? 0);
                        WriteToPlc(nextOrder.order2.sheetType, ClampSheetType(obj.Order2SheetType));
                        WriteToPlc(nextOrder.order2.sheetM1, obj.Order2M1 ?? 0);
                        WriteToPlc(nextOrder.order2.sheetM2, obj.Order2M2 ?? 0);
                        WriteToPlc(nextOrder.order2.sheetM3, obj.Order2M3 ?? 0);
                        WriteToPlc(nextOrder.order2.sheetM4, obj.Order2M4 ?? 0);
                        WriteToPlc(nextOrder.order2.sheetM5, obj.Order2M5 ?? 0);
                        WriteToPlc(nextOrder.order2.sheetLength, obj.Order2SheetLength ?? 0);
                        WriteToPlc(nextOrder.order2.numberOfCuts, obj.Order2NumberOfCuts ?? 0);

                        WriteToPlc(nextOrder.order2.numberOfCutsProduced, 0);
                        WriteToPlc(nextOrder.order2.numberOfCutsRemaining, 0);
                        WriteToPlc(nextOrder.order2.pileQuantity, obj.Order2PileQuantity ?? 0);
                        WriteToPlc(nextOrder.order2.pileQuantityRemaining, 0);
                        WriteToPlc(nextOrder.order2.pileCounter, 0);
                        WriteToPlc(nextOrder.order2.scrapCounter, 0);
                        WriteToPlc(nextOrder.order2.counterReset, false);
                    }
                    else
                    {
                        // Clear
                        WriteToPlc(nextOrder.order2.id, 0);
                        WriteToPlc(nextOrder.order2.client, "");
                        WriteToPlc(nextOrder.order2.product, "");
                        WriteToPlc(nextOrder.order2.sheetQuantity, 0);
                        WriteToPlc(nextOrder.order2.sheetType, 0);
                        WriteToPlc(nextOrder.order2.sheetM1, 0);
                        WriteToPlc(nextOrder.order2.sheetM2, 0);
                        WriteToPlc(nextOrder.order2.sheetM3, 0);
                        WriteToPlc(nextOrder.order2.sheetM4, 0);
                        WriteToPlc(nextOrder.order2.sheetM5, 0);
                        WriteToPlc(nextOrder.order2.sheetLength, 0);
                        WriteToPlc(nextOrder.order2.numberOfCuts, 0);
                        WriteToPlc(nextOrder.order2.numberOfCutsProduced, 0);
                        WriteToPlc(nextOrder.order2.numberOfCutsRemaining, 0);
                        WriteToPlc(nextOrder.order2.pileQuantity, 0);
                        WriteToPlc(nextOrder.order2.pileQuantityRemaining, 0);
                        WriteToPlc(nextOrder.order2.pileCounter, 0);
                        WriteToPlc(nextOrder.order2.scrapCounter, 0);
                        WriteToPlc(nextOrder.order2.counterReset, false);
                    }

                    // Update cache ONLY after successful write
                    _lastNextOrderSignature = sig;

                    // optional legacy latch (still ok to keep)
                    NextOrderModel.tableID = obj.Id;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private int ClampSheetType(int? sheetType)
        {
            if (!sheetType.HasValue) return 0;
            if (sheetType.Value < 0) return 0;
            if (sheetType.Value > 2) return 2;
            return sheetType.Value;
        }

        // ============================================================
        // TIMERS
        // ============================================================
        private void tmrDateTime_Tick(object sender, EventArgs e)
        {
            lblDateTimeAct.Text = DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString();
        }


        private DateTime _lastMachineSpeedLoggedSlot = DateTime.MinValue;
        private void tmrLogMachineSpeed_Tick(object sender, EventArgs e)
        {
            try
            {
                // Guard: ADS not connected / symbols not ready
                if (tcClient == null || !tcClient.IsConnected || currentOrder == null)
                    return;

                // 1) Read speed from PLC
                int speed = ReadInt((DynamicSymbol)currentOrder.lineSpeed);

                // Optional clamp (your expected range 0..300)
                if (speed < 0) speed = 0;
                if (speed > 300) speed = 300;

                // 2) Force a stable 30s slot to avoid duplicates
                DateTime now = DateTime.Now;
                int secSlot = (now.Second < 60) ? 0 : 60;
                DateTime slot = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, secSlot);

                // If already logged this slot, do nothing
                if (slot <= _lastMachineSpeedLoggedSlot)
                    return;

                // 3) Write to SQL
                using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                {
                    const string sql = @"
INSERT INTO dbo.MachineSpeedRecords (Date_Time, Machine_Speed)
VALUES (@Date_Time, @Machine_Speed);";

                    db.Execute(sql, new
                    {
                        Date_Time = slot,
                        Machine_Speed = speed
                    });
                }

                // 4) Update last logged slot (only after successful insert)
                _lastMachineSpeedLoggedSlot = slot;
            }
            catch (Exception ex)
            {
                // Avoid MessageBox in a timer loop; log instead
                Debug.WriteLine("tmrLogMachineSpeed_Tick error: " + ex);
            }
        }

        // ============================================================
        // ORDER CHANGE HANDSHAKE (RISING EDGE)
        // ============================================================
        private void WireCurrentOrderEdgeHandler()
        {
            CurrentOrderUpdated += OnCurrentOrderUpdated;
        }

        private void OnCurrentOrderUpdated(order_TypeStruct snapshot)
        {
            bool req = snapshot?.changeOrderRequest ?? false;
            bool risingEdge = req && !_changeOrderRequestOld;
            _changeOrderRequestOld = req;

            if (!risingEdge) return;
            if (_changeOrderBusy) return;

            _changeOrderBusy = true;
            try
            {
                FinishCurrentOrderInDb_AndAckPlc();
            }
            finally
            {
                _changeOrderBusy = false;
            }

        }


        // Parses "yyyy-MM-dd HH:mm:ss" (the format you write), safely returns null if invalid.
        private DateTime? TryParsePlcTimestamp(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Adjust formats if your PLC uses another pattern
            string[] formats = new[]
            {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm"
    };

            if (DateTime.TryParseExact(
                    text.Trim(),
                    formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime dt))
            {
                return dt;
            }

            // Last resort: try normal parse (can be locale-dependent)
            if (DateTime.TryParse(text.Trim(), out dt))
                return dt;

            return null;
        }

        private void FinishCurrentOrderInDb_AndAckPlc()
        {
            try
            {
                int tableIdPlc = ReadInt(currentOrder.tableID);

                int levelSelector = ReadInt(currentOrder.levelSelector);
                bool order2Enabled = (levelSelector == 3);

                int o1Produced = ReadInt(currentOrder.order1.numberOfCutsProduced);
                int o2Produced = order2Enabled ? ReadInt(currentOrder.order2.numberOfCutsProduced) : 0;

                string startedAtText = Convert.ToString(ReadPlc((DynamicSymbol)currentOrder.startedAt)) ?? "";
                DateTime? startedAtDb = TryParsePlcTimestamp(startedAtText);

                DateTime finishedAt = DateTime.Now;

                const int productionStateInProduction = 1;
                const int productionStateFinished = 4;

                int nextTableId = ReadInt(nextOrder.tableID);

                bool skipFinishCurrent = false;

                using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                {
                    db.Open();

                    // ---------------------------------------------------------
                    //  TRANSACTION: guarantee "only one ProductionState = 1"
                    // ---------------------------------------------------------
                    using (var tx = db.BeginTransaction(IsolationLevel.Serializable))
                    {
                        // 0) Lock the "in production" rows to avoid race conditions
                        const string sqlCountInProduction = @"
SELECT COUNT(1)
FROM dbo.ProductionList_Plc WITH (UPDLOCK, HOLDLOCK)
WHERE ProductionState = @ps;";

                        int inProdCount = db.ExecuteScalar<int>(
                            sqlCountInProduction,
                            new { ps = productionStateInProduction },
                            transaction: tx
                        );


                        // 1) If there is more than one "in production", force them to Finished (4)
                        //    We'll do a generic cleanup first; then set the correct next one to 1 later.
                        if (inProdCount > 1)
                        {
                            const string sqlCleanupDuplicates = @"
UPDATE dbo.ProductionList_Plc
SET
    ProductionState = @psFinished,
    FinishedAt = COALESCE(FinishedAt, @finishedAt)
WHERE ProductionState = @psInProduction;";

                            db.Execute(sqlCleanupDuplicates, new
                            {
                                psFinished = productionStateFinished,
                                psInProduction = productionStateInProduction,
                                finishedAt
                            }, transaction: tx);
                        }

                        // 2) Validate CURRENT order Id (optional finish)
                        //    Re-check under lock to avoid stale reads
                        const string sqlGetCurrentInProductionId = @"
SELECT TOP 1 Id
FROM dbo.ProductionList_Plc WITH (UPDLOCK, HOLDLOCK)
WHERE ProductionState = @ps
ORDER BY StartedAt DESC, Id DESC;";

                        int dbCurrentInProductionId = db.ExecuteScalar<int>(
                            sqlGetCurrentInProductionId,
                            new { ps = productionStateInProduction },
                            transaction: tx
                        );

                        if (tableIdPlc <= 0 || dbCurrentInProductionId <= 0 || dbCurrentInProductionId != tableIdPlc)
                        {
                            skipFinishCurrent = true;
                        }

                        // 3) Finish current (only if validation OK)
                        if (!skipFinishCurrent)
                        {
                            const string sqlFinishCurrent = @"
UPDATE dbo.ProductionList_Plc
SET
    StartedAt                  = @startedAt,
    Order1NumberOfCutsProduced = @o1Produced,
    Order2NumberOfCutsProduced = @o2Produced,
    FinishedAt                 = @finishedAt,
    ProductionState            = @productionState
WHERE Id = @tableId;";

                            db.Execute(sqlFinishCurrent, new
                            {
                                tableId = tableIdPlc,
                                startedAt = startedAtDb,
                                o1Produced,
                                o2Produced,
                                finishedAt,
                                productionState = productionStateFinished
                            }, transaction: tx);
                        }

                        // 4) Start next (set ProductionState = 1)
                        if (nextTableId > 0)
                        {
                            const string sqlStartNext = @"
UPDATE dbo.ProductionList_Plc
SET
    ProductionState = @psInProduction,
    StartedAt = COALESCE(StartedAt, @startedAt)
WHERE Id = @nextTableId;";

                            db.Execute(sqlStartNext, new
                            {
                                nextTableId,
                                psInProduction = productionStateInProduction,
                                startedAt = DateTime.Now
                            }, transaction: tx);

                            // 5) HARD GUARANTEE: after starting next, no other row can remain = 1
                            //    If anything else is still = 1 (race/legacy), force it to 4.
                            const string sqlForceSingleInProduction = @"
UPDATE dbo.ProductionList_Plc
SET
    ProductionState = @psFinished,
    FinishedAt = COALESCE(FinishedAt, @finishedAt)
WHERE ProductionState = @psInProduction
  AND Id <> @nextTableId;";

                            db.Execute(sqlForceSingleInProduction, new
                            {
                                psFinished = productionStateFinished,
                                psInProduction = productionStateInProduction,
                                finishedAt,
                                nextTableId
                            }, transaction: tx);
                        }
                        else
                        {
                            // If there is no next, still ensure there are no ProductionState=1 leftovers
                            const string sqlNoNextCleanup = @"
UPDATE dbo.ProductionList_Plc
SET
    ProductionState = @psFinished,
    FinishedAt = COALESCE(FinishedAt, @finishedAt)
WHERE ProductionState = @psInProduction;";

                            db.Execute(sqlNoNextCleanup, new
                            {
                                psFinished = productionStateFinished,
                                psInProduction = productionStateInProduction,
                                finishedAt
                            }, transaction: tx);
                        }

                        tx.Commit();
                    }
                }

                // Optional warning AFTER DB transaction (don’t block the DB consistency)
                if (skipFinishCurrent)
                {
                    MessageBox.Show(
                        "Falha na gravação do pedido ATUAL (será ignorado).\n\n" +
                        "PLC currentOrder.tableID = " + tableIdPlc + "\n\n" +
                        "A operação do PRÓXIMO pedido continuará normalmente.",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // 6) Write start time to NEXT order in PLC (continue)
                string startTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                WriteToPlc(nextOrder.startedAt, startTimeText);

                // 7) PLC ACK
                WriteToPlc(currentOrder.saveSQLFinished, true);

                // 8) Notify screens + reload
                if (IsHandleCreated)
                    BeginInvoke(new Action(() => DatabaseItemsReloaded?.Invoke()));
                else
                    DatabaseItemsReloaded?.Invoke();

                LoadDatabaseItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateCurrentRunningOrderProducedCountsInDb()
        {
            try
            {
                // ------------------------------
                // 1) Read PLC snapshot
                // ------------------------------
                int tableIdPlc = ReadInt(currentOrder.tableID);
                if (tableIdPlc <= 0)
                    return;

                int levelSelector = ReadInt(currentOrder.levelSelector);
                bool order2Enabled = (levelSelector == 3);

                int o1Produced = ReadInt(currentOrder.order1.numberOfCutsProduced);
                int o2Produced = order2Enabled ? ReadInt(currentOrder.order2.numberOfCutsProduced) : 0;

                // ------------------------------
                // 2) Update DB (skip if not found / not current running)
                // ------------------------------
                const int PS_RunningCurrent = 2;

                const string sqlUpdateProduced = @"
UPDATE dbo.ProductionList_Plc
SET
    Order1NumberOfCutsProduced = @o1Produced,
    Order2NumberOfCutsProduced = @o2Produced
WHERE
    Id = @tableId
    AND ProductionState = @psRunning;";

                using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString))
                {
                    db.Open();

                    int rows = db.Execute(sqlUpdateProduced, new
                    {
                        tableId = tableIdPlc,
                        psRunning = PS_RunningCurrent,
                        o1Produced,
                        o2Produced
                    });

                    // If rows == 0: DB doesn't have this Id as "running current" (or missing row) -> skip.
                    // Optionally: log to Debug or a file if you want observability.
                }
            }
            catch
            {
                // Per requirement: if anything fails, do not block operation.
                // Optionally log exception (Debug.WriteLine / file / telemetry).
            }
        }


        // ============================================================
        // OPTIONAL: Original stub handlers you had
        // ============================================================
        private void pMain_Paint(object sender, PaintEventArgs e) { }
        private void label10_Click(object sender, EventArgs e) { }

        private void tmrUpdateNextOrder_Tick(object sender, EventArgs e)
        {

            WriteNextOrderToPlc();

        }

        private void tmrUpdateInterface_Tick(object sender, EventArgs e)
        {

        }

        private readonly BindingSource _currentOrderBsMain = new BindingSource();

        private void SetupCurrentOrderBindings_Main()
        {
            // Start binding to the current model instance
            _currentOrderBsMain.DataSource = CurrentOrderModel;

            // Bind line speed label text
            lbl_currentOrder_lineSpeed.DataBindings.Clear();

            var b = new Binding("Text", _currentOrderBsMain, "lineSpeed", true, DataSourceUpdateMode.Never);

            // Format -> string (and you can add units if you want)
            b.Format += (s, e) =>
            {
                int v = 0;
                try { v = Convert.ToInt32(e.Value); } catch { }
                e.Value = v.ToString(); // or $"{v} mpm"
            };

            lbl_currentOrder_lineSpeed.DataBindings.Add(b);
        }


    }
}
