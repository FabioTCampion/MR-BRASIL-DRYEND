using DryEnd.Application;
using DryEnd.Infrastructure.Ads;
using DryEnd.Infrastructure.Database;
using DryEnd.Web;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var adsOptions = builder.Configuration
    .GetSection(AdsOptions.SectionName)
    .Get<AdsOptions>() ?? new AdsOptions();
adsOptions.Validate();
var databaseOptions = new DatabaseOptions
{
    Provider = builder.Configuration.GetValue<DatabaseProvider>($"{DatabaseOptions.SectionName}:Provider"),
    ConnectionString = builder.Configuration.GetConnectionString("DryEnd"),
    OrdersTable = builder.Configuration[$"{DatabaseOptions.SectionName}:OrdersTable"] ?? "dbo.ProductionList_Plc",
    MachineSpeedTable = builder.Configuration[$"{DatabaseOptions.SectionName}:MachineSpeedTable"] ?? "dbo.MachineSpeedRecords"
};
var nextOrderSyncOptions = builder.Configuration
    .GetSection(NextOrderSyncOptions.SectionName)
    .Get<NextOrderSyncOptions>() ?? new NextOrderSyncOptions();
nextOrderSyncOptions.Validate();
var machineSpeedLoggingOptions = builder.Configuration
    .GetSection(MachineSpeedLoggingOptions.SectionName)
    .Get<MachineSpeedLoggingOptions>() ?? new MachineSpeedLoggingOptions();
machineSpeedLoggingOptions.Validate();

builder.Services.AddSingleton(adsOptions);
builder.Services.AddSingleton(databaseOptions);
builder.Services.AddSingleton(nextOrderSyncOptions);
builder.Services.AddSingleton(machineSpeedLoggingOptions);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProductionDataRepository, ProductionDataRepository>();
builder.Services.AddSingleton<IDatabaseConnectionFactory, ProviderDatabaseConnectionFactory>();
builder.Services.AddSingleton<IProductionQueries, ProviderProductionQueries>();
builder.Services.AddSingleton<IPlcMonitorStateStore, PlcMonitorStateStore>();
builder.Services.AddSingleton<AdsPlcConnection>();
builder.Services.AddSingleton<IPlcConnection>(provider => provider.GetRequiredService<AdsPlcConnection>());
builder.Services.AddSingleton<IPlcOrderEditor>(provider => provider.GetRequiredService<AdsPlcConnection>());
builder.Services.AddSingleton<IPlcNextOrderWriter>(provider => provider.GetRequiredService<AdsPlcConnection>());
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHostedService<PlcMonitorWorker>();
builder.Services.AddHostedService<NextOrderSyncWorker>();
builder.Services.AddHostedService<MachineSpeedLoggingWorker>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseCors();
app.MapGet("/api/diagnostics", (IPlcMonitorStateStore store) => Results.Ok(store.Current));
app.MapGet("/api/diagnostics/symbols", (AdsPlcConnection connection, string search = "order") =>
    Results.Ok(connection.FindSymbolPaths(search)));
app.MapHub<DiagnosticsHub>("/hubs/diagnostics");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapProductionDataEndpoints();
app.MapPlcOrderEndpoints();

app.Run();

public partial class Program;
