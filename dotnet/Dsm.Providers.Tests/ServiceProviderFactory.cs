using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dsm.Providers.Tests;
public static class ServiceProviderFactory
{
    public static IServiceProvider Create(
        Action<IConfiguration> configureConfiguration,
        Action<IConfiguration, IServiceCollection> addServices
    )
    {
        var configuration = new ConfigurationBuilder()
            // .SetBasePath(Directory.GetCurrentDirectory())
            // .AddJsonFile("appsettings.json", false)
            .Build();
        configureConfiguration(configuration);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging((logging) =>
        {
            logging.AddConsole();
        });
        ServiceCollectionConfiguration.AddServices(configuration, serviceCollection);
        addServices(configuration, serviceCollection);
        return serviceCollection.BuildServiceProvider();
    }
}