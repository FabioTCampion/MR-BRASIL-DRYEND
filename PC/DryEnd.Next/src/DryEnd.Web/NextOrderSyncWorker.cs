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
        if (currentOrder.ChangeOrderRequest)
        {
            LogBlockedOnce("change-order", "Next-order synchronization is paused while an automatic order change is in progress.");
            return;
        }
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

        // EN: The same ID is already synchronized; preserve it unless its data diverged.
        // PT: O mesmo ID ja esta sincronizado; preserva-o caso os dados estejam divergentes.
        if (plcNextOrder.TableId == update.TableId)
        {
            if (!update.Matches(plcNextOrder))
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
            "Queued order {TableId} replaced PLC nextOrder table ID {PreviousTableId} and was confirmed by ADS readback.",
            update.TableId,
            plcNextOrder.TableId);
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
