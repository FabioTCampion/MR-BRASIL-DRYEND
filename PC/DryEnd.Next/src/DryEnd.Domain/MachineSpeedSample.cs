namespace DryEnd.Domain;

public sealed record MachineSpeedSample(DateTime Slot, int Speed)
{
    public static bool TryCreate(
        PlcMonitorSnapshot snapshot,
        DateTime localNow,
        DateTimeOffset utcNow,
        int slotIntervalSeconds,
        int maximumSnapshotAgeSeconds,
        out MachineSpeedSample? sample)
    {
        sample = null;

        if (snapshot.State != PlcConnectionState.Online ||
            snapshot.Data is null ||
            snapshot.LastSuccessfulReadUtc is null ||
            snapshot.Data.CapturedAtUtc == default ||
            !float.IsFinite(snapshot.Data.CurrentOrder.LineSpeed) ||
            slotIntervalSeconds <= 0 ||
            maximumSnapshotAgeSeconds <= 0)
        {
            return false;
        }

        var maximumAge = TimeSpan.FromSeconds(maximumSnapshotAgeSeconds);
        var readAge = utcNow - snapshot.LastSuccessfulReadUtc.Value;
        var captureAge = utcNow - snapshot.Data.CapturedAtUtc;
        if (readAge < TimeSpan.Zero || readAge > maximumAge ||
            captureAge < TimeSpan.Zero || captureAge > maximumAge)
        {
            return false;
        }

        var slotTicks = TimeSpan.FromSeconds(slotIntervalSeconds).Ticks;
        var stableSlot = new DateTime(
            localNow.Ticks - (localNow.Ticks % slotTicks),
            localNow.Kind);
        var boundedSpeed = Math.Clamp(snapshot.Data.CurrentOrder.LineSpeed, 0.0f, 300.0f);
        var roundedSpeed = (int)Math.Round(boundedSpeed, MidpointRounding.AwayFromZero);

        sample = new MachineSpeedSample(stableSlot, roundedSpeed);
        return true;
    }
}
