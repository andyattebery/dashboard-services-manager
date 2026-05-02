using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dsm.Provider.App;
using Dsm.Providers.HostBuilder;
using Dsm.Shared.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddHostedService<ProviderService>()
    .AddDsmProviderServices();

builder.Configuration
    .AddDsmProviderConfiguration()
    .AddEnvironmentVariables(Constants.EnvironmentVariablePrefix);

var host = builder.Build();
host.Run();
