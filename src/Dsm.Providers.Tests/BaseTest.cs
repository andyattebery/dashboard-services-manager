using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers.HostBuilder;
using Dsm.Shared.Tests;

namespace Dsm.Providers.Tests;
public class BaseTest
{
    protected IServiceProvider ServiceProvider = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        ServiceProvider = ServiceProviderFactory.Create(ConfigureConfiguration, AddServices);
    }

    protected virtual void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        services.AddDsmProviderServices();
    }

    protected virtual void ConfigureConfiguration(IConfigurationBuilder configurationBuilder)
    {
    }
}