using DryEnd.Application;
using DryEnd.Domain;
using System.Security.Claims;

namespace DryEnd.Web;

public static class PlcOrderEndpoints
{
    public static void MapPlcOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/plc/next-order/comparison", async (
            IProductionDataRepository repository,
            IPlcMonitorStateStore monitor,
            CancellationToken cancellationToken) =>
        {
            var plcNextOrder = monitor.Current.Data?.NextOrder;
            if (plcNextOrder is null || plcNextOrder.TableId <= 0)
                return Results.Ok(new NextOrderComparison(false, false, 0, [], null));

            var candidate = (await repository.GetQueueAsync(cancellationToken))
                .FirstOrDefault(order => order.Id == plcNextOrder.TableId);
            if (candidate is null)
                return Results.Ok(new NextOrderComparison(true, false, plcNextOrder.TableId, [], "O pedido do PLC não foi encontrado na fila do sistema."));

            try
            {
                var expected = NextOrderUpdate.FromDatabase(candidate);
                var differences = Compare(expected, plcNextOrder);
                return Results.Ok(new NextOrderComparison(true, differences.Count > 0, plcNextOrder.TableId, differences, null));
            }
            catch (ArgumentException exception)
            {
                return Results.Ok(new NextOrderComparison(true, true, plcNextOrder.TableId, [], exception.Message));
            }
        }).RequireAuthorization();

        app.MapPut("/api/plc/current-order", async (
            CurrentOrderPatch patch,
            IPlcOrderEditor editor,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var readback = await editor.PatchCurrentOrderAsync(patch, cancellationToken);
                logger.LogInformation(
                    "PLC current order patched through ADS. TableId={TableId}, ProductionList={ProductionListNumber}",
                    readback.TableId,
                    readback.ProductionListNumber);
                return Results.Ok(readback);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "PLC current order update failed.");
                return Results.Problem(
                    title: "PLC current order update failed",
                    detail: exception.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).RequireAuthorization(ApplicationRoles.Supervisor);

        app.MapPost("/api/plc/next-order/{id:int}", async (
            int id,
            bool overwrite,
            IProductionDataRepository repository,
            IPlcNextOrderWriter writer,
            IPlcMonitorStateStore monitor,
            ClaimsPrincipal principal,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var candidate = (await repository.GetQueueAsync(cancellationToken))
                    .FirstOrDefault(order => order.Id == id);
                if (candidate is null)
                    return Results.NotFound(new { error = $"Pending order {id} was not found." });

                var plcNextOrder = monitor.Current.Data?.NextOrder;
                var currentOrder = monitor.Current.Data?.CurrentOrder;
                if (currentOrder?.ChangeOrderRequest == true || currentOrder?.AocRequest == true)
                    return Results.Conflict(new { error = "Troca automática em andamento. Aguarde para reescrever o próximo pedido." });
                if (!overwrite && plcNextOrder is { TableId: > 0 } && plcNextOrder.TableId != id)
                    return Results.Conflict(new
                    {
                        error = $"PLC nextOrder is occupied by table ID {plcNextOrder.TableId}. Set overwrite=true only for an authorized manual test."
                    });

                var update = NextOrderUpdate.FromDatabase(candidate);
                var differences = plcNextOrder is null ? [] : Compare(update, plcNextOrder);
                var readback = await writer.WriteNextOrderAsync(update, cancellationToken);
                if (!update.Matches(readback))
                    throw new InvalidOperationException($"ADS readback did not confirm next order {id}.");

                logger.LogWarning(
                    "PLC nextOrder was manually rewritten and confirmed by ADS readback. TableId={TableId}, User={User}, Overwrite={Overwrite}, DifferentFields={DifferentFields}",
                    id,
                    principal.Identity?.Name ?? "unknown",
                    overwrite,
                    string.Join(", ", differences.Select(difference => difference.Field)));
                return Results.Ok(readback);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Manual PLC next-order update failed for table ID {TableId}.", id);
                return Results.Problem(
                    title: "PLC next-order update failed",
                    detail: exception.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).RequireAuthorization(ApplicationRoles.Supervisor);

        app.MapPost("/api/plc/current-order/change-request", async (
            IPlcOrderCommandWriter writer,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            await ExecuteCommandAsync(
                writer.RequestChangeOrderAsync,
                "Manual order-change request",
                logger,
                cancellationToken)).RequireAuthorization(ApplicationRoles.Operator);

        app.MapPost("/api/plc/current-order/automatic-change-request", async (
            IPlcOrderCommandWriter writer,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            await ExecuteCommandAsync(
                writer.RequestAutomaticOrderChangeAsync,
                "Automatic order-change request",
                logger,
                cancellationToken)).RequireAuthorization(ApplicationRoles.Operator);
    }

    private static List<NextOrderDifference> Compare(NextOrderUpdate expected, OrderSnapshot actual)
    {
        var result = new List<NextOrderDifference>();
        Add(result, "Lista de produção", expected.ProductionListNumber, actual.ProductionListNumber);
        Add(result, "Seletor de nível", expected.LevelSelector, actual.LevelSelector);
        Add(result, "Composição", expected.PaperComposition, actual.PaperComposition);
        Add(result, "Onda", expected.FluteType, actual.FluteType);
        Add(result, "Largura", expected.PaperWidth, actual.PaperWidth);
        for (var index = 0; index < 5; index++)
            Add(result, $"Papel {index + 1}", expected.PaperLayers[index], actual.PaperLayers.ElementAtOrDefault(index) ?? string.Empty);
        AddChannel(result, "Pedido 1", expected.Order1, actual.Order1);
        if (expected.LevelSelector == 3)
            AddChannel(result, "Pedido 2", expected.Order2, actual.Order2);
        return result;
    }

    private static void AddChannel(List<NextOrderDifference> result, string name, NextOrderChannelUpdate expected, OrderChannelSnapshot actual)
    {
        Add(result, $"{name} · OF", expected.Id, actual.Id);
        Add(result, $"{name} · Produto", expected.Product, actual.Product);
        Add(result, $"{name} · Cliente", expected.Client, actual.Client);
        Add(result, $"{name} · Tipo de chapa", expected.SheetType, actual.SheetType);
        Add(result, $"{name} · Chapas por corte", expected.SheetQuantity, actual.SheetQuantity);
        Add(result, $"{name} · Comprimento", expected.SheetLength, actual.SheetLength);
        for (var index = 0; index < 5; index++)
            Add(result, $"{name} · M{index + 1}", expected.SheetMeasures[index], actual.SheetMeasures.ElementAtOrDefault(index));
        Add(result, $"{name} · Número de cortes", expected.NumberOfCuts, actual.NumberOfCuts);
        Add(result, $"{name} · Quantidade da pilha", expected.PileQuantity, actual.PileQuantity);
    }

    private static void Add<T>(List<NextOrderDifference> result, string field, T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
            return;
        result.Add(new NextOrderDifference(field, expected?.ToString() ?? string.Empty, actual?.ToString() ?? string.Empty));
    }

    private sealed record NextOrderDifference(string Field, string SystemValue, string PlcValue);
    private sealed record NextOrderComparison(bool Available, bool Divergent, int TableId, IReadOnlyList<NextOrderDifference> Differences, string? Error);

    private static async Task<IResult> ExecuteCommandAsync(
        Func<CancellationToken, Task<bool>> command,
        string commandName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var confirmed = await command(cancellationToken);
            logger.LogWarning(
                "{CommandName} written and confirmed through the dedicated ADS command channel.",
                commandName);
            return Results.Ok(new { confirmed });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{CommandName} failed.", commandName);
            return Results.Problem(
                title: $"{commandName} failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
