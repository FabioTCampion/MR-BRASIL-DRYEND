using DryEnd.Application;
using DryEnd.Domain;

namespace DryEnd.Web;

public sealed class MachineSpeedLoggingWorker(
    IProductionDataRepository repository,
    IPlcMonitorStateStore monitor,
    MachineSpeedLoggingOptions options,
    TimeProvider timeProvider,
    ILogger<MachineSpeedLoggingWorker> logger) : BackgroundService
{
    private DateTime? _lastCompletedSlot;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Machine-speed logging is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await LogCurrentSlotAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Machine-speed logging failed and will retry without changing the PLC runtime state.");
            }

            await Task.Delay(options.CheckIntervalMilliseconds, stoppingToken);
        }
    }

    private async Task LogCurrentSlotAsync(CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeProvider.LocalTimeZone).DateTime;
        if (!MachineSpeedSample.TryCreate(
                monitor.Current,
                localNow,
                utcNow,
                options.SlotIntervalSeconds,
                options.MaximumSnapshotAgeSeconds,
                out var sample) ||
            sample is null ||
            sample.Slot == _lastCompletedSlot)
        {
            return;
        }

        // EN: Database-side deduplication remains authoritative across service restarts.
        // PT: A deduplicacao no banco permanece autoritativa entre reinicializacoes do servico.
        var inserted = await repository.TryAddMachineSpeedAsync(sample, cancellationToken);
        _lastCompletedSlot = sample.Slot;

        if (inserted)
            logger.LogDebug("Machine speed {Speed} m/min was recorded for slot {Slot}.", sample.Speed, sample.Slot);
    }
}
