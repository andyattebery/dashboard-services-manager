using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dsm.Provider.App;
using Dsm.Providers.HostBuilder;
using Dsm.Shared.Configuration;
using Serilog;

const string OutputTemplate =
    "{Timestamp:o} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

// Bootstrap logger captures startup logs before host config is built; replaced by AddSerilog below.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: OutputTemplate)
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console(outputTemplate: OutputTemplate));

    builder.Services
        .AddHostedService<ProviderService>()
        .AddDsmProviderServices();

    builder.Configuration
        .AddDsmProviderConfiguration()
        .AddEnvironmentVariables(Constants.EnvironmentVariablePrefix);

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
