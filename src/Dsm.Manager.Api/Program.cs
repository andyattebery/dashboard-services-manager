using AspNetCore.Authentication.ApiKey;
using Dsm.Manager.Api.Authentication;
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

    builder.Configuration
        .AddDsmManagerConfiguration()
        .AddEnvironmentVariables(Constants.EnvironmentVariablePrefix);

    builder.Services.AddControllers();
    builder.Services.AddHealthChecks();
    builder.Services
        .AddOpenApi()
        .AddProblemDetails()
        .AddDsmManagerServices();

    // Opt-in API key auth. Scheme + authorization are registered only when ApiKey is set,
    // so the trusted-LAN default deployment doesn't carry the auth pipeline at all.
    var apiKeyConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["ManagerOptions:ApiKey"]);
    if (apiKeyConfigured)
    {
        builder.Services
            .AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
            .AddApiKeyInHeader<ConfigApiKeyProvider>(opts =>
            {
                opts.Realm = "DSM";
                opts.KeyName = Constants.ApiKeyHeaderName;
            });
        builder.Services.AddAuthorization();
    }

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

    if (apiKeyConfigured)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    var controllerEndpoints = app.MapControllers();
    if (apiKeyConfigured)
    {
        controllerEndpoints.RequireAuthorization();
    }
    app.MapHealthChecks("/health");

    var managerOptions = app.Services.GetRequiredService<IOptions<ManagerOptions>>().Value;
    app.Logger.LogInformation("Manager starting with {Count} dashboards [{Dashboards}], API key {ApiKeyState}",
        managerOptions.DashboardManagers.Count,
        string.Join(", ", managerOptions.DashboardManagers.Select(m => m.DashboardManagerType)),
        apiKeyConfigured ? "enabled" : "disabled");

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
