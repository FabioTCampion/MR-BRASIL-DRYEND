using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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
        // SINGLETON / ADS CONFIGURATION
        // ============================================================
        public static frmMain instance;

        // Keep original IP from PROGRAM TO BE UPDATED
        public string adsAddres = "192.168.30.79.1.1";
        // public string adsAddres = "169.254.44.92.1.1";
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
        // PLC COMMUNICATION MONITOR
        // ============================================================
        private bool _wdLastValue;
        private DateTime _wdLastChangeTime = DateTime.MinValue;
        private DateTime _lastCurrentOrderUpdateTime = DateTime.MinValue;

        private bool _wdInitialized = false;
        private bool _connectionPopupOpen = false;
        private bool _plcCommunicationHealthy = false;

        // Watchdog pulse = 500 ms, timer = 1 s -> 3 s gives safe margin
        private readonly TimeSpan _watchdogTimeout = TimeSpan.FromSeconds(3);

        // If ValueChanged stops arriving for 3 s, consider communication stale
        private readonly TimeSpan _updateTimeout = TimeSpan.FromSeconds(3);

        // ============================================================
        // MODELS / THREADING
        // ============================================================
        private readonly object _orderLock = new object();

        public order_TypeStruct CurrentOrderModel { get; private set; } = new order_TypeStruct();
        public order_TypeStruct NextOrderModel { get; private set; } = new order_TypeStruct();

        public event Action<order_TypeStruct> CurrentOrderUpdated;

        // Change-order handshake protection
        private bool _changeOrderRequestOld = false;
        private bool _changeOrderBusy = false;

        // ============================================================
        // DATABASE / LOGGING
        // ============================================================
        private readonly MachineNotRunningTimePerHour machineStoppedTimeInstance = new MachineNotRunningTimePerHour();
        public event Action DatabaseItemsReloaded;

        private DateTime _lastMachineSpeedLoggedSlot = DateTime.MinValue;

        // ============================================================
        // BINDINGS
        // ============================================================
        private readonly BindingSource _currentOrderBsMain = new BindingSource();

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

            TryReconnectAds();

            SetupCurrentOrderBindings_Main();
            WireCurrentOrderEdgeHandler();

            RefreshPlcCommunicationState();
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
        // PLC COMMUNICATION STATE / TIMER GATING
        // ============================================================
        private bool IsAdsConnected()
        {
            try
            {
                return tcClient != null
                    && tcClient.IsConnected
                    && currentOrder != null
                    && nextOrder != null;
            }
            catch
            {
                return false;
            }
        }

        private bool IsPlcCommunicationHealthy()
        {
            if (!IsAdsConnected())
                return false;

            if (_lastCurrentOrderUpdateTime == DateTime.MinValue)
                return false;

            DateTime now = DateTime.Now;

            if (now - _lastCurrentOrderUpdateTime > _updateTimeout)
                return false;

            if (_wdInitialized && (now - _wdLastChangeTime > _watchdogTimeout))
                return false;

            return true;
        }

        private void SetPlcCommunicationState(bool healthy)
        {
            _plcCommunicationHealthy = healthy;

            // PLC-dependent timers
            tmrUpdateNextOrder.Enabled = healthy;
            tmrLogMachineSpeed.Enabled = healthy;
            tmr_UpdateCurrentOrder.Enabled = healthy;

            // Connection monitor must remain enabled
            tmr_CheckConnection.Enabled = true;
        }

        private void RefreshPlcCommunicationState()
        {
            SetPlcCommunicationState(IsPlcCommunicationHealthy());
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
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <= timeOutInMilliSeconds)
            {
                try
                {
                    if (tcSystemClient.ReadState().AdsState == state)
                        return true;
                }
                catch (AdsErrorException)
                {
                    // Ignore transient ADS errors while runtime is changing state
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
                if (_currentOrderSymbol != null)
                    _currentOrderSymbol.ValueChanged -= currentOrder_ValueChanged;

                if (_nextOrderSymbol != null)
                    _nextOrderSymbol.ValueChanged -= nextOrder_ValueChanged;
            }
            catch
            {
            }

            try { tcClient?.Dispose(); } catch { }
            try { tcSystemClient?.Dispose(); } catch { }
            try { _session?.Dispose(); } catch { }

            _plcCommunicationHealthy = false;
            SetPlcCommunicationState(false);
        }

        private void _session_ConnectionStateChanged(object sender, TwinCAT.ConnectionStateChangedEventArgs e)
        {
            lblConnectionState.Text = e.NewState.ToString();

            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    RefreshPlcCommunicationState();
                }));
            }
            else
            {
                RefreshPlcCommunicationState();
            }
        }

        private void TryReconnectAds()
        {
            try
            {
                DisposeAds();

                // Reset monitor BEFORE reconnect/subscription
                _wdInitialized = false;
                _lastCurrentOrderUpdateTime = DateTime.MinValue;
                _wdLastChangeTime = DateTime.MinValue;
                _wdLastValue = false;

                SetPlcCommunicationState(false);

                AdsConnect();

                lblConnectionState.Text = "Run";
            }
            catch (Exception ex)
            {
                lblConnectionState.Text = "Offline";
                SetPlcCommunicationState(false);

                MessageBox.Show(
                    "Falha ao reconectar.\n\n" + ex.Message,
                    "Reconexão",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // ADS VALUE CHANGED -> LOCAL MODEL
        // ============================================================
        private void currentOrder_ValueChanged(object sender, ValueChangedArgs e)
        {

            order_TypeStruct snapshot = PlcOrderMapper.FromPlc(e.Value);

            _lastCurrentOrderUpdateTime = DateTime.Now;

            bool wd = snapshot?.plcWatchDog ?? false;
            Debug.WriteLine($"WD event: {wd} at {DateTime.Now:HH:mm:ss.fff}");

            if (!_wdInitialized)
            {
                _wdInitialized = true;
                _wdLastValue = wd;
                _wdLastChangeTime = DateTime.Now;
            }
            else if (wd != _wdLastValue)
            {
                _wdLastValue = wd;
                _wdLastChangeTime = DateTime.Now;
            }

            lock (_orderLock)
            {
                CurrentOrderModel = snapshot;
            }

            RefreshPlcCommunicationState();

            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    _currentOrderBsMain.DataSource = snapshot;
                    _currentOrderBsMain.ResetBindings(false);

                    CurrentOrderUpdated?.Invoke(snapshot);
                }));
            }
        }

        private void nextOrder_ValueChanged(object sender, ValueChangedArgs e)
        {
            order_TypeStruct snapshot = PlcOrderMapper.FromPlc(e.Value);

            lock (_orderLock)
            {
                NextOrderModel = snapshot;
            }
        }

        // ============================================================
        // DATABASE LOADING
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
        // PUBLIC ADS WRITE HELPERS
        // ============================================================
        public void writeBoolToPlc(dynamic writeSymbol, bool value)
        {
            try
            {
                ((DynamicSymbol)writeSymbol).WriteValue(value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void writeResetButtonToPlc(dynamic writeSymbol)
        {
            try
            {
                ((DynamicSymbol)writeSymbol).WriteValue(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void writeSetButtonToPlc(dynamic writeSymbol)
        {
            try
            {
                ((DynamicSymbol)writeSymbol).WriteValue(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void writeIntToPlc(dynamic writeSymbol, int value)
        {
            try
            {
                ((DynamicSymbol)writeSymbol).WriteValue(value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void writeFloatToPlc(dynamic writeSymbol, float value)
        {
            try
            {
                ((DynamicSymbol)writeSymbol).WriteValue(value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // ============================================================
        // CENTRALIZED INTERNAL ADS READ / WRITE HELPERS
        // ============================================================
        private static object ReadPlc(DynamicSymbol sym)
        {
            return sym?.ReadValue();
        }

        private static int ToIntSafe(object value)
        {
            if (value == null)
                return 0;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private static bool ToBoolSafe(object value)
        {
            if (value == null)
                return false;

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return false;
            }
        }

        private static void WriteToPlc(DynamicSymbol variable, object value)
        {
            if (variable == null)
                return;

            variable.WriteValue(value ?? 0);
        }

        private static int ReadInt(DynamicSymbol sym)
        {
            return ToIntSafe(ReadPlc(sym));
        }

        // ============================================================
        // NEXT ORDER -> PLC
        // ============================================================
        private int ComputeNextOrderSignature(ProductionListPlc obj)
        {
            unchecked
            {
                int h = 17;

                void AddInt(int v) => h = (h * 31) + v;
                void AddStr(string s) => h = (h * 31) + (s == null ? 0 : s.GetHashCode());

                int levelSelector = obj.LevelSelector ?? 0;
                bool order2Enabled = (levelSelector == 3);

                // Header / composition
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

                // Order 1
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

                // Order 2
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
            if (!_plcCommunicationHealthy)
                return;

            LoadDatabaseItems();

            var obj = nextOrderBindingSource.Current as ProductionListPlc;
            if (obj == null)
                return;

            int sig = ComputeNextOrderSignature(obj);

            lock (_nextOrderWriteLock)
            {
                if (_lastNextOrderSignature == sig)
                    return;

                try
                {
                    // Header / composition
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

                    // Order 1
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

                    // Reset runtime counters on write (behavior preserved from PROGRAM TO BE UPDATED)
                    WriteToPlc(nextOrder.order1.numberOfCutsProduced, 0);
                    WriteToPlc(nextOrder.order1.numberOfCutsRemaining, 0);
                    WriteToPlc(nextOrder.order1.pileQuantity, obj.Order1PileQuantity ?? 0);
                    WriteToPlc(nextOrder.order1.pileQuantityRemaining, 0);
                    WriteToPlc(nextOrder.order1.pileCounter, 0);
                    WriteToPlc(nextOrder.order1.scrapCounter, 0);
                    WriteToPlc(nextOrder.order1.counterReset, false);

                    // Order 2
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

                    _lastNextOrderSignature = sig;
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
            if (!sheetType.HasValue)
                return 0;

            if (sheetType.Value < 0)
                return 0;

            if (sheetType.Value > 2)
                return 2;

            return sheetType.Value;
        }

        // ============================================================
        // CHANGE ORDER HANDSHAKE
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

            if (!risingEdge)
                return;

            if (_changeOrderBusy)
                return;

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

        private DateTime? TryParsePlcTimestamp(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            string[] formats =
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

            if (DateTime.TryParse(text.Trim(), out dt))
                return dt;

            return null;
        }

        private void FinishCurrentOrderInDb_AndAckPlc()
        {
            if (!_plcCommunicationHealthy)
                return;

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

                    using (var tx = db.BeginTransaction(IsolationLevel.Serializable))
                    {
                        const string sqlCountInProduction = @"
SELECT COUNT(1)
FROM dbo.ProductionList_Plc WITH (UPDLOCK, HOLDLOCK)
WHERE ProductionState = @ps;";

                        int inProdCount = db.ExecuteScalar<int>(
                            sqlCountInProduction,
                            new { ps = productionStateInProduction },
                            transaction: tx);

                        if (inProdCount > 1)
                        {
                            const string sqlCleanupDuplicates = @"
UPDATE dbo.ProductionList_Plc
SET
    ProductionState = @psFinished,
    FinishedAt = COALESCE(FinishedAt, @finishedAt)
WHERE ProductionState = @psInProduction;";

                            db.Execute(
                                sqlCleanupDuplicates,
                                new
                                {
                                    psFinished = productionStateFinished,
                                    psInProduction = productionStateInProduction,
                                    finishedAt
                                },
                                transaction: tx);
                        }

                        const string sqlGetCurrentInProductionId = @"
SELECT TOP 1 Id
FROM dbo.ProductionList_Plc WITH (UPDLOCK, HOLDLOCK)
WHERE ProductionState = @ps
ORDER BY StartedAt DESC, Id DESC;";

                        int dbCurrentInProductionId = db.ExecuteScalar<int>(
                            sqlGetCurrentInProductionId,
                            new { ps = productionStateInProduction },
                            transaction: tx);

                        if (tableIdPlc <= 0 || dbCurrentInProductionId <= 0 || dbCurrentInProductionId != tableIdPlc)
                        {
                            skipFinishCurrent = true;
                        }

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

                            db.Execute(
                                sqlFinishCurrent,
                                new
                                {
                                    tableId = tableIdPlc,
                                    startedAt = startedAtDb,
                                    o1Produced,
                                    o2Produced,
                                    finishedAt,
                                    productionState = productionStateFinished
                                },
                                transaction: tx);
                        }

                        if (nextTableId > 0)
                        {
                            const string sqlStartNext = @"
UPDATE dbo.ProductionList_Plc
SET
    ProductionState = @psInProduction,
    StartedAt = COALESCE(StartedAt, @startedAt)
WHERE Id = @nextTableId;";

                            db.Execute(
                                sqlStartNext,
                                new
                                {
                                    nextTableId,
                                    psInProduction = productionStateInProduction,
                                    startedAt = DateTime.Now
                                },
                                transaction: tx);

                            const string sqlForceSingleInProduction = @"
UPDATE dbo.ProductionList_Plc
SET
    ProductionState = @psFinished,
    FinishedAt = COALESCE(FinishedAt, @finishedAt)
WHERE ProductionState = @psInProduction
  AND Id <> @nextTableId;";

                            db.Execute(
                                sqlForceSingleInProduction,
                                new
                                {
                                    psFinished = productionStateFinished,
                                    psInProduction = productionStateInProduction,
                                    finishedAt,
                                    nextTableId
                                },
                                transaction: tx);
                        }
                        else
                        {
                            const string sqlNoNextCleanup = @"
UPDATE dbo.ProductionList_Plc
SET
    ProductionState = @psFinished,
    FinishedAt = COALESCE(FinishedAt, @finishedAt)
WHERE ProductionState = @psInProduction;";

                            db.Execute(
                                sqlNoNextCleanup,
                                new
                                {
                                    psFinished = productionStateFinished,
                                    psInProduction = productionStateInProduction,
                                    finishedAt
                                },
                                transaction: tx);
                        }

                        tx.Commit();
                    }
                }

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

                string startTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                WriteToPlc(nextOrder.startedAt, startTimeText);

                WriteToPlc(currentOrder.saveSQLFinished, true);

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
            if (!_plcCommunicationHealthy)
                return;

            try
            {
                int tableIdPlc = ReadInt(currentOrder.tableID);
                if (tableIdPlc <= 0)
                    return;

                int levelSelector = ReadInt(currentOrder.levelSelector);
                bool order2Enabled = (levelSelector == 3);

                int o1Produced = ReadInt(currentOrder.order1.numberOfCutsProduced);
                int o2Produced = order2Enabled ? ReadInt(currentOrder.order2.numberOfCutsProduced) : 0;

                // Preserved from PROGRAM TO BE UPDATED
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

                    db.Execute(
                        sqlUpdateProduced,
                        new
                        {
                            tableId = tableIdPlc,
                            psRunning = PS_RunningCurrent,
                            o1Produced,
                            o2Produced
                        });
                }
            }
            catch
            {
                // Do not block machine operation if background sync fails
            }
        }

        // ============================================================
        // UI BINDINGS
        // ============================================================
        private void SetupCurrentOrderBindings_Main()
        {
            _currentOrderBsMain.DataSource = CurrentOrderModel;

            lbl_currentOrder_lineSpeed.DataBindings.Clear();

            Binding b = new Binding("Text", _currentOrderBsMain, "lineSpeed", true, DataSourceUpdateMode.Never);

            b.Format += (s, e) =>
            {
                int v = 0;

                try
                {
                    v = Convert.ToInt32(e.Value);
                }
                catch
                {
                }

                e.Value = v.ToString();
            };

            lbl_currentOrder_lineSpeed.DataBindings.Add(b);
        }

        // ============================================================
        // TIMERS
        // ============================================================
        private void tmrDateTime_Tick(object sender, EventArgs e)
        {
            lblDateTimeAct.Text = DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString();
        }

        private void tmrUpdateNextOrder_Tick(object sender, EventArgs e)
        {
            if (!_plcCommunicationHealthy)
                return;

            WriteNextOrderToPlc();
        }

        private void tmrLogMachineSpeed_Tick(object sender, EventArgs e)
        {
            if (!_plcCommunicationHealthy)
                return;

            try
            {
                if (tcClient == null || !tcClient.IsConnected || currentOrder == null)
                    return;

                int speed = ReadInt((DynamicSymbol)currentOrder.lineSpeed);

                if (speed < 0) speed = 0;
                if (speed > 300) speed = 300;

                DateTime now = DateTime.Now;

                // Stable 30-second slots: second 0 or 30
                int secSlot = (now.Second < 30) ? 0 : 30;
                DateTime slot = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, secSlot);

                if (slot <= _lastMachineSpeedLoggedSlot)
                    return;

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

                _lastMachineSpeedLoggedSlot = slot;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("tmrLogMachineSpeed_Tick error: " + ex);
            }
        }

        private void tmr_UpdateCurrentOrder_Tick(object sender, EventArgs e)
        {
            if (!_plcCommunicationHealthy)
                return;

            UpdateCurrentRunningOrderProducedCountsInDb();
        }

        private void tmr_CheckConnection_Tick(object sender, EventArgs e)
        {
            if (_connectionPopupOpen)
                return;

            DateTime now = DateTime.Now;

            if (!IsAdsConnected())
            {
                SetPlcCommunicationState(false);
                ShowReconnectPopup("Sem conexão ADS com o PLC.");
                return;
            }

            //if (_lastCurrentOrderUpdateTime == DateTime.MinValue)
            //{
            //    SetPlcCommunicationState(false);
            //    ShowReconnectPopup("Conectado ao ADS, mas ainda sem atualização do PLC.");
            //    return;
            //}

            //if (now - _lastCurrentOrderUpdateTime > _updateTimeout)
            //{
            //    SetPlcCommunicationState(false);
            //    ShowReconnectPopup("Sem atualização do PLC (ValueChanged parou).");
            //    return;
            //}

            if (_wdInitialized && (now - _wdLastChangeTime > _watchdogTimeout))
            {
                SetPlcCommunicationState(false);
                ShowReconnectPopup("Watchdog do PLC travado (dados congelados).");
                return;
            }

            RefreshPlcCommunicationState();
        }

        private void ShowReconnectPopup(string message)
        {
            if (_connectionPopupOpen)
                return;

            _connectionPopupOpen = true;
            tmr_CheckConnection.Stop();

            try
            {
                MessageBox.Show(
                    message + "\n\nPressione OK para tentar reconectar.",
                    "Conexão PLC",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                TryReconnectAds();
            }
            finally
            {
                _connectionPopupOpen = false;
                tmr_CheckConnection.Start();
            }
        }

        // ============================================================
        // OPTIONAL / DESIGNER STUBS
        // ============================================================
        private void pMain_Paint(object sender, PaintEventArgs e)
        {
        }

        private void label10_Click(object sender, EventArgs e)
        {
        }

        private void tmrUpdateInterface_Tick(object sender, EventArgs e)
        {
        }
    }
}