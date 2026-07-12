using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace Hmi.Data
{
    /// <summary>
    /// Data model mapped to SQL table: dbo.ProductionList_Plc.
    /// Naming is intentionally aligned to PLC field naming to keep ADS <-> DB <-> HMI mapping straightforward.
    ///
    /// Conventions / field semantics:
    /// - SheetType (PLC INT):
    ///     0 = None / Not used
    ///     1 = Two Scorers
    ///     2 = Four Scorers
    /// - LevelSelector (PLC INT):
    ///     1 = Order1 only (Upper/Top)
    ///     2 = Order2 only (Lower/Bottom)
    ///     3 = Both (Order1 + Order2)
    /// - ProductionListNumber is stored as VARCHAR in SQL and kept as string here to avoid parsing issues.
    /// - StartedAt / FinishedAt are nullable in SQL and kept as DateTime? to preserve DB truth.
    ///
    /// Notes:
    /// - "UI helper fields" are display-only and are not persisted unless corresponding SQL columns exist.
    /// - If you later decide to persist descriptions/details, add columns and update your CRUD accordingly.
    /// </summary>
    public class ProductionListPlc
    {
        /// <summary>
        /// Primary key (IDENTITY in SQL).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Production queue order. Typically used to sort "next orders" (ascending).
        /// </summary>
        public int? ProductionSequence { get; set; }

        /// <summary>
        /// Production state used by the HMI/PLC integration.
        /// Common usage in your project:
        /// - 0 = Programmed by PCP
        /// - 1 = Programmed by operator (production queue)
        /// - 2 = Current running order
        /// - 3 = Order finished (typically Order1NumberOfCutsProduced >= Order1NumberOfCuts)
        /// - 4 = Order not finished (can be finished later)
        /// </summary>
        public int? ProductionState { get; set; }


        /// <summary>
        /// Accumulated machine-not-running time for this production item (units defined by your DB logic).
        /// </summary>
        public int? MachineNotRunningTime { get; set; }

        /// <summary>
        /// Timestamp when this production item started (nullable in DB).
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Timestamp when this production item finished (nullable in DB).
        /// </summary>
        public DateTime? FinishedAt { get; set; }

        /// <summary>
        /// Paper composition string (e.g., recipe / stack description used by operators).
        /// </summary>
        public string PaperComposition { get; set; }

        /// <summary>
        /// Flute type (e.g., A/B/C/E or your internal code).
        /// </summary>
        public string FluteType { get; set; }

        /// <summary>
        /// Paper width (typically mm), if applicable for the order set.
        /// </summary>
        public int? PaperWidth { get; set; }

        /// <summary>Paper layer 1 identifier/name.</summary>
        public string Paper1 { get; set; }

        /// <summary>Paper layer 2 identifier/name.</summary>
        public string Paper2 { get; set; }

        /// <summary>Paper layer 3 identifier/name.</summary>
        public string Paper3 { get; set; }

        /// <summary>Paper layer 4 identifier/name.</summary>
        public string Paper4 { get; set; }

        /// <summary>Paper layer 5 identifier/name.</summary>
        public string Paper5 { get; set; }

        /// <summary>
        /// External production list identifier (VARCHAR in SQL). Kept as string to avoid conversion issues.
        /// </summary>
        public string ProductionListNumber { get; set; }

        // -------------------------
        // Order 1 (PLC naming)
        // -------------------------

        /// <summary>Order1 internal identifier (nullable depending on workflow).</summary>
        public int? Order1Id { get; set; }

        /// <summary>Order1 product / item description.</summary>
        public string Order1Product { get; set; }

        /// <summary>Order1 client / customer name.</summary>
        public string Order1Client { get; set; }

        /// <summary>Order1 sheet quantity (planned).</summary>
        public int? Order1SheetQuantity { get; set; }

        /// <summary>
        /// Order1 sheet type (PLC INT):
        /// 0=None, 1=Two Scorers, 2=Four Scorers
        /// </summary>
        public int? Order1SheetType { get; set; }

        /// <summary>Order1 score position M1 (units defined by PLC/recipe; typically mm).</summary>
        public int? Order1M1 { get; set; }

        /// <summary>Order1 score position M2.</summary>
        public int? Order1M2 { get; set; }

        /// <summary>Order1 score position M3.</summary>
        public int? Order1M3 { get; set; }

        /// <summary>Order1 score position M4.</summary>
        public int? Order1M4 { get; set; }

        /// <summary>Order1 score position M5.</summary>
        public int? Order1M5 { get; set; }

        /// <summary>Order1 sheet length (typically mm).</summary>
        public int? Order1SheetLength { get; set; }

        /// <summary>Order1 number of cuts (planned total).</summary>
        public int? Order1NumberOfCuts { get; set; }

        /// <summary>Order1 number of cuts already produced (counter snapshot).</summary>
        public int? Order1NumberOfCutsProduced { get; set; }

        /// <summary>Order1 pile quantity (target sheets per pile / stack).</summary>
        public int? Order1PileQuantity { get; set; }

        // -------------------------
        // PLC selector
        // -------------------------

        /// <summary>
        /// PLC level selector (PLC INT):
        /// 1=Order1 only, 2=Order2 only, 3=Both
        /// Used by HMI logic to decide which order blocks are active/visible.
        /// </summary>
        public int? LevelSelector { get; set; }

        // -------------------------
        // Order 2 (PLC naming)
        // -------------------------

        /// <summary>Order2 internal identifier (nullable depending on workflow).</summary>
        public int? Order2Id { get; set; }

        /// <summary>Order2 product / item description.</summary>
        public string Order2Product { get; set; }

        /// <summary>Order2 client / customer name.</summary>
        public string Order2Client { get; set; }

        /// <summary>Order2 sheet quantity (planned).</summary>
        public int? Order2SheetQuantity { get; set; }

        /// <summary>
        /// Order2 sheet type (PLC INT):
        /// 0=None, 1=Two Scorers, 2=Four Scorers
        /// </summary>
        public int? Order2SheetType { get; set; }

        /// <summary>Order2 score position M1 (units defined by PLC/recipe; typically mm).</summary>
        public int? Order2M1 { get; set; }

        /// <summary>Order2 score position M2.</summary>
        public int? Order2M2 { get; set; }

        /// <summary>Order2 score position M3.</summary>
        public int? Order2M3 { get; set; }

        /// <summary>Order2 score position M4.</summary>
        public int? Order2M4 { get; set; }

        /// <summary>Order2 score position M5.</summary>
        public int? Order2M5 { get; set; }

        /// <summary>Order2 sheet length (typically mm).</summary>
        public int? Order2SheetLength { get; set; }

        /// <summary>Order2 number of cuts (planned total).</summary>
        public int? Order2NumberOfCuts { get; set; }

        /// <summary>Order2 number of cuts already produced (counter snapshot).</summary>
        public int? Order2NumberOfCutsProduced { get; set; }

        /// <summary>Order2 pile quantity (target sheets per pile / stack).</summary>
        public int? Order2PileQuantity { get; set; }

        // -------------------------
        // UI helper fields (NOT mapped to DB unless columns exist)
        // -------------------------

        /// <summary>
        /// Free-form details used for HMI display (e.g., composition summary, width/trim, notes).
        /// Not persisted unless you add a SQL column and include it in CRUD queries.
        /// </summary>
        public string OrderDetails { get; set; }

        /// <summary>
        /// Prebuilt Order1 description string for UI list/cards.
        /// Not persisted unless you add a SQL column and include it in CRUD queries.
        /// </summary>
        public string Order1Description { get; set; }

        /// <summary>
        /// Prebuilt Order2 description string for UI list/cards.
        /// Not persisted unless you add a SQL column and include it in CRUD queries.
        /// </summary>
        public string Order2Description { get; set; }
    }

    /// <summary>
    /// Repository for dbo.ProductionList_Plc using Dapper.
    /// Provides focused read methods used by the HMI:
    /// - GetCurrent(): current running order item
    /// - GetNextList(): queued/next order list for operator selection and PLC download
    ///
    /// Notes:
    /// - Keep SQL filters consistent with your application state model (ProductionState meanings).
    /// - If your ProductionState mapping evolves, update these queries first to avoid UI confusion.
    /// </summary>
    public class ProductionListPlcRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Reads the connection string "cn" from App.config / Web.config.
        /// </summary>
        public ProductionListPlcRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["cn"].ConnectionString;
        }

        /// <summary>
        /// Creates a new SQL connection instance.
        /// Caller methods wrap usage in a using{} to ensure proper disposal.
        /// </summary>
        public IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);

        /// <summary>
        /// Returns the current running order item, based on ProductionState = 3.
        /// If multiple rows exist, the most recent (TOP 1) is returned according to SQL Server default ordering
        /// (consider adding an ORDER BY if you want deterministic selection, e.g., StartedAt DESC or Id DESC).
        /// </summary>
        public ProductionListPlc GetCurrent()
        {
            using (var db = CreateConnection())
            {
                return db.QueryFirstOrDefault<ProductionListPlc>(
                    "SELECT TOP 1 * FROM dbo.ProductionList_Plc WHERE ProductionState = 3");
            }
        }

        /// <summary>
        /// Returns the queued "next orders" list.
        /// Default filter mirrors your HMI logic:
        /// - ProductionState <= 1
        /// - ProductionSequence > 0
        /// Ordered by ProductionSequence ASC (first in queue first).
        /// </summary>
        public System.Collections.Generic.List<ProductionListPlc> GetNextList(int top = 100)
        {
            using (var db = CreateConnection())
            {
                return db.Query<ProductionListPlc>(
                    $"SELECT TOP {top} * FROM dbo.ProductionList_Plc WHERE (ProductionState <= 1) AND (ProductionSequence > 0) ORDER BY ProductionSequence ASC"
                ).AsList();
            }
        }
    }
}
