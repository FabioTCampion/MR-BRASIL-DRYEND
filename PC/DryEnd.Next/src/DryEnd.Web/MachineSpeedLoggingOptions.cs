namespace DryEnd.Web;

public sealed class MachineSpeedLoggingOptions
{
    public const string SectionName = "MachineSpeedLogging";

    public bool Enabled { get; set; } = true;
    public int CheckIntervalMilliseconds { get; set; } = 1_000;
    public int SlotIntervalSeconds { get; set; } = 30;
    public int MaximumSnapshotAgeSeconds { get; set; } = 10;

    public void Validate()
    {
        if (CheckIntervalMilliseconds < 500)
            throw new InvalidOperationException("Machine-speed logging check interval must be at least 500 ms.");
        if (SlotIntervalSeconds is < 1 or > 3600)
            throw new InvalidOperationException("Machine-speed logging slot interval must be between 1 and 3600 seconds.");
        if (MaximumSnapshotAgeSeconds is < 1 or > 3600)
            throw new InvalidOperationException("Maximum PLC snapshot age must be between 1 and 3600 seconds.");
    }
}
