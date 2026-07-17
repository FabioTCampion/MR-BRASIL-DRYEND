using DryEnd.Application;
using DryEnd.Domain;
using DryEnd.Infrastructure.Ads;
using Microsoft.AspNetCore.SignalR;

namespace DryEnd.Web;

public sealed class PlcMonitorWorker(
    IPlcConnection connection,
    IPlcMonitorStateStore store,
    IHubContext<DiagnosticsHub> hub,
    AdsOptions options,
    ILogger<PlcMonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishAsync(store.Current with
                {
                    State = connection.IsConnected ? PlcConnectionState.Online : PlcConnectionState.Connecting,
                    LastError = null
                }, stoppingToken);

                await connection.ConnectAsync(stoppingToken);
                await PublishAsync(store.Current with { State = PlcConnectionState.WaitingForStableData }, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var data = await connection.ReadSnapshotAsync(stoppingToken);
                    await PublishAsync(new PlcMonitorSnapshot(
                        PlcConnectionState.Online,
                        data,
                        data.CapturedAtUtc,
                        null), stoppingToken);
                    await Task.Delay(options.PollIntervalMilliseconds, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "ADS monitoring cycle failed. Retrying without changing the TwinCAT runtime state.");
                await PublishAsync(store.Current with
                {
                    State = PlcConnectionState.Reconnecting,
                    LastError = exception.Message
                }, stoppingToken);
                await SafeDisconnectAsync(stoppingToken);
                await Task.Delay(options.ReconnectDelayMilliseconds, stoppingToken);
            }
        }

        await SafeDisconnectAsync(CancellationToken.None);
        store.Update(store.Current with { State = PlcConnectionState.Offline });
    }

    private async Task PublishAsync(PlcMonitorSnapshot snapshot, CancellationToken cancellationToken)
    {
        store.Update(snapshot);
        await hub.Clients.All.SendAsync("diagnosticsUpdated", snapshot, cancellationToken);
    }

    private async Task SafeDisconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await connection.DisconnectAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "ADS disconnect failed during cleanup.");
        }
    }
}
