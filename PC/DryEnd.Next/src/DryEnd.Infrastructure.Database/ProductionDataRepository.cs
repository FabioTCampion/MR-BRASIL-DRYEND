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
    private string StopTable => TableBesideOrders("ProductionStops");
    private string StopReasonTable => TableBesideOrders("ProductionStopReasons");
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

    public async Task<IReadOnlyList<MachineSpeedRecord>> GetAllMachineSpeedAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<MachineSpeedRecord>(new CommandDefinition(
            queries.AllMachineSpeed,
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

    public async Task<bool> UpdateHistoryAsync(ProductionOrderRecord order, CancellationToken cancellationToken)
    {
        if (order.Id <= 0)
            throw new ArgumentException("A valid history ID is required.");
        order.UpdatedAt = DateTime.UtcNow;
        order.LastModifiedBy ??= "SYSTEM-WEB";
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            queries.UpdateHistory, order, cancellationToken: cancellationToken)) == 1;
    }

    public async Task<bool> DeleteHistoryAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            queries.DeleteHistory, new { Id = id }, cancellationToken: cancellationToken)) == 1;
    }

    public async Task<int> ClearHistoryAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            queries.ClearHistory, cancellationToken: cancellationToken));
    }

    public async Task<bool> RecoverHistoryAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
            throw new ArgumentException("A valid history ID is required.");
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            queries.RecoverHistory,
            new { Id = id },
            cancellationToken: cancellationToken)) == 1;
    }

    public async Task<int> ClearPendingAsync(
        int currentTableId,
        int nextTableId,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            queries.ClearPendingOrders,
            new { CurrentTableId = currentTableId, NextTableId = nextTableId },
            cancellationToken: cancellationToken));
    }

    public async Task<int?> ReorderPendingAsync(
        IReadOnlyList<int> orderedIds,
        string modifiedBy,
        CancellationToken cancellationToken)
    {
        if (orderedIds.Count == 0)
            throw new ArgumentException("A fila deve conter pelo menos um pedido.");
        if (orderedIds.Any(id => id <= 0) || orderedIds.Distinct().Count() != orderedIds.Count)
            throw new ArgumentException("A sequência contém IDs inválidos ou duplicados.");

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var currentIds = (await connection.QueryAsync<int>(new CommandDefinition(
            queries.PendingOrderIdsForUpdate,
            transaction: transaction,
            cancellationToken: cancellationToken))).AsList();
        if (currentIds.Count != orderedIds.Count || currentIds.Except(orderedIds).Any())
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var updatedAt = DateTime.UtcNow;
        var updated = 0;
        for (var index = 0; index < orderedIds.Count; index++)
        {
            updated += await connection.ExecuteAsync(new CommandDefinition(
                queries.UpdateProductionSequence,
                new
                {
                    Id = orderedIds[index],
                    ProductionSequence = index + 1,
                    UpdatedAt = updatedAt,
                    LastModifiedBy = modifiedBy
                },
                transaction,
                cancellationToken: cancellationToken));
        }
        if (updated != orderedIds.Count)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }
        await transaction.CommitAsync(cancellationToken);
        return updated;
    }

    public async Task<bool> SwapPendingOrderLevelsAsync(
        int id,
        string modifiedBy,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            throw new ArgumentException("A valid order ID is required.");
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            queries.SwapPendingOrderLevels,
            new { Id = id, UpdatedAt = DateTime.UtcNow, LastModifiedBy = modifiedBy },
            cancellationToken: cancellationToken)) == 1;
    }

    public async Task<ProductionImportResult> ImportPendingAsync(
        IReadOnlyList<ProductionOrderRecord> orders,
        CancellationToken cancellationToken)
    {
        if (orders.Count == 0)
            throw new ArgumentException("At least one order is required for import.");

        var importedAt = DateTime.UtcNow;
        foreach (var order in orders)
        {
            order.ImportedAt = importedAt;
            order.CreatedBy = "SYSTEM-TRIMBOX";
            order.LastModifiedBy = "SYSTEM-TRIMBOX";
            NormalizeAndValidate(order, creating: true);
        }

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var insertedIds = new List<int>();
        var duplicateCount = 0;
        var nextSequence = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            queries.GetMaxPendingSequence,
            transaction: transaction,
            cancellationToken: cancellationToken));
        foreach (var order in orders)
        {
            var duplicate = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                queries.CountImportDuplicate,
                order,
                transaction,
                cancellationToken: cancellationToken));
            if (duplicate > 0)
            {
                duplicateCount++;
                continue;
            }

            order.ProductionSequence = ++nextSequence;
            insertedIds.Add(await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                queries.InsertOrder,
                order,
                transaction,
                cancellationToken: cancellationToken)));
        }

        await transaction.CommitAsync(cancellationToken);
        return new ProductionImportResult(insertedIds.Count, duplicateCount, insertedIds);
    }

    public async Task CompleteChangeOrderAsync(
        ChangeOrderCompletion completion,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var inProductionCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            queries.CountInProduction,
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (inProductionCount > 1)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                queries.CleanupAllInProduction,
                new { completion.FinishedAt },
                transaction,
                cancellationToken: cancellationToken));
        }

        if (completion.CurrentTableId > 0)
        {
            var currentExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                queries.CountOrderById,
                completion,
                transaction,
                cancellationToken: cancellationToken)) > 0;

            if (currentExists)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    queries.FinishCurrentOrder,
                    completion,
                    transaction,
                    cancellationToken: cancellationToken));
            }
            else
            {
                var recoveredAlreadyExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    queries.CountRecoveredHistory,
                    completion,
                    transaction,
                    cancellationToken: cancellationToken)) > 0;
                if (!recoveredAlreadyExists)
                {
                    var recoveredOrder = CreateRecoveredHistory(completion);
                    await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                        queries.InsertOrder,
                        recoveredOrder,
                        transaction,
                        cancellationToken: cancellationToken));
                }
            }
        }

        if (completion.NextTableId > 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                queries.StartNextOrder,
                completion,
                transaction,
                cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(
                queries.ForceSingleInProduction,
                completion,
                transaction,
                cancellationToken: cancellationToken));
        }
        else
        {
            await connection.ExecuteAsync(new CommandDefinition(
                queries.CleanupAllInProduction,
                new { completion.FinishedAt },
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionStopReason>> GetStopReasonsAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ProductionStopReason>(new CommandDefinition(
            $"SELECT Code, Category, Description, IsActive FROM {StopReasonTable} WHERE IsActive={TrueValue} ORDER BY Code;",
            cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ProductionStop>> GetStopsAsync(DateTime date, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        var limit = options.Provider == DatabaseProvider.SqlServer ? "TOP 500 " : string.Empty;
        var suffix = options.Provider == DatabaseProvider.SqlServer ? string.Empty : " LIMIT 500";
        var rows = await connection.QueryAsync<ProductionStop>(new CommandDefinition($"""
            SELECT {limit}s.Id, s.StartedAt, s.FinishedAt, s.ReasonCode,
              r.Description AS ReasonDescription, r.Category, s.Observation,
              s.JustifiedBy, s.JustifiedAt, s.CurrentTableId, s.ProductionListNumber
            FROM {StopTable} s LEFT JOIN {StopReasonTable} r ON r.Code=s.ReasonCode
            WHERE s.StartedAt < @EndDate AND (s.FinishedAt IS NULL OR s.FinishedAt >= @StartDate)
            ORDER BY s.StartedAt DESC{suffix};
            """, new { StartDate=date.Date, EndDate=date.Date.AddDays(1) }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<int> GetPendingStopCountAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            $"SELECT COUNT(1) FROM {StopTable} WHERE FinishedAt IS NOT NULL AND ReasonCode IS NULL;",
            cancellationToken: cancellationToken));
    }

    public async Task UpdateStopDetectionAsync(bool stopped, DateTime observedAt, int currentTableId, int productionListNumber, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        var sql = stopped
            ? $"INSERT INTO {StopTable}(StartedAt,CurrentTableId,ProductionListNumber) SELECT @ObservedAt,@CurrentTableId,@ProductionListNumber WHERE NOT EXISTS (SELECT 1 FROM {StopTable} WHERE FinishedAt IS NULL);"
            : $"UPDATE {StopTable} SET FinishedAt=@ObservedAt WHERE FinishedAt IS NULL;";
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { ObservedAt=observedAt, CurrentTableId=currentTableId, ProductionListNumber=productionListNumber },
            cancellationToken: cancellationToken));
    }

    public async Task ReplaceUnjustifiedStopsAsync(
        IReadOnlyList<DetectedProductionStop> stops,
        int currentTableId,
        int productionListNumber,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            $"DELETE FROM {StopTable} WHERE ReasonCode IS NULL;",
            transaction: transaction,
            cancellationToken: cancellationToken));

        foreach (var stop in stops)
        {
            var overlapsJustified = await connection.ExecuteScalarAsync<int>(new CommandDefinition($"""
                SELECT COUNT(*) FROM {StopTable}
                WHERE ReasonCode IS NOT NULL
                  AND StartedAt < @RangeEnd
                  AND COALESCE(FinishedAt, @RangeEnd) > @StartedAt;
                """,
                new { stop.StartedAt, RangeEnd = stop.FinishedAt ?? DateTime.MaxValue },
                transaction,
                cancellationToken: cancellationToken));
            if (overlapsJustified > 0) continue;

            await connection.ExecuteAsync(new CommandDefinition($"""
                INSERT INTO {StopTable}
                    (StartedAt, FinishedAt, CurrentTableId, ProductionListNumber)
                VALUES
                    (@StartedAt, @FinishedAt, @CurrentTableId, @ProductionListNumber);
                """,
                new
                {
                    stop.StartedAt,
                    stop.FinishedAt,
                    CurrentTableId = stop.FinishedAt is null ? currentTableId : (int?)null,
                    ProductionListNumber = stop.FinishedAt is null ? productionListNumber : (int?)null
                },
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> JustifyStopAsync(long id, ProductionStopJustification justification, DateTime justifiedAt, CancellationToken cancellationToken)
    {
        if (justification.ReasonCode <= 0) throw new ArgumentException("A stop reason is required.");
        if (string.IsNullOrWhiteSpace(justification.JustifiedBy)) throw new ArgumentException("The operator name is required.");
        if (justification.Observation?.Length > 1000) throw new ArgumentException("Observation cannot exceed 1000 characters.");
        await using var connection = connectionFactory.CreateConnection();
        var changed = await connection.ExecuteAsync(new CommandDefinition($"""
            UPDATE {StopTable} SET ReasonCode=@ReasonCode, Observation=@Observation,
              JustifiedBy=@JustifiedBy, JustifiedAt=@JustifiedAt
            WHERE Id=@Id AND FinishedAt IS NOT NULL
              AND EXISTS (SELECT 1 FROM {StopReasonTable} WHERE Code=@ReasonCode AND IsActive={TrueValue});
            """, new { Id=id, justification.ReasonCode, Observation=justification.Observation?.Trim(), JustifiedBy=justification.JustifiedBy.Trim(), JustifiedAt=justifiedAt }, cancellationToken: cancellationToken));
        return changed == 1;
    }

    private string TableBesideOrders(string name)
    {
        var index = options.OrdersTable.LastIndexOf('.');
        return index >= 0 ? $"{options.OrdersTable[..(index + 1)]}{name}" : name;
    }

    private string TrueValue => options.Provider == DatabaseProvider.PostgreSql ? "TRUE" : "1";

    private async Task<IReadOnlyList<ProductionOrderRecord>> QueryOrdersAsync(
        ProductionQuery query,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<ProductionOrderRecord>(new CommandDefinition(
            query.Sql, query.Parameters, cancellationToken: cancellationToken));
        var rows = result.AsList();
        foreach (var row in rows)
            NormalizeLegacyLevel2(row);
        return rows;
    }

    private static void NormalizeLegacyLevel2(ProductionOrderRecord order)
    {
        if (order.LevelSelector != 2 || order.Order1Id.HasValue || !order.Order2Id.HasValue)
            return;
        order.Order1Id = order.Order2Id; order.Order1Product = order.Order2Product; order.Order1Client = order.Order2Client;
        order.Order1SheetQuantity = order.Order2SheetQuantity; order.Order1SheetType = order.Order2SheetType;
        order.Order1M1 = order.Order2M1; order.Order1M2 = order.Order2M2; order.Order1M3 = order.Order2M3;
        order.Order1M4 = order.Order2M4; order.Order1M5 = order.Order2M5; order.Order1SheetLength = order.Order2SheetLength;
        order.Order1NumberOfCuts = order.Order2NumberOfCuts; order.Order1NumberOfCutsProduced = order.Order2NumberOfCutsProduced;
        order.Order1PileQuantity = order.Order2PileQuantity;
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

        var now = DateTime.UtcNow;
        if (creating)
        {
            order.ProductionState = 0;
            order.MachineNotRunningTime = 0;
            order.StartedAt = null;
            order.FinishedAt = null;
            order.HistorySavedAt = null;
            order.HistoryCreatedFromPlc = false;
            order.Order1NumberOfCutsProduced = 0;
            order.Order2NumberOfCutsProduced = 0;
            order.CreatedAt ??= now;
            order.CreatedBy ??= "SYSTEM-WEB";
        }
        order.UpdatedAt = now;
        order.LastModifiedBy ??= order.CreatedBy ?? "SYSTEM-WEB";

        ValidateOrder1(order);

        if (order.LevelSelector == 3)
            ValidateOrder2(order);
        else
            ClearOrder2(order);
    }

    private static ProductionOrderRecord CreateRecoveredHistory(ChangeOrderCompletion completion)
    {
        var snapshot = completion.CurrentOrderSnapshot;
        var order1Enabled = snapshot.LevelSelector is 1 or 2 or 3;
        var order2Enabled = snapshot.LevelSelector == 3;

        return new ProductionOrderRecord
        {
            PlcSourceTableId = completion.CurrentTableId,
            CreatedAt = completion.CurrentStartedAt ?? completion.FinishedAt,
            UpdatedAt = completion.FinishedAt,
            HistorySavedAt = completion.FinishedAt,
            HistoryCreatedFromPlc = true,
            CreatedBy = "SYSTEM-PLC-HANDSHAKE",
            LastModifiedBy = "SYSTEM-PLC-HANDSHAKE",
            ProductionState = 4,
            MachineNotRunningTime = 0,
            StartedAt = completion.CurrentStartedAt,
            FinishedAt = completion.FinishedAt,
            PaperComposition = snapshot.PaperComposition,
            FluteType = snapshot.FluteType,
            PaperWidth = snapshot.PaperWidth,
            Paper1 = ItemOrNull(snapshot.PaperLayers, 0),
            Paper2 = ItemOrNull(snapshot.PaperLayers, 1),
            Paper3 = ItemOrNull(snapshot.PaperLayers, 2),
            Paper4 = ItemOrNull(snapshot.PaperLayers, 3),
            Paper5 = ItemOrNull(snapshot.PaperLayers, 4),
            ProductionListNumber = snapshot.ProductionListNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            LevelSelector = snapshot.LevelSelector,
            Order1Id = order1Enabled ? snapshot.Order1.Id : null,
            Order1Product = order1Enabled ? snapshot.Order1.Product : null,
            Order1Client = order1Enabled ? snapshot.Order1.Client : null,
            Order1SheetQuantity = order1Enabled ? snapshot.Order1.SheetQuantity : null,
            Order1SheetType = order1Enabled ? snapshot.Order1.SheetType : null,
            Order1M1 = order1Enabled ? ItemOrZero(snapshot.Order1.SheetMeasures, 0) : null,
            Order1M2 = order1Enabled ? ItemOrZero(snapshot.Order1.SheetMeasures, 1) : null,
            Order1M3 = order1Enabled ? ItemOrZero(snapshot.Order1.SheetMeasures, 2) : null,
            Order1M4 = order1Enabled ? ItemOrZero(snapshot.Order1.SheetMeasures, 3) : null,
            Order1M5 = order1Enabled ? ItemOrZero(snapshot.Order1.SheetMeasures, 4) : null,
            Order1SheetLength = order1Enabled ? snapshot.Order1.SheetLength : null,
            Order1NumberOfCuts = order1Enabled ? snapshot.Order1.NumberOfCuts : null,
            Order1NumberOfCutsProduced = order1Enabled ? completion.Order1NumberOfCutsProduced : 0,
            Order1PileQuantity = order1Enabled ? snapshot.Order1.PileQuantity : null,
            Order2Id = order2Enabled ? snapshot.Order2.Id : null,
            Order2Product = order2Enabled ? snapshot.Order2.Product : null,
            Order2Client = order2Enabled ? snapshot.Order2.Client : null,
            Order2SheetQuantity = order2Enabled ? snapshot.Order2.SheetQuantity : null,
            Order2SheetType = order2Enabled ? snapshot.Order2.SheetType : null,
            Order2M1 = order2Enabled ? ItemOrZero(snapshot.Order2.SheetMeasures, 0) : null,
            Order2M2 = order2Enabled ? ItemOrZero(snapshot.Order2.SheetMeasures, 1) : null,
            Order2M3 = order2Enabled ? ItemOrZero(snapshot.Order2.SheetMeasures, 2) : null,
            Order2M4 = order2Enabled ? ItemOrZero(snapshot.Order2.SheetMeasures, 3) : null,
            Order2M5 = order2Enabled ? ItemOrZero(snapshot.Order2.SheetMeasures, 4) : null,
            Order2SheetLength = order2Enabled ? snapshot.Order2.SheetLength : null,
            Order2NumberOfCuts = order2Enabled ? snapshot.Order2.NumberOfCuts : null,
            Order2NumberOfCutsProduced = order2Enabled ? completion.Order2NumberOfCutsProduced : 0,
            Order2PileQuantity = order2Enabled ? snapshot.Order2.PileQuantity : null
        };
    }

    private static string? ItemOrNull(IReadOnlyList<string> values, int index) =>
        index < values.Count && !string.IsNullOrWhiteSpace(values[index]) ? values[index] : null;

    private static int ItemOrZero(IReadOnlyList<short> values, int index) =>
        index < values.Count ? values[index] : 0;

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
