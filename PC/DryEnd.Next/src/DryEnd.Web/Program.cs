using DryEnd.Application;
using DryEnd.Infrastructure.Ads;
using DryEnd.Web;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var adsOptions = builder.Configuration
    .GetSection(AdsOptions.SectionName)
    .Get<AdsOptions>() ?? new AdsOptions();
adsOptions.Validate();

builder.Services.AddSingleton(adsOptions);
builder.Services.AddSingleton<IPlcMonitorStateStore, PlcMonitorStateStore>();
builder.Services.AddSingleton<AdsPlcConnection>();
builder.Services.AddSingleton<IPlcConnection>(provider => provider.GetRequiredService<AdsPlcConnection>());
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHostedService<PlcMonitorWorker>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseCors();
app.MapGet("/api/diagnostics", (IPlcMonitorStateStore store) => Results.Ok(store.Current));
app.MapGet("/api/diagnostics/symbols", (AdsPlcConnection connection, string search = "order") =>
    Results.Ok(connection.FindSymbolPaths(search)));
app.MapPost("/api/diagnostics/current-order/flute-type", async (
    DiagnosticFluteTypeWriteRequest request,
    AdsPlcConnection connection,
    CancellationToken cancellationToken) =>
    Results.Ok(await connection.WriteCurrentFluteTypeAsync(request.Value, cancellationToken)));
app.MapHub<DiagnosticsHub>("/hubs/diagnostics");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;

public sealed record DiagnosticFluteTypeWriteRequest(string Value);
