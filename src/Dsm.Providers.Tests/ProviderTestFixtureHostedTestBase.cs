using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers.HostBuilder;

namespace Dsm.Providers.Tests;

public abstract class ProviderTestFixtureHostedTestBase : TestFixtureHostedTestBase
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddDsmProviderServices();
    }
}
