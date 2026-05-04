using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dsm.Provider.App;
using Dsm.Providers.HostBuilder;
using Dsm.Shared.Configuration;
using Dsm.Shared.Logging;
using Serilog;

DsmSerilog.InitializeBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ConfigureDsmDefaults(builder.Configuration, services));

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
