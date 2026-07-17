using DryEnd.Domain;

namespace DryEnd.Application;

public interface IPlcOrderEditor
{
    Task<OrderSnapshot> UpdateCurrentOrderAsync(
        CurrentOrderUpdate update,
        CancellationToken cancellationToken);
}
