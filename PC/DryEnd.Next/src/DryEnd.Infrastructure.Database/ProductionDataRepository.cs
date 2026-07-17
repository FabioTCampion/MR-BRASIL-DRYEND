using Dapper;
using DryEnd.Application;
using DryEnd.Domain;
using System.Data;

namespace DryEnd.Infrastructure.Database;

public sealed class ProductionDataRepository(
    DatabaseOptions options,
    IDatabaseConnectionFactory connectionFactory,
    IProductionQueries queries) : IProductionDataRepository
{
    public async Task<DatabaseStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        if (!options.IsConfigured)
            return new DatabaseStatus(false, false, "Database connection is not configured.");
        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition(queries.Ping, cancellationToken: cancellationToken));
            return new DatabaseStatus(true, true, null);
        }
        catch (Exception exception)
        {
            return new DatabaseStatus(true, false, exception.Message);
        }
    }

    public Task<IReadOnlyList<ProductionOrderRecord>> GetQueueAsync(CancellationToken cancellationToken) =>
        QueryOrdersAsync(new ProductionQuery(queries.Queue), cancellationToken);

    public Task<IReadOnlyList<ProductionOrderRecord>> GetHistoryAsync(
        OrderSearchMode mode,
        string? search,
        DateTime? date,
        CancellationToken cancellationToken) =>
        QueryOrdersAsync(queries.BuildHistory(mode, search, date), cancellationToken);

    public async Task<IReadOnlyList<MachineSpeedRecord>> GetMachineSpeedAsync(DateTime date, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<MachineSpeedRecord>(new CommandDefinition(
            queries.MachineSpeed,
            new { StartDate = date.Date, EndDate = date.Date.AddDays(1) },
            cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<bool> TryAddMachineSpeedAsync(
        MachineSpeedSample sample,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var insertedRows = await connection.ExecuteAsync(new CommandDefinition(
            queries.InsertMachineSpeed,
            new { sample.Slot, sample.Speed },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return insertedRows == 1;
    }

    public async Task<int> CreateAsync(ProductionOrderRecord order, CancellationToken cancellationToken)
    {
        NormalizeAndValidate(order, creating: true);
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            queries.InsertOrder, order, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(ProductionOrderRecord order, CancellationToken cancellationToken)
    {
        NormalizeAndValidate(order, creating: false);
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            queries.UpdateOrder, order, cancellationToken: cancellationToken)) == 1;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            queries.DeleteOrder, new { Id = id }, cancellationToken: cancellationToken)) == 1;
    }

    private async Task<IReadOnlyList<ProductionOrderRecord>> QueryOrdersAsync(
        ProductionQuery query,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<ProductionOrderRecord>(new CommandDefinition(
            query.Sql, query.Parameters, cancellationToken: cancellationToken));
        return result.AsList();
    }

    private static void NormalizeAndValidate(ProductionOrderRecord order, bool creating)
    {
        if (!creating && order.Id <= 0)
            throw new ArgumentException("A valid order ID is required.");
        if (string.IsNullOrWhiteSpace(order.PaperComposition))
            throw new ArgumentException("Paper composition is required.");
        if (string.IsNullOrWhiteSpace(order.FluteType))
            throw new ArgumentException("Flute type is required.");
        if (order.PaperWidth is null or <= 0)
            throw new ArgumentException("Paper width must be greater than zero.");
        if (string.IsNullOrWhiteSpace(order.ProductionListNumber))
            throw new ArgumentException("Production list number is required.");
        if (order.LevelSelector is < 1 or > 3)
            throw new ArgumentException("Level selector must be 1, 2 or 3.");

        if (order.LevelSelector is 1 or 3)
            ValidateOrder1(order);
        else
            ClearOrder1(order);

        if (order.LevelSelector is 2 or 3)
            ValidateOrder2(order);
        else
            ClearOrder2(order);
    }

    private static void ValidateOrder1(ProductionOrderRecord order)
    {
        if (order.Order1SheetType is null or < 0 or > 2)
            throw new ArgumentException("Order 1 sheet type must be 0, 1 or 2.");
        if (order.Order1SheetQuantity is null or <= 0)
            throw new ArgumentException("Order 1 sheet quantity must be greater than zero.");
        if (order.Order1SheetLength is null or < 450 or > 2800)
            throw new ArgumentException("Order 1 sheet length must be between 450 and 2800 mm.");
        if (order.Order1NumberOfCuts is null or <= 0)
            throw new ArgumentException("Order 1 number of cuts must be greater than zero.");
        if (order.Order1PileQuantity is null or <= 0)
            throw new ArgumentException("Order 1 pile quantity must be greater than zero.");
    }

    private static void ValidateOrder2(ProductionOrderRecord order)
    {
        if (order.Order2SheetType is null or < 0 or > 2)
            throw new ArgumentException("Order 2 sheet type must be 0, 1 or 2.");
        if (order.Order2SheetQuantity is null or <= 0)
            throw new ArgumentException("Order 2 sheet quantity must be greater than zero.");
        if (order.Order2SheetLength is null or < 450 or > 2800)
            throw new ArgumentException("Order 2 sheet length must be between 450 and 2800 mm.");
        if (order.Order2NumberOfCuts is null or <= 0)
            throw new ArgumentException("Order 2 number of cuts must be greater than zero.");
        if (order.Order2PileQuantity is null or <= 0)
            throw new ArgumentException("Order 2 pile quantity must be greater than zero.");
    }

    private static void ClearOrder1(ProductionOrderRecord order)
    {
        order.Order1Id = null; order.Order1Product = null; order.Order1Client = null;
        order.Order1SheetQuantity = null; order.Order1SheetType = null;
        order.Order1M1 = null; order.Order1M2 = null; order.Order1M3 = null;
        order.Order1M4 = null; order.Order1M5 = null; order.Order1SheetLength = null;
        order.Order1NumberOfCuts = null; order.Order1PileQuantity = null;
    }

    private static void ClearOrder2(ProductionOrderRecord order)
    {
        order.Order2Id = null; order.Order2Product = null; order.Order2Client = null;
        order.Order2SheetQuantity = null; order.Order2SheetType = null;
        order.Order2M1 = null; order.Order2M2 = null; order.Order2M3 = null;
        order.Order2M4 = null; order.Order2M5 = null; order.Order2SheetLength = null;
        order.Order2NumberOfCuts = null; order.Order2PileQuantity = null;
    }
}
