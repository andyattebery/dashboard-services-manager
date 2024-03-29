using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers;

namespace Dsm.Providers.Tests;
public class BaseTest
{
    protected IServiceProvider ServiceProvider;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        ServiceProvider = ServiceProviderFactory.Create(ConfigureConfiguration, AddServices);
    }

    protected virtual void AddServices(IConfiguration configuration, IServiceCollection services)
    {
    }

    protected virtual void ConfigureConfiguration(IConfiguration configuration)
    {
    }
}