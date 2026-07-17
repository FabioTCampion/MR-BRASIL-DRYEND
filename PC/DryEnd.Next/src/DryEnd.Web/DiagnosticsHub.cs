using DryEnd.Application;
using DryEnd.Domain;
using Microsoft.AspNetCore.SignalR;

namespace DryEnd.Web;

public sealed class DiagnosticsHub(IPlcMonitorStateStore store) : Hub
{
    public PlcMonitorSnapshot GetCurrentSnapshot() => store.Current;
}
