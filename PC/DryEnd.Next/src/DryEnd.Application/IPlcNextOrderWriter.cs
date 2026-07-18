using DryEnd.Domain;

namespace DryEnd.Application;

public interface IPlcNextOrderWriter
{
    Task<OrderSnapshot> WriteNextOrderAsync(
        NextOrderUpdate update,
        CancellationToken cancellationToken);
}

public interface IPlcChangeOrderAcknowledger
{
    Task AcknowledgeChangeOrderAsync(
        DateTime nextOrderStartedAt,
        CancellationToken cancellationToken);
}

public interface IPlcOrderCommandWriter
{
    Task<bool> RequestChangeOrderAsync(CancellationToken cancellationToken);
    Task<bool> RequestAutomaticOrderChangeAsync(CancellationToken cancellationToken);
}
