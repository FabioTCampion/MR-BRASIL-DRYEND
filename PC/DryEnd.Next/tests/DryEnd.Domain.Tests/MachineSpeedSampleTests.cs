namespace DryEnd.Domain.Tests;

public sealed class MachineSpeedSampleTests
{
    [Theory]
    [InlineData(-12.5f, 0)]
    [InlineData(42.6f, 43)]
    [InlineData(450.0f, 300)]
    public void TryCreate_UsesStableSlotAndBoundsSpeed(float sourceSpeed, int expectedSpeed)
    {
        var capturedAt = new DateTimeOffset(2026, 7, 17, 13, 22, 44, TimeSpan.Zero);
        var snapshot = CreateSnapshot(PlcConnectionState.Online, sourceSpeed, capturedAt);

        var created = MachineSpeedSample.TryCreate(
            snapshot,
            new DateTime(2026, 7, 17, 10, 22, 44, 890, DateTimeKind.Local),
            capturedAt.AddSeconds(2),
            30,
            10,
            out var sample);

        Assert.True(created);
        Assert.NotNull(sample);
        Assert.Equal(new DateTime(2026, 7, 17, 10, 22, 30, DateTimeKind.Local), sample.Slot);
        Assert.Equal(expectedSpeed, sample.Speed);
    }

    [Theory]
    [InlineData(PlcConnectionState.Offline, 10.0f)]
    [InlineData(PlcConnectionState.Reconnecting, 10.0f)]
    [InlineData(PlcConnectionState.Online, float.NaN)]
    [InlineData(PlcConnectionState.Online, float.PositiveInfinity)]
    public void TryCreate_RejectsUnavailableOrInvalidSnapshot(PlcConnectionState state, float speed)
    {
        var snapshot = CreateSnapshot(state, speed, DateTimeOffset.UtcNow);

        var created = MachineSpeedSample.TryCreate(
            snapshot,
            DateTime.Now,
            snapshot.Data!.CapturedAtUtc.AddSeconds(1),
            30,
            10,
            out var sample);

        Assert.False(created);
        Assert.Null(sample);
    }

    [Fact]
    public void TryCreate_RejectsStaleOnlineSnapshot()
    {
        var capturedAt = new DateTimeOffset(2026, 7, 17, 13, 22, 0, TimeSpan.Zero);
        var snapshot = CreateSnapshot(PlcConnectionState.Online, 75.0f, capturedAt);

        var created = MachineSpeedSample.TryCreate(
            snapshot,
            new DateTime(2026, 7, 17, 10, 22, 30, DateTimeKind.Local),
            capturedAt.AddSeconds(11),
            30,
            10,
            out var sample);

        Assert.False(created);
        Assert.Null(sample);
    }

    private static PlcMonitorSnapshot CreateSnapshot(
        PlcConnectionState state,
        float speed,
        DateTimeOffset capturedAt)
    {
        var channel = new OrderChannelSnapshot(0, string.Empty, string.Empty, 0, 0, 0,
            new short[5], 0, 0, 0, 0, 0, 0, 0, 0);
        var generated = new GeneratedOrderSnapshot(0, 0, 0, 0, 0, 0, 0, 0,
            Array.Empty<ToolReferenceSnapshot>(), Array.Empty<ToolReferenceSnapshot>(), false,
            Array.Empty<int>(), Array.Empty<int>());
        var order = new OrderSnapshot(string.Empty, 0, 0, 0, string.Empty, string.Empty, 0,
            new string[5], speed, 0, 0, 0, 0, false, false, false, false, false, false, false,
            channel, channel, generated);
        var data = new PlcDataSnapshot(order, order, capturedAt);
        return new PlcMonitorSnapshot(state, data, capturedAt, null);
    }
}
