using DryEnd.Domain;

namespace DryEnd.Application;

public interface IPlcConnection : IAsyncDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken);
    Task<PlcDataSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
}
