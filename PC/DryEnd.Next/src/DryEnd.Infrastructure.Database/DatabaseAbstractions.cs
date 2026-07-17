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
    string InsertOrder { get; }
    string UpdateOrder { get; }
    string DeleteOrder { get; }
    ProductionQuery BuildHistory(OrderSearchMode mode, string? search, DateTime? date);
}

public sealed record ProductionQuery(string Sql, object? Parameters = null);
