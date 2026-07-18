using DryEnd.Domain;

namespace DryEnd.Application;

public interface IProductionDataRepository
{
    Task<DatabaseStatus> GetStatusAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionOrderRecord>> GetQueueAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionOrderRecord>> GetHistoryAsync(
        OrderSearchMode mode,
        string? search,
        DateTime? date,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<MachineSpeedRecord>> GetMachineSpeedAsync(DateTime date, CancellationToken cancellationToken);
    Task<IReadOnlyList<MachineSpeedRecord>> GetAllMachineSpeedAsync(CancellationToken cancellationToken);
    Task<bool> TryAddMachineSpeedAsync(MachineSpeedSample sample, CancellationToken cancellationToken);
    Task<int> CreateAsync(ProductionOrderRecord order, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(ProductionOrderRecord order, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<bool> UpdateHistoryAsync(ProductionOrderRecord order, CancellationToken cancellationToken);
    Task<bool> DeleteHistoryAsync(int id, CancellationToken cancellationToken);
    Task<int> ClearHistoryAsync(CancellationToken cancellationToken);
    Task<bool> RecoverHistoryAsync(int id, CancellationToken cancellationToken);
    Task<int> ClearPendingAsync(
        int currentTableId,
        int nextTableId,
        CancellationToken cancellationToken);
    Task<int?> ReorderPendingAsync(
        IReadOnlyList<int> orderedIds,
        string modifiedBy,
        CancellationToken cancellationToken);
    Task<bool> SwapPendingOrderLevelsAsync(
        int id,
        string modifiedBy,
        CancellationToken cancellationToken);
    Task<ProductionImportResult> ImportPendingAsync(
        IReadOnlyList<ProductionOrderRecord> orders,
        CancellationToken cancellationToken);
    Task CompleteChangeOrderAsync(
        ChangeOrderCompletion completion,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionStopReason>> GetStopReasonsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionStop>> GetStopsAsync(DateTime date, CancellationToken cancellationToken);
    Task<int> GetPendingStopCountAsync(CancellationToken cancellationToken);
    Task UpdateStopDetectionAsync(
        bool stopped,
        DateTime observedAt,
        int currentTableId,
        int productionListNumber,
        CancellationToken cancellationToken);
    Task ReplaceUnjustifiedStopsAsync(
        IReadOnlyList<DetectedProductionStop> stops,
        int currentTableId,
        int productionListNumber,
        CancellationToken cancellationToken);
    Task<bool> JustifyStopAsync(
        long id,
        ProductionStopJustification justification,
        DateTime justifiedAt,
        CancellationToken cancellationToken);
}

public sealed record ChangeOrderCompletion(
    int CurrentTableId,
    int NextTableId,
    DateTime? CurrentStartedAt,
    DateTime FinishedAt,
    DateTime NextStartedAt,
    int Order1NumberOfCutsProduced,
    int Order2NumberOfCutsProduced,
    OrderSnapshot CurrentOrderSnapshot);
