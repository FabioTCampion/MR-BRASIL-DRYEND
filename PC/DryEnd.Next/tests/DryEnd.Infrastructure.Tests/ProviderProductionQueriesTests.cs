using DryEnd.Domain;
using DryEnd.Infrastructure.Database;

namespace DryEnd.Infrastructure.Tests;

public sealed class ProviderProductionQueriesTests
{
    [Theory]
    [InlineData(DatabaseProvider.SqlServer, "TOP 100", "OUTPUT INSERTED.Id")]
    [InlineData(DatabaseProvider.PostgreSql, "LIMIT 100", "RETURNING Id")]
    [InlineData(DatabaseProvider.Sqlite, "LIMIT 100", "RETURNING Id")]
    public void Queries_UseProviderSpecificPaginationAndIdentity(
        DatabaseProvider provider,
        string expectedLimit,
        string expectedIdentity)
    {
        var queries = Create(provider);

        Assert.Contains(expectedLimit, queries.Queue, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(expectedIdentity, queries.InsertOrder, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(DatabaseProvider.SqlServer, "VARCHAR(50)")]
    [InlineData(DatabaseProvider.PostgreSql, "TEXT")]
    [InlineData(DatabaseProvider.Sqlite, "TEXT")]
    public void WorkOrderSearch_UsesProviderSpecificTextCast(DatabaseProvider provider, string expectedType)
    {
        var query = Create(provider).BuildHistory(OrderSearchMode.WorkOrder, "123", null);

        Assert.Contains(expectedType, query.Sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Queries_UseLegacyProductionStateBoundaries()
    {
        var queries = Create(DatabaseProvider.SqlServer);

        Assert.Contains("ProductionState < 1", queries.Queue, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ProductionState < 1", queries.UpdateOrder, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ProductionState < 1", queries.DeleteOrder, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "ProductionState >= 4",
            queries.BuildHistory(OrderSearchMode.None, null, null).Sql,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(DatabaseProvider.SqlServer, "UPDLOCK")]
    [InlineData(DatabaseProvider.PostgreSql, "WHERE NOT EXISTS")]
    [InlineData(DatabaseProvider.Sqlite, "WHERE NOT EXISTS")]
    public void MachineSpeedInsert_DeduplicatesSlotInsideDatabase(
        DatabaseProvider provider,
        string expectedProtection)
    {
        var query = Create(provider).InsertMachineSpeed;

        Assert.Contains("INSERT INTO", query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Date_Time = @Slot", query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(expectedProtection, query, StringComparison.OrdinalIgnoreCase);
    }

    private static ProviderProductionQueries Create(DatabaseProvider provider) => new(new DatabaseOptions
    {
        Provider = provider,
        ConnectionString = "diagnostic-only",
        OrdersTable = provider == DatabaseProvider.Sqlite ? "ProductionList_Plc" : "dbo.ProductionList_Plc",
        MachineSpeedTable = provider == DatabaseProvider.Sqlite ? "MachineSpeedRecords" : "dbo.MachineSpeedRecords"
    });
}
