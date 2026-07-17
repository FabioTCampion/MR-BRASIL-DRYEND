namespace DryEnd.Web;

public sealed class NextOrderSyncOptions
{
    public const string SectionName = "NextOrderSync";

    public bool Enabled { get; set; } = true;
    public int IntervalMilliseconds { get; set; } = 5_000;

    public void Validate()
    {
        if (IntervalMilliseconds < 1_000)
            throw new InvalidOperationException("Next-order synchronization interval must be at least 1000 ms.");
    }
}
