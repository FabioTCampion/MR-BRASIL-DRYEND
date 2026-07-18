using DryEnd.Application;
using DryEnd.Domain;
using System.Security.Claims;

namespace DryEnd.Web;

public static class ProductionDataEndpoints
{
    public static void MapProductionDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/production-data").RequireAuthorization();

        group.MapGet("/status", async (IProductionDataRepository repository, CancellationToken token) =>
            Results.Ok(await repository.GetStatusAsync(token)));

        group.MapGet("/orders", async (IProductionDataRepository repository, CancellationToken token) =>
            await ExecuteAsync(() => repository.GetQueueAsync(token)));

        group.MapGet("/history", async (
            IProductionDataRepository repository,
            OrderSearchMode mode,
            string? search,
            DateTime? date,
            CancellationToken token) =>
            await ExecuteAsync(() => repository.GetHistoryAsync(mode, search, date, token)));

        group.MapGet("/machine-speed", async (
            IProductionDataRepository repository,
            DateTime date,
            CancellationToken token) =>
            await ExecuteAsync(() => repository.GetMachineSpeedAsync(date, token)));

        group.MapGet("/stop-reasons", async (IProductionDataRepository repository, CancellationToken token) =>
            await ExecuteAsync(() => repository.GetStopReasonsAsync(token)));

        group.MapGet("/stops", async (DateTime date, IProductionDataRepository repository, CancellationToken token) =>
            await ExecuteAsync(() => repository.GetStopsAsync(date, token)));

        group.MapGet("/stops/pending-count", async (IProductionDataRepository repository, CancellationToken token) =>
            await ExecuteAsync(async () => new { count = await repository.GetPendingStopCountAsync(token) }));

        group.MapPut("/stops/{id:long}/justification", async (
            long id,
            ProductionStopJustification justification,
            ClaimsPrincipal principal,
            IProductionDataRepository repository,
            TimeProvider timeProvider,
            CancellationToken token) =>
            await ExecuteAsync(async () => new
            {
                updated = await repository.JustifyStopAsync(id, justification with { JustifiedBy = principal.Identity!.Name! }, timeProvider.GetLocalNow().DateTime, token)
            })).RequireAuthorization(ApplicationRoles.Operator);

        group.MapPost("/orders", async (
            ProductionOrderRecord order,
            IProductionDataRepository repository,
            CancellationToken token) =>
        {
            order.CreatedBy = "SYSTEM-WEB";
            order.LastModifiedBy = "SYSTEM-WEB";
            return await ExecuteAsync(async () => new { id = await repository.CreateAsync(order, token) });
        }).RequireAuthorization(ApplicationRoles.Supervisor);

        group.MapPut("/orders/{id:int}", async (
            int id,
            ProductionOrderRecord order,
            IProductionDataRepository repository,
            CancellationToken token) =>
        {
            order.Id = id;
            order.LastModifiedBy = "SYSTEM-WEB";
            return await ExecuteAsync(() => repository.UpdateAsync(order, token));
        }).RequireAuthorization(ApplicationRoles.Supervisor);

        group.MapDelete("/orders/{id:int}", async (
            int id,
            IProductionDataRepository repository,
            CancellationToken token) =>
            await ExecuteAsync(() => repository.DeleteAsync(id, token))).RequireAuthorization(ApplicationRoles.Supervisor);

        group.MapDelete("/orders", async (
            IProductionDataRepository repository,
            IPlcMonitorStateStore monitor,
            CancellationToken token) =>
        {
            var plcData = monitor.Current.Data;
            var currentTableId = plcData?.CurrentOrder.TableId ?? 0;
            var nextTableId = plcData?.NextOrder.TableId ?? 0;
            return await ExecuteAsync(async () => new
            {
                deletedCount = await repository.ClearPendingAsync(currentTableId, nextTableId, token),
                protectedTableIds = new[] { currentTableId, nextTableId }.Where(id => id > 0).Distinct()
            });
        }).RequireAuthorization(ApplicationRoles.Supervisor);

        group.MapPut("/orders/sequence", async (
            ReorderProductionQueue request,
            ClaimsPrincipal principal,
            IProductionDataRepository repository,
            IPlcMonitorStateStore monitor,
            CancellationToken token) =>
        {
            if (monitor.Current.Data?.CurrentOrder.ChangeOrderRequest == true)
                return Results.Conflict(new { error = "Troca automática em andamento. Aguarde para reorganizar a fila." });
            try
            {
                var updated = await repository.ReorderPendingAsync(
                    request.OrderedIds,
                    principal.Identity!.Name!,
                    token);
                return updated.HasValue
                    ? Results.Ok(new { updatedCount = updated.Value })
                    : Results.Conflict(new { error = "A fila foi alterada por outro processo. Recarregue e tente novamente." });
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        }).RequireAuthorization(ApplicationRoles.Operator);

        group.MapPut("/orders/{id:int}/swap-levels", async (
            int id,
            ClaimsPrincipal principal,
            IProductionDataRepository repository,
            IPlcMonitorStateStore monitor,
            CancellationToken token) =>
        {
            if (monitor.Current.Data?.CurrentOrder.ChangeOrderRequest == true)
                return Results.Conflict(new { error = "Troca automática em andamento. Aguarde para inverter os níveis." });
            return await ExecuteAsync(async () => new
            {
                swapped = await repository.SwapPendingOrderLevelsAsync(
                    id,
                    principal.Identity!.Name!,
                    token)
            });
        }).RequireAuthorization(ApplicationRoles.Supervisor);

        group.MapPut("/history/{id:int}", async (
            int id,
            ProductionOrderRecord order,
            ClaimsPrincipal principal,
            IProductionDataRepository repository,
            CancellationToken token) =>
        {
            order.Id = id;
            order.LastModifiedBy = principal.Identity!.Name!;
            return await ExecuteAsync(async () => new
            {
                updated = await repository.UpdateHistoryAsync(order, token)
            });
        }).RequireAuthorization(ApplicationRoles.Administrator);

        group.MapPost("/history/{id:int}/recover", async (
            int id,
            IProductionDataRepository repository,
            CancellationToken token) =>
            await ExecuteAsync(async () => new
            {
                recovered = await repository.RecoverHistoryAsync(id, token)
            })).RequireAuthorization(ApplicationRoles.Supervisor);

        group.MapDelete("/history/{id:int}", async (
            int id,
            IProductionDataRepository repository,
            CancellationToken token) =>
            await ExecuteAsync(async () => new
            {
                deleted = await repository.DeleteHistoryAsync(id, token)
            })).RequireAuthorization(ApplicationRoles.Administrator);

        group.MapDelete("/history", async (
            IProductionDataRepository repository,
            CancellationToken token) =>
            await ExecuteAsync(async () => new
            {
                deletedCount = await repository.ClearHistoryAsync(token)
            })).RequireAuthorization(ApplicationRoles.Administrator);
    }

    private sealed record ReorderProductionQueue(IReadOnlyList<int> OrderedIds);

    private static async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            return Results.Problem(
                title: "Database operation unavailable",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
