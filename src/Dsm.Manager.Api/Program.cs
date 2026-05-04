using Dsm.Managers.Configuration;
using Dsm.Managers.HostBuilder;
using Dsm.Shared.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

const string OutputTemplate =
    "{Timestamp:o} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

// Bootstrap logger captures startup logs before host config is built; replaced by AddSerilog below.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: OutputTemplate)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console(outputTemplate: OutputTemplate));

    builder.Services.AddControllers();
    builder.Services
        .AddOpenApi()
        .AddDsmManagerServices();

    builder.Configuration
        .AddDsmManagerConfiguration()
        .AddEnvironmentVariables(Constants.EnvironmentVariablePrefix);

    var app = builder.Build();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} from {ClientIp} '{UserAgent}' responded {StatusCode} in {Elapsed:0.0} ms ({ServiceCount} services)";

        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            var forwardedFor = ctx.Request.Headers["X-Forwarded-For"].ToString();
            var clientIp = string.IsNullOrEmpty(forwardedFor)
                ? ctx.Connection.RemoteIpAddress?.ToString()
                : forwardedFor;

            diag.Set("ClientIp", clientIp);
            diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
        };

        // /dashboard-services hits every RefreshInterval per provider host; even one Info per
        // tick stacks up. The operator-facing "manager did real work" signal is the Info from
        // DashboardCommandProcessor (only on actual change).
        opts.GetLevel = (_, _, _) => LogEventLevel.Debug;
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseAuthorization();
    app.MapControllers();

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
