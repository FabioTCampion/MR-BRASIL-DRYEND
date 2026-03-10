using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dapper;
using Hmi.Data;

namespace Hmi
{
    public partial class frmHist : Form
    {
        public static frmHist instance;

        // Keep if you still use it elsewhere
        private EntityState objState = EntityState.Unchanged;

        // Table used by the history screen
        private const string HistoryTableName = "dbo.ProductionList_Plc";

        // Business rule: 1=Order1, 2=Order2, 3=Both
        private const int LevelOrder1 = 1;
        private const int LevelOrder2 = 2;
        private const int LevelBoth = 3;

        private const int HistoryMinProductionState = 4; // "ProductionState > 3"

        public frmHist()
        {
            InitializeComponent();
            instance = this;

            // Optional: wire events here if not done in Designer
            // this.Enter += frmHist_Enter;
            // btnSearch.Click += btnSearch_Click;
            // btnClearFilter.Click += btnClearFilter_Click;
        }

        // =====================================================================
        // FORM LIFECYCLE
        // =====================================================================
        private void frmOrders_Load(object sender, EventArgs e)
        {
            // If you want to load something on open:
            // LoadDatabaseItems();
        }

        private void frmHist_Enter(object sender, EventArgs e)
        {
            // Default behavior from your original code
            SearchByDate(DateTime.Now);
            UpdateOrderDescriptionAndTotals();
        }

        // =====================================================================
        // DATABASE
        // =====================================================================
        private IDbConnection OpenConnection()
        {
            var db = new SqlConnection(ConfigurationManager.ConnectionStrings["cn"].ConnectionString);
            if (db.State == ConnectionState.Closed) db.Open();
            return db;
        }

        private void LoadDatabaseItems()
        {
            try
            {
                using (var db = OpenConnection())
                {
                    string sql = $@"
SELECT TOP 100 *
FROM {HistoryTableName}
WHERE ProductionState >= @MinState
ORDER BY FinishedAt DESC;";

                    var list = db.Query<ProductionListPlc>(sql, new { MinState = HistoryMinProductionState }).ToList();
                    productionListBindingSource.DataSource = list;
                }

                UpdateOrderDescriptionAndTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================================
        // SEARCH UI
        // =====================================================================
        private enum SearchMode
        {
            None,
            ClientName,
            Composition,
            ProductionListNumber,
            OF,
            Product,
            ByDate
        }

        private SearchMode GetSelectedSearchMode()
        {
            if (ckbSearchByDate.Checked) return SearchMode.ByDate;
            if (ckbSearchClientName.Checked) return SearchMode.ClientName;
            if (ckbSearchComposition.Checked) return SearchMode.Composition;
            if (ckbSearchListNumber.Checked) return SearchMode.ProductionListNumber;
            if (ckbSearchOF.Checked) return SearchMode.OF;
            if (ckbSearchProduct.Checked) return SearchMode.Product;

            return SearchMode.None;
        }

        private void SetExclusiveCheckBox(CheckBox selected)
        {
            // Single place to enforce "only one checkbox at a time"
            var all = new[]
            {
                ckbSearchClientName,
                ckbSearchComposition,
                ckbSearchListNumber,
                ckbSearchOF,
                ckbSearchProduct,
                ckbSearchWidth,     // kept because you had it in the UI
                ckbSearchByDate
            };

            foreach (var cb in all)
            {
                if (cb == null) continue;
                if (!ReferenceEquals(cb, selected)) cb.Checked = false;
                cb.Refresh();
            }

            // Enable text input when mode requires it
            var mode = GetSelectedSearchMode();
            txtSearch.Enabled = (mode != SearchMode.None && mode != SearchMode.ByDate);
        }

        private void ckbSearchCheckedChanged(object sender, EventArgs e)
        {
            var selected = sender as CheckBox;
            if (selected == null || !selected.Checked)
                return;

            SetExclusiveCheckBox(selected);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                var mode = GetSelectedSearchMode();
                switch (mode)
                {
                    case SearchMode.ClientName:
                        SearchClientName(txtSearch.Text);
                        break;

                    case SearchMode.Composition:
                        SearchComposition(txtSearch.Text);
                        break;

                    case SearchMode.ProductionListNumber:
                        SearchProductionList(txtSearch.Text);
                        break;

                    case SearchMode.OF:
                        SearchOFNumber(txtSearch.Text);
                        break;

                    case SearchMode.Product:
                        SearchProduct(txtSearch.Text);
                        break;

                    case SearchMode.ByDate:
                        SearchByDate(dateTimePicker1.Value);
                        break;

                    default:
                        // No filter selected -> load default view
                        LoadDatabaseItems();
                        return;
                }

                UpdateOrderDescriptionAndTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            LoadDatabaseItems();
        }

        // =====================================================================
        // SEARCH QUERIES (parameterized)
        // =====================================================================
        private void SearchClientName(string filter)
        {
            string sql = $@"
SELECT TOP 100 *
FROM {HistoryTableName}
WHERE ProductionState >= @MinState
  AND (Order1Client LIKE @p OR Order2Client LIKE @p)
ORDER BY FinishedAt DESC;";

            RunSearch(sql, new { MinState = HistoryMinProductionState, p = Like(filter) });
        }

        private void SearchOFNumber(string filter)
        {
            string sql = $@"
SELECT TOP 100 *
FROM {HistoryTableName}
WHERE ProductionState >= @MinState
  AND (CAST(Order1Id AS VARCHAR(50)) LIKE @p OR CAST(Order2Id AS VARCHAR(50)) LIKE @p)
ORDER BY FinishedAt DESC;";

            RunSearch(sql, new { MinState = HistoryMinProductionState, p = Like(filter) });
        }

        private void SearchProduct(string filter)
        {
            string sql = $@"
SELECT TOP 100 *
FROM {HistoryTableName}
WHERE ProductionState >= @MinState
  AND (Order1Product LIKE @p OR Order2Product LIKE @p)
ORDER BY FinishedAt DESC;";

            RunSearch(sql, new { MinState = HistoryMinProductionState, p = Like(filter) });
        }

        private void SearchProductionList(string filter)
        {
            // Keep column name exactly as in your DB mapping (your earlier code used ProductionListNumber)
            string sql = $@"
SELECT TOP 100 *
FROM {HistoryTableName}
WHERE ProductionState >= @MinState
  AND ProductionListNumber LIKE @p
ORDER BY FinishedAt DESC;";

            RunSearch(sql, new { MinState = HistoryMinProductionState, p = Like(filter) });
        }

        private void SearchComposition(string filter)
        {
            string sql = $@"
SELECT TOP 100 *
FROM {HistoryTableName}
WHERE ProductionState >= @MinState
  AND PaperComposition LIKE @p
ORDER BY FinishedAt DESC;";

            RunSearch(sql, new { MinState = HistoryMinProductionState, p = Like(filter) });
        }

        private void SearchByDate(DateTime filterDate)
        {
            string sql = $@"
SELECT TOP 100 *
FROM {HistoryTableName}
WHERE ProductionState >= @MinState
  AND CONVERT(DATE, StartedAt) = @FilterDate
ORDER BY StartedAt DESC;";

            RunSearch(sql, new { MinState = HistoryMinProductionState, FilterDate = filterDate.Date });
        }

        private void RunSearch(string sql, object param)
        {
            try
            {
                using (var db = OpenConnection())
                {
                    var list = db.Query<ProductionListPlc>(sql, param).ToList();
                    productionListBindingSource.DataSource = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Like(string s) => $"%{(s ?? string.Empty).Trim()}%";

        // =====================================================================
        // DESCRIPTION / TOTALS
        // =====================================================================
        private static int Nz(int? v) => v ?? 0;
        private static string Nz(string v) => v ?? "";
        private static DateTime NzDt(DateTime? v) => v ?? DateTime.MinValue;

        private static bool IsOrder2Enabled(int levelSelector)
        {
            // Safer rule (covers Both and Order2-only)
            return levelSelector == LevelBoth || levelSelector == LevelOrder2;
        }

        private static string SheetTypeText(int sheetType)
        {
            // PLC enum: 0=None, 1=Two Scorers, 2=Four Scorers
            switch (sheetType)
            {
                case 0: return "SEM VINCOS";
                case 1: return "DOIS VINCOS";
                case 2: return "QUATRO VINCOS";
                default: return "DESCONHECIDO";
            }
        }

        private string BuildOrderDescription(ProductionListPlc p, bool order1)
        {
            // Builds the block for the grid/list: client/product/OF/measure + production counters.
            // PT-BR: Gera o texto consolidado do pedido (1 ou 2) para exibição.

            int level = Nz(p.LevelSelector);
            if (!order1 && !IsOrder2Enabled(level))
                return "DESABILITADO";

            string client = order1 ? Nz(p.Order1Client) : Nz(p.Order2Client);
            string product = order1 ? Nz(p.Order1Product) : Nz(p.Order2Product);
            string of = order1 ? p.Order1Id.ToString() : p.Order2Id.ToString();

            int sheetQty = order1 ? Nz(p.Order1SheetQuantity) : Nz(p.Order2SheetQuantity);
            int sheetType = order1 ? Nz(p.Order1SheetType) : Nz(p.Order2SheetType);
            int sheetLen = order1 ? Nz(p.Order1SheetLength) : Nz(p.Order2SheetLength);

            int totalCuts = order1 ? Nz(p.Order1NumberOfCuts) : Nz(p.Order2NumberOfCuts);
            int producedCuts = order1 ? Nz(p.Order1NumberOfCutsProduced) : Nz(p.Order2NumberOfCutsProduced);
            int remainingCuts = Math.Max(0, totalCuts - producedCuts);

            int m1 = order1 ? Nz(p.Order1M1) : Nz(p.Order2M1);
            int m2 = order1 ? Nz(p.Order1M2) : Nz(p.Order2M2);
            int m3 = order1 ? Nz(p.Order1M3) : Nz(p.Order2M3);
            int m4 = order1 ? Nz(p.Order1M4) : Nz(p.Order2M4);
            int m5 = order1 ? Nz(p.Order1M5) : Nz(p.Order2M5);

            var sb = new StringBuilder(256);
            sb.Append("CLIENTE: ").Append(client).Append('\n');
            sb.Append("PRODUTO: ").Append(product).Append('\n');
            sb.Append("Nº DA OF: ").Append(of).Append('\n');

            // MEDIDA: qty x M1[*M2*M3][*M4*M5]
            sb.Append("MEDIDA: ").Append(sheetQty).Append(" x ").Append(m1);

            if (sheetType >= 1) sb.Append('*').Append(m2).Append('*').Append(m3);
            if (sheetType >= 2) sb.Append('*').Append(m4).Append('*').Append(m5);

            sb.Append('\n');
            sb.Append("TIPO: ").Append(SheetTypeText(sheetType)).Append('\n');
            sb.Append("COMPRIMENTO: ").Append(sheetLen).Append('\n');
            sb.Append("Nº DE CORTES: ").Append(totalCuts).Append('\n');
            sb.Append("PRODUZIDOS: ").Append(producedCuts).Append('\n');
            sb.Append("SALDO: ").Append(remainingCuts);

            return sb.ToString();
        }

        private string BuildOrder1Description(ProductionListPlc p) => BuildOrderDescription(p, order1: true);
        private string BuildOrder2Description(ProductionListPlc p) => BuildOrderDescription(p, order1: false);

        private string BuildOrderDetailDescription(ProductionListPlc p)
        {
            // Details block: dates, elapsed, trim, composition, produced meters and m².
            // PT-BR: Detalhes: datas, tempo, refile, composição, metros lineares e m² produzidos.

            int paperWidth = Nz(p.PaperWidth);

            int o1Width = (Nz(p.Order1M1) + Nz(p.Order1M2) + Nz(p.Order1M3) + Nz(p.Order1M4) + Nz(p.Order1M5)) * Nz(p.Order1SheetQuantity);

            int o2Width = 0;
            if (IsOrder2Enabled(Nz(p.LevelSelector)))
            {
                o2Width = (Nz(p.Order2M1) + Nz(p.Order2M2) + Nz(p.Order2M3) + Nz(p.Order2M4) + Nz(p.Order2M5)) * Nz(p.Order2SheetQuantity);
            }

            int totalWidth = o1Width + o2Width;

            int trim = paperWidth - totalWidth;
            if (trim < 0) trim = 0;

            int trimPct = (paperWidth > 0) ? (trim * 100) / paperWidth : 0;

            DateTime startedAt = NzDt(p.StartedAt);
            DateTime finishedAt = NzDt(p.FinishedAt);

            TimeSpan elapsed = finishedAt - startedAt;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

            string elapsedText = elapsed.Days > 0
                ? $"{elapsed.Days} DIAS - {elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
                : $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

            // Produced linear meters (based on Order1 produced cuts)
            double linearMetersProduced = (Nz(p.Order1SheetLength) * (double)Nz(p.Order1NumberOfCutsProduced)) / 1000.0;
            double m2Produced = linearMetersProduced * (paperWidth / 1000.0);

            var sb = new StringBuilder(256);
            sb.Append("DATA: ").Append(startedAt).Append(" -> ").Append(finishedAt).Append('\n');
            sb.Append("DECORRIDO: ").Append(elapsedText).Append('\n');
            sb.Append("FORMATO: ").Append(paperWidth).Append(" MM\n");
            sb.Append("LARGURA TOTAL: ").Append(totalWidth).Append(" MM\n");
            sb.Append("REFILE: 2 * ").Append(trim / 2).Append(" MM (").Append(trimPct).Append("%)\n");
            sb.Append("COMPOSIÇÃO: ").Append(Nz(p.PaperComposition)).Append('\n');
            sb.Append("M² PRODUZIDO: ").Append(Math.Round(m2Produced, 0)).Append('\n');
            sb.Append("METRO LINEAR PRODUZIDO: ").Append(Math.Round(linearMetersProduced, 0));

            return sb.ToString();
        }

        private void UpdateOrderDescriptionAndTotals()
        {
            double totalLinearMeters = 0;
            double totalM2 = 0;

            try
            {
                var list = productionListBindingSource.DataSource as List<ProductionListPlc> ?? new List<ProductionListPlc>();

                foreach (var p in list)
                {
                    p.Order1Description = BuildOrder1Description(p);
                    p.Order2Description = BuildOrder2Description(p);
                    p.OrderDetails = BuildOrderDetailDescription(p);

                    double lm = (Nz(p.Order1SheetLength) * (double)Nz(p.Order1NumberOfCutsProduced)) / 1000.0;
                    double m2 = lm * (Nz(p.PaperWidth) / 1000.0);

                    totalLinearMeters += lm;
                    totalM2 += m2;
                }

                if (txtLinearMeters != null) txtLinearMeters.Text = Math.Round(totalLinearMeters, 0).ToString();
                if (txtM2 != null) txtM2.Text = Math.Round(totalM2, 0).ToString();

                productionListBindingSource.DataSource = list;
                productionListBindingSource.ResetBindings(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearFilter_Click_1(object sender, EventArgs e)
        {

        }
    }
}
