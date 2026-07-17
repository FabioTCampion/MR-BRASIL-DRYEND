using DryEnd.Application;
using DryEnd.Domain;

namespace DryEnd.Web;

public static class ProductionDataEndpoints
{
    public static void MapProductionDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/production-data");

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

        group.MapPost("/orders", async (
            ProductionOrderRecord order,
            IProductionDataRepository repository,
            CancellationToken token) =>
            await ExecuteAsync(async () => new { id = await repository.CreateAsync(order, token) }));

        group.MapPut("/orders/{id:int}", async (
            int id,
            ProductionOrderRecord order,
            IProductionDataRepository repository,
            CancellationToken token) =>
        {
            order.Id = id;
            return await ExecuteAsync(() => repository.UpdateAsync(order, token));
        });

        group.MapDelete("/orders/{id:int}", async (
            int id,
            IProductionDataRepository repository,
            CancellationToken token) =>
            await ExecuteAsync(() => repository.DeleteAsync(id, token)));
    }

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
