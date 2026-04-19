using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dsm.Shared.Tests;
public static class ServiceProviderFactory
{
    public static IServiceProvider Create(
        Action<IConfigurationBuilder> configureConfiguration,
        Action<IConfiguration, IServiceCollection> addServices)
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                configureConfiguration(configurationBuilder);
            })
            .ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                addServices(hostBuilderContext.Configuration, serviceCollection);
            });
        var host = hostBuilder.Build();
        return host.Services;
        // var configurationBuilder = new ConfigurationBuilder()
        //     .SetBasePath(Directory.GetCurrentDirectory())
        //     // .AddJsonFile("appsettings.json", false)
        //     ;
        // configureConfiguration(configurationBuilder);
        //
        // var configuration = configurationBuilder.Build();
        //
        // var serviceCollection = new ServiceCollection();
        //
        // serviceCollection.AddLogging((logging) =>
        // {
        //     logging.AddConsole();
        // });

        // addServices(configuration, serviceCollection);
        // return serviceCollection.BuildServiceProvider();
    }
}