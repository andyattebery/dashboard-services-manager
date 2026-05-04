using Dsm.Managers.Configuration;
using Dsm.Managers.HostBuilder;
using Dsm.Shared.Configuration;
using Dsm.Shared.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

DsmSerilog.InitializeBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ConfigureDsmDefaults(builder.Configuration, services)
        .Enrich.WithClientIp()
        .Enrich.WithRequestHeader("User-Agent"));

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddControllers();
    builder.Services.AddHealthChecks();
    builder.Services
        .AddOpenApi()
        .AddProblemDetails()
        .AddDsmManagerServices();

    builder.Configuration
        .AddDsmManagerConfiguration()
        .AddEnvironmentVariables(Constants.EnvironmentVariablePrefix);

    var app = builder.Build();

    // First in the pipeline so it catches exceptions thrown by any later middleware.
    // Combined with AddProblemDetails(), unhandled exceptions are logged at Error and the
    // client gets an RFC 7807 ProblemDetails response (with stack trace details only in
    // Development).
    app.UseExceptionHandler();

    // Match the previous code's permissive trust: any upstream proxy's X-Forwarded-For is
    // honored. Tightening this (KnownNetworks for a known proxy subnet) is a separate
    // security concern.
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor
    };
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);

    // /dashboard-services hits every RefreshInterval per provider host; even one Info per
    // tick stacks up. The operator-facing "manager did real work" signal is the Info from
    // DashboardCommandProcessor (only on actual change).
    app.UseSerilogRequestLogging(opts => opts.GetLevel = (_, _, _) => LogEventLevel.Debug);

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    var managerOptions = app.Services.GetRequiredService<IOptions<ManagerOptions>>().Value;
    app.Logger.LogInformation("Manager starting with {Count} dashboards [{Dashboards}]",
        managerOptions.DashboardManagers.Count,
        string.Join(", ", managerOptions.DashboardManagers.Select(m => m.DashboardManagerType)));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
