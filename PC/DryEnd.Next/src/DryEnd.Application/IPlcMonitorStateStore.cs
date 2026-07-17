using DryEnd.Domain;

namespace DryEnd.Application;

public interface IPlcMonitorStateStore
{
    PlcMonitorSnapshot Current { get; }
    void Update(PlcMonitorSnapshot snapshot);
}
