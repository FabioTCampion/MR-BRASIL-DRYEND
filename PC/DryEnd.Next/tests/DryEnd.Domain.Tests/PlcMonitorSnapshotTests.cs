using DryEnd.Domain;

namespace DryEnd.Domain.Tests;

public sealed class PlcMonitorSnapshotTests
{
    [Fact]
    public void Initial_IsOfflineAndHasNoProductionData()
    {
        Assert.Equal(PlcConnectionState.Offline, PlcMonitorSnapshot.Initial.State);
        Assert.Null(PlcMonitorSnapshot.Initial.Data);
    }
}
