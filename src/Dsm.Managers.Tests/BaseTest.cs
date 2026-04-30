using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Managers;
using Dsm.Managers.HostBuilder;
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
        services.AddDsmManagerServices();
    }

    protected virtual void ConfigureConfiguration(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddDsmManagerConfiguration();
    }
}