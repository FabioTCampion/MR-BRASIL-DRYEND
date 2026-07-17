namespace DryEnd.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; init; } = DatabaseProvider.SqlServer;
    public string? ConnectionString { get; init; }
    public string OrdersTable { get; init; } = "dbo.ProductionList_Plc";
    public string MachineSpeedTable { get; init; } = "dbo.MachineSpeedRecords";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}

public enum DatabaseProvider
{
    SqlServer,
    PostgreSql,
    Sqlite
}
