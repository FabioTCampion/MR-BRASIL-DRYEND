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
    Task<bool> TryAddMachineSpeedAsync(MachineSpeedSample sample, CancellationToken cancellationToken);
    Task<int> CreateAsync(ProductionOrderRecord order, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(ProductionOrderRecord order, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
