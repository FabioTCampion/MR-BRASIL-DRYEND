using DryEnd.Application;
using DryEnd.Domain;

namespace DryEnd.Web;

public sealed class NextOrderSyncWorker(
    IProductionDataRepository repository,
    IPlcNextOrderWriter writer,
    IPlcMonitorStateStore monitor,
    NextOrderSyncOptions options,
    ILogger<NextOrderSyncWorker> logger) : BackgroundService
{
    private string? _lastBlockedReason;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Automatic next-order synchronization is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SynchronizeOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogBlockedOnce($"error:{exception.Message}", exception.Message, exception);
            }

            await Task.Delay(options.IntervalMilliseconds, stoppingToken);
        }
    }

    private async Task SynchronizeOnceAsync(CancellationToken cancellationToken)
    {
        var snapshot = monitor.Current;
        if (snapshot.State != PlcConnectionState.Online || snapshot.Data is null)
            return;

        var currentOrder = snapshot.Data.CurrentOrder;
        var plcNextOrder = snapshot.Data.NextOrder;
        var queue = await repository.GetQueueAsync(cancellationToken);
        var candidate = queue
            .Where(order => order.ProductionState is < 1)
            .Where(order => order.ProductionSequence is > 0)
            .Where(order => order.Id != currentOrder.TableId)
            .OrderBy(order => order.ProductionSequence)
            .ThenBy(order => order.Id)
            .FirstOrDefault();

        if (candidate is null)
        {
            _lastBlockedReason = null;
            return;
        }

        NextOrderUpdate update;
        try
        {
            update = NextOrderUpdate.FromDatabase(candidate);
        }
        catch (ArgumentException exception)
        {
            LogBlockedOnce($"invalid:{candidate.Id}:{exception.Message}",
                $"Queued order {candidate.Id} was not written to the PLC because validation failed: {exception.Message}");
            return;
        }

        // EN: A different positive ID means the PLC already owns an accepted next order.
        // PT: Um ID positivo diferente indica que o PLC ja possui um proximo pedido aceito.
        if (plcNextOrder.TableId > 0 && plcNextOrder.TableId != currentOrder.TableId)
        {
            if (plcNextOrder.TableId != update.TableId)
                LogBlockedOnce($"occupied:{plcNextOrder.TableId}:{update.TableId}",
                    $"PLC next-order slot is occupied by table ID {plcNextOrder.TableId}; queued order {update.TableId} was not written.");
            else if (!update.Matches(plcNextOrder))
                LogBlockedOnce($"divergent:{update.TableId}",
                    $"PLC next order {update.TableId} differs from SQL. PLC data was preserved and not overwritten.");
            else
                _lastBlockedReason = null;
            return;
        }

        var readback = await writer.WriteNextOrderAsync(update, cancellationToken);
        if (!update.Matches(readback))
            throw new InvalidOperationException($"ADS readback did not confirm queued order {update.TableId}.");

        _lastBlockedReason = null;
        logger.LogInformation(
            "Queued order {TableId} was written to PLC nextOrder and confirmed by ADS readback.",
            update.TableId);
    }

    private void LogBlockedOnce(string key, string message, Exception? exception = null)
    {
        if (string.Equals(_lastBlockedReason, key, StringComparison.Ordinal))
            return;

        _lastBlockedReason = key;
        if (exception is null)
            logger.LogWarning("{Message}", message);
        else
            logger.LogWarning(exception, "{Message}", message);
    }
}
