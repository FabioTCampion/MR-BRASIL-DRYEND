using DryEnd.Domain;

namespace DryEnd.Application;

public interface IPlcNextOrderWriter
{
    Task<OrderSnapshot> WriteNextOrderAsync(
        NextOrderUpdate update,
        CancellationToken cancellationToken);
}
