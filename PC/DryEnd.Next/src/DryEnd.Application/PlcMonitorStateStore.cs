using DryEnd.Domain;

namespace DryEnd.Application;

public sealed class PlcMonitorStateStore : IPlcMonitorStateStore
{
    private PlcMonitorSnapshot _current = PlcMonitorSnapshot.Initial;

    public PlcMonitorSnapshot Current => Volatile.Read(ref _current);

    public void Update(PlcMonitorSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Interlocked.Exchange(ref _current, snapshot);
    }
}
