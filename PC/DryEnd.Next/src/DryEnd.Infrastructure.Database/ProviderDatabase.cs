using System.Data.Common;
using System.Text.RegularExpressions;
using DryEnd.Domain;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace DryEnd.Infrastructure.Database;

public sealed class ProviderDatabaseConnectionFactory(DatabaseOptions options) : IDatabaseConnectionFactory
{
    public DbConnection CreateConnection()
    {
        if (!options.IsConfigured)
            throw new InvalidOperationException("DryEnd database connection is not configured.");

        return options.Provider switch
        {
            DatabaseProvider.SqlServer => new SqlConnection(options.ConnectionString),
            DatabaseProvider.PostgreSql => new NpgsqlConnection(options.ConnectionString),
            DatabaseProvider.Sqlite => CreateOptionalSqliteConnection(options.ConnectionString!),
            _ => throw new NotSupportedException($"Database provider '{options.Provider}' is not supported.")
        };
    }

    private static DbConnection CreateOptionalSqliteConnection(string connectionString)
    {
        var connectionType = Type.GetType(
            "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite",
            throwOnError: false);
        if (connectionType is null)
            throw new NotSupportedException(
                "SQLite driver is not installed. Add a vulnerability-free Microsoft.Data.Sqlite provider package to the deployment.");
        return (DbConnection)Activator.CreateInstance(connectionType, connectionString)!;
    }
}

public sealed class ProviderProductionQueries : IProductionQueries
{
    private const int QueueStateUpperBoundExclusive = 1;
    private const int HistoryStateLowerBoundInclusive = 4;

    private const string Columns = """
        Id, ProductionSequence, ProductionState, MachineNotRunningTime, StartedAt, FinishedAt,
        PaperComposition, FluteType, PaperWidth, Paper1, Paper2, Paper3, Paper4, Paper5,
        ProductionListNumber, Order1Id, Order1Product, Order1Client, Order1SheetQuantity,
        Order1SheetType, Order1M1, Order1M2, Order1M3, Order1M4, Order1M5,
        Order1SheetLength, Order1NumberOfCuts, Order1NumberOfCutsProduced, Order1PileQuantity,
        LevelSelector, Order2Id, Order2Product, Order2Client, Order2SheetQuantity,
        Order2SheetType, Order2M1, Order2M2, Order2M3, Order2M4, Order2M5,
        Order2SheetLength, Order2NumberOfCuts, Order2NumberOfCutsProduced, Order2PileQuantity
        """;

    private readonly DatabaseProvider _provider;
    private readonly string _ordersTable;
    private readonly string _speedTable;

    public ProviderProductionQueries(DatabaseOptions options)
    {
        _provider = options.Provider;
        _ordersTable = ValidateIdentifier(options.OrdersTable);
        _speedTable = ValidateIdentifier(options.MachineSpeedTable);
    }

    public string Ping => "SELECT 1";
    public string Queue => LimitedSelect(
        $"{Columns} FROM {_ordersTable} WHERE ProductionState < {QueueStateUpperBoundExclusive} " +
        "ORDER BY CASE WHEN ProductionSequence > 0 THEN 0 ELSE 1 END, ProductionSequence ASC, Id ASC",
        100);
    public string MachineSpeed => $"""
        SELECT Date_Time AS DateTime, Machine_Speed AS MachineSpeed FROM {_speedTable}
        WHERE Date_Time >= @StartDate AND Date_Time < @EndDate ORDER BY Date_Time;
        """;
    public string InsertMachineSpeed => _provider == DatabaseProvider.SqlServer
        ? $"""
          INSERT INTO {_speedTable} (Date_Time, Machine_Speed)
          SELECT @Slot, @Speed
          WHERE NOT EXISTS (
              SELECT 1 FROM {_speedTable} WITH (UPDLOCK, HOLDLOCK) WHERE Date_Time = @Slot
          );
          """
        : $"""
          INSERT INTO {_speedTable} (Date_Time, Machine_Speed)
          SELECT @Slot, @Speed
          WHERE NOT EXISTS (
              SELECT 1 FROM {_speedTable} WHERE Date_Time = @Slot
          );
          """;
    public string InsertOrder => BuildInsert();
    public string UpdateOrder => $"""
        UPDATE {_ordersTable} SET
          ProductionSequence=@ProductionSequence, PaperComposition=@PaperComposition,
          FluteType=@FluteType, PaperWidth=@PaperWidth, Paper1=@Paper1, Paper2=@Paper2,
          Paper3=@Paper3, Paper4=@Paper4, Paper5=@Paper5, ProductionListNumber=@ProductionListNumber,
          Order1Id=@Order1Id, Order1Product=@Order1Product, Order1Client=@Order1Client,
          Order1SheetQuantity=@Order1SheetQuantity, Order1SheetType=@Order1SheetType,
          Order1M1=@Order1M1, Order1M2=@Order1M2, Order1M3=@Order1M3, Order1M4=@Order1M4,
          Order1M5=@Order1M5, Order1SheetLength=@Order1SheetLength,
          Order1NumberOfCuts=@Order1NumberOfCuts, Order1PileQuantity=@Order1PileQuantity,
          LevelSelector=@LevelSelector, Order2Id=@Order2Id, Order2Product=@Order2Product,
          Order2Client=@Order2Client, Order2SheetQuantity=@Order2SheetQuantity,
          Order2SheetType=@Order2SheetType, Order2M1=@Order2M1, Order2M2=@Order2M2,
          Order2M3=@Order2M3, Order2M4=@Order2M4, Order2M5=@Order2M5,
          Order2SheetLength=@Order2SheetLength, Order2NumberOfCuts=@Order2NumberOfCuts,
          Order2PileQuantity=@Order2PileQuantity
        WHERE Id=@Id AND ProductionState < {QueueStateUpperBoundExclusive};
        """;
    public string DeleteOrder =>
        $"DELETE FROM {_ordersTable} WHERE Id=@Id AND ProductionState < {QueueStateUpperBoundExclusive};";

    public ProductionQuery BuildHistory(OrderSearchMode mode, string? search, DateTime? date)
    {
        var where = $"ProductionState >= {HistoryStateLowerBoundInclusive}";
        object? parameters = null;
        var pattern = $"%{search?.Trim() ?? string.Empty}%";
        switch (mode)
        {
            case OrderSearchMode.Client:
                where += " AND (Order1Client LIKE @Pattern OR Order2Client LIKE @Pattern)";
                parameters = new { Pattern = pattern };
                break;
            case OrderSearchMode.Composition:
                where += " AND PaperComposition LIKE @Pattern";
                parameters = new { Pattern = pattern };
                break;
            case OrderSearchMode.ProductionList:
                where += " AND ProductionListNumber LIKE @Pattern";
                parameters = new { Pattern = pattern };
                break;
            case OrderSearchMode.WorkOrder:
                var textType = _provider == DatabaseProvider.SqlServer ? "VARCHAR(50)" : "TEXT";
                where += $" AND (CAST(Order1Id AS {textType}) LIKE @Pattern OR CAST(Order2Id AS {textType}) LIKE @Pattern)";
                parameters = new { Pattern = pattern };
                break;
            case OrderSearchMode.Product:
                where += " AND (Order1Product LIKE @Pattern OR Order2Product LIKE @Pattern)";
                parameters = new { Pattern = pattern };
                break;
            case OrderSearchMode.None when date.HasValue:
                where += " AND StartedAt >= @StartDate AND StartedAt < @EndDate";
                parameters = new { StartDate = date.Value.Date, EndDate = date.Value.Date.AddDays(1) };
                break;
        }

        var body = $"{Columns} FROM {_ordersTable} WHERE {where} " +
                   "ORDER BY COALESCE(FinishedAt, StartedAt) DESC, Id DESC";
        return new ProductionQuery(LimitedSelect(body, 100), parameters);
    }

    private string BuildInsert()
    {
        const string fields = """
            ProductionSequence, ProductionState, MachineNotRunningTime, StartedAt, FinishedAt,
            PaperComposition, FluteType, PaperWidth, Paper1, Paper2, Paper3, Paper4, Paper5,
            ProductionListNumber, Order1Id, Order1Product, Order1Client, Order1SheetQuantity,
            Order1SheetType, Order1M1, Order1M2, Order1M3, Order1M4, Order1M5,
            Order1SheetLength, Order1NumberOfCuts, Order1NumberOfCutsProduced, Order1PileQuantity,
            LevelSelector, Order2Id, Order2Product, Order2Client, Order2SheetQuantity,
            Order2SheetType, Order2M1, Order2M2, Order2M3, Order2M4, Order2M5,
            Order2SheetLength, Order2NumberOfCuts, Order2NumberOfCutsProduced, Order2PileQuantity
            """;
        const string values = """
            @ProductionSequence, 0, 0, NULL, NULL,
            @PaperComposition, @FluteType, @PaperWidth, @Paper1, @Paper2, @Paper3, @Paper4, @Paper5,
            @ProductionListNumber, @Order1Id, @Order1Product, @Order1Client, @Order1SheetQuantity,
            @Order1SheetType, @Order1M1, @Order1M2, @Order1M3, @Order1M4, @Order1M5,
            @Order1SheetLength, @Order1NumberOfCuts, 0, @Order1PileQuantity,
            @LevelSelector, @Order2Id, @Order2Product, @Order2Client, @Order2SheetQuantity,
            @Order2SheetType, @Order2M1, @Order2M2, @Order2M3, @Order2M4, @Order2M5,
            @Order2SheetLength, @Order2NumberOfCuts, 0, @Order2PileQuantity
            """;

        return _provider == DatabaseProvider.SqlServer
            ? $"INSERT INTO {_ordersTable} ({fields}) OUTPUT INSERTED.Id VALUES ({values});"
            : $"INSERT INTO {_ordersTable} ({fields}) VALUES ({values}) RETURNING Id;";
    }

    private string LimitedSelect(string body, int limit) => _provider == DatabaseProvider.SqlServer
        ? $"SELECT TOP {limit} {body};"
        : $"SELECT {body} LIMIT {limit};";

    private static string ValidateIdentifier(string value)
    {
        if (!Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)?$"))
            throw new InvalidOperationException($"Invalid database object name '{value}'.");
        return value;
    }
}
