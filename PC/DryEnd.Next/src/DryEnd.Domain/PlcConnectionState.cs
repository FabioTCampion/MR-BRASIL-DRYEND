namespace DryEnd.Domain;

public enum PlcConnectionState
{
    Offline,
    Connecting,
    WaitingForStableData,
    Online,
    Degraded,
    Reconnecting
}
