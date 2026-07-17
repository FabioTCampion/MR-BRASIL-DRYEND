namespace DryEnd.Domain;

public sealed record PlcMonitorSnapshot(
    PlcConnectionState State,
    PlcDataSnapshot? Data,
    DateTimeOffset? LastSuccessfulReadUtc,
    string? LastError)
{
    public static PlcMonitorSnapshot Initial { get; } =
        new(PlcConnectionState.Offline, null, null, null);
}
