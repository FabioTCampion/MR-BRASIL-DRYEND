using DryEnd.Application;
using DryEnd.Domain;

namespace DryEnd.Web;

public static class PlcOrderEndpoints
{
    public static void MapPlcOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/plc/current-order", async (
            CurrentOrderUpdate update,
            IPlcOrderEditor editor,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var readback = await editor.UpdateCurrentOrderAsync(update, cancellationToken);
                logger.LogInformation(
                    "PLC current order updated through ADS. TableId={TableId}, ProductionList={ProductionListNumber}",
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
        });
    }
}
