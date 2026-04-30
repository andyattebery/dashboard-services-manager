using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Managers.HostBuilder;

namespace Dsm.Managers.Tests;

public abstract class ManagerTestFixtureHostedTestBase : TestFixtureHostedTestBase
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddDsmManagerServices();
    }

    protected override void ConfigureConfiguration(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddDsmManagerConfiguration();
    }
}
