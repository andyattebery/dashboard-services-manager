using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Managers;
using Dsm.Managers.Hosting;
using Dsm.Shared.Tests;

namespace Dsm.Managers.Tests;
public class BaseTest
{
    protected IServiceProvider ServiceProvider;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        ServiceProvider = ServiceProviderFactory.Create(ConfigureConfiguration, AddServices);
    }

    protected virtual void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        HostBuilderConfiguration.AddServices(configuration, services);
    }

    protected virtual void ConfigureConfiguration(IConfigurationBuilder configurationBuilder)
    {
        HostBuilderConfiguration.ConfigureConfiguration(configurationBuilder);
    }
}