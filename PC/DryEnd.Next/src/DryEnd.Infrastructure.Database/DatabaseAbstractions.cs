using System.Data.Common;
using DryEnd.Domain;

namespace DryEnd.Infrastructure.Database;

public interface IDatabaseConnectionFactory
{
    DbConnection CreateConnection();
}

public interface IProductionQueries
{
    string Ping { get; }
    string Queue { get; }
    string MachineSpeed { get; }
    string AllMachineSpeed { get; }
    string InsertMachineSpeed { get; }
    string InsertOrder { get; }
    string UpdateOrder { get; }
    string DeleteOrder { get; }
    string UpdateHistory { get; }
    string DeleteHistory { get; }
    string ClearHistory { get; }
    string RecoverHistory { get; }
    string ClearPendingOrders { get; }
    string PendingOrderIdsForUpdate { get; }
    string UpdateProductionSequence { get; }
    string SwapPendingOrderLevels { get; }
    string CountImportDuplicate { get; }
    string GetMaxPendingSequence { get; }
    string CountInProduction { get; }
    string CleanupAllInProduction { get; }
    string GetCurrentInProductionId { get; }
    string CountOrderById { get; }
    string CountRecoveredHistory { get; }
    string FinishCurrentOrder { get; }
    string StartNextOrder { get; }
    string ForceSingleInProduction { get; }
    ProductionQuery BuildHistory(OrderSearchMode mode, string? search, DateTime? date);
}

public interface IDatabaseSchemaMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken);
}

public sealed record ProductionQuery(string Sql, object? Parameters = null);
