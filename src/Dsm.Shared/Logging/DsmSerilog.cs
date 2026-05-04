using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Dsm.Shared.Logging;

public static class DsmSerilog
{
    public const string StandardOutputTemplate =
        "{Timestamp:o} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
    public const string VerboseOutputTemplate =
        "{Timestamp:o} [{Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}";

    public static void InitializeBootstrapLogger() =>
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: StandardOutputTemplate)
            .CreateBootstrapLogger();

    public static LoggerConfiguration ConfigureDsmDefaults(
        this LoggerConfiguration lc,
        IConfiguration configuration,
        IServiceProvider services) =>
        lc.ReadFrom.Configuration(configuration)
          .ReadFrom.Services(services)
          .WriteTo.Logger(sub => sub
              .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Verbose)
              .WriteTo.Console(outputTemplate: VerboseOutputTemplate))
          .WriteTo.Logger(sub => sub
              .Filter.ByExcluding(e => e.Level == LogEventLevel.Verbose)
              .WriteTo.Console(outputTemplate: StandardOutputTemplate));
}
