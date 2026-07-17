using DryEnd.Application;
using DryEnd.Domain;

namespace DryEnd.Application.Tests;

public sealed class PlcMonitorStateStoreTests
{
    [Fact]
    public void Update_ReplacesCurrentSnapshot()
    {
        var store = new PlcMonitorStateStore();
        var expected = PlcMonitorSnapshot.Initial with { State = PlcConnectionState.Connecting };

        store.Update(expected);

        Assert.Same(expected, store.Current);
    }
}
