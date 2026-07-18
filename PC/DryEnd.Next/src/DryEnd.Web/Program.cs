using DryEnd.Application;
using DryEnd.Infrastructure.Ads;
using DryEnd.Infrastructure.Database;
using DryEnd.Web;
using System.Text.Json.Serialization;
using DryEnd.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

var externalConfigurationPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "CPNTeck",
    "DryEnd",
    "appsettings.Production.json");
builder.Configuration.AddJsonFile(externalConfigurationPath, optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddWindowsService(options => options.ServiceName = "CPNTeck Dry End");

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
var changeOrderHandshakeOptions = builder.Configuration
    .GetSection(ChangeOrderHandshakeOptions.SectionName)
    .Get<ChangeOrderHandshakeOptions>() ?? new ChangeOrderHandshakeOptions();
changeOrderHandshakeOptions.Validate();
var machineSpeedLoggingOptions = builder.Configuration
    .GetSection(MachineSpeedLoggingOptions.SectionName)
    .Get<MachineSpeedLoggingOptions>() ?? new MachineSpeedLoggingOptions();
machineSpeedLoggingOptions.Validate();

builder.Services.AddSingleton(adsOptions);
builder.Services.AddSingleton(databaseOptions);
builder.Services.AddSingleton(nextOrderSyncOptions);
builder.Services.AddSingleton(changeOrderHandshakeOptions);
builder.Services.AddSingleton(machineSpeedLoggingOptions);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProductionDataRepository, ProductionDataRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddSingleton<ITrimboxOrderParser, TrimboxOrderParser>();
builder.Services.AddSingleton<IDatabaseConnectionFactory, ProviderDatabaseConnectionFactory>();
builder.Services.AddSingleton<IProductionQueries, ProviderProductionQueries>();
builder.Services.AddSingleton<IDatabaseSchemaMigrator, DatabaseSchemaMigrator>();
builder.Services.AddHostedService<DatabaseInitializationWorker>();
builder.Services.AddSingleton<IPlcMonitorStateStore, PlcMonitorStateStore>();
builder.Services.AddSingleton<AdsPlcConnection>();
builder.Services.AddSingleton<IPlcConnection>(provider => provider.GetRequiredService<AdsPlcConnection>());
builder.Services.AddSingleton<IPlcOrderEditor>(provider => provider.GetRequiredService<AdsPlcConnection>());
builder.Services.AddSingleton<IPlcNextOrderWriter>(provider => provider.GetRequiredService<AdsPlcConnection>());
builder.Services.AddSingleton<IPlcChangeOrderAcknowledger>(provider => provider.GetRequiredService<AdsPlcConnection>());
builder.Services.AddSingleton<IPlcOrderCommandWriter>(provider => provider.GetRequiredService<AdsPlcConnection>());
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHostedService<PlcMonitorWorker>();
builder.Services.AddHostedService<NextOrderSyncWorker>();
builder.Services.AddHostedService<ChangeOrderHandshakeWorker>();
builder.Services.AddHostedService<MachineSpeedLoggingWorker>();
builder.Services.AddHostedService<ProductionStopWorker>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
var keyPath = builder.Configuration["Authentication:KeyPath"] ?? Path.Combine(AppContext.BaseDirectory, "data", "keys");
Directory.CreateDirectory(keyPath);
builder.Services.AddDataProtection().SetApplicationName("CPNTeck.DryEnd").PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "DryEnd.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApplicationRoles.Operator, policy => policy.RequireRole(ApplicationRoles.Operator, ApplicationRoles.Supervisor, ApplicationRoles.Administrator))
    .AddPolicy(ApplicationRoles.Supervisor, policy => policy.RequireRole(ApplicationRoles.Supervisor, ApplicationRoles.Administrator))
    .AddPolicy(ApplicationRoles.Administrator, policy => policy.RequireRole(ApplicationRoles.Administrator));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    var isOrderCommand = context.Request.Method == HttpMethods.Post &&
        context.Request.Path.StartsWithSegments("/api/plc/current-order") &&
        context.Request.Path.Value?.EndsWith("change-request", StringComparison.OrdinalIgnoreCase) == true;
    if (!isOrderCommand)
    {
        await next();
        return;
    }

    var requestLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    requestLogger.LogWarning(
        "PLC order command HTTP request received. Path={Path}.",
        context.Request.Path);
    var commandStopwatch = System.Diagnostics.Stopwatch.StartNew();
    await next();
    commandStopwatch.Stop();
    requestLogger.LogWarning(
        "PLC order command HTTP request completed. Path={Path}, StatusCode={StatusCode}, ElapsedMilliseconds={ElapsedMilliseconds}.",
        context.Request.Path,
        context.Response.StatusCode,
        commandStopwatch.ElapsedMilliseconds);
});
app.MapGet("/api/diagnostics", (IPlcMonitorStateStore store) => Results.Ok(store.Current)).RequireAuthorization();
app.MapGet("/api/diagnostics/symbols", (AdsPlcConnection connection, string search = "order") =>
    Results.Ok(connection.FindSymbolPaths(search))).RequireAuthorization(ApplicationRoles.Supervisor);
app.MapHub<DiagnosticsHub>("/hubs/diagnostics").RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (
    IProductionDataRepository repository,
    IPlcMonitorStateStore monitor,
    CancellationToken cancellationToken) =>
{
    var database = await repository.GetStatusAsync(cancellationToken);
    var plcOnline = monitor.Current.State == PlcConnectionState.Online;
    var ready = database.Available && plcOnline;
    return Results.Json(new
    {
        status = ready ? "ready" : "not-ready",
        ready,
        databaseAvailable = database.Available,
        plcOnline
    }, statusCode: ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
});
app.MapGet("/api/version", () => Results.Ok(new
{
    version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
    environment = app.Environment.EnvironmentName
}));
app.MapProductionDataEndpoints();
app.MapPlcOrderEndpoints();
app.MapTrimboxImportEndpoints();
app.MapAuthenticationEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
