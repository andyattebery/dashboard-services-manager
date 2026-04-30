using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Dsm.Shared.Tests;

public abstract class TestFixtureHostedTestBase
{
    private IHost? _host;

    protected IServiceProvider ServiceProvider = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        _host = TestHost.Create(ConfigureConfiguration, AddServices);
        ServiceProvider = _host.Services;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _host?.Dispose();

    protected virtual void AddServices(IServiceCollection services) { }

    protected virtual void ConfigureConfiguration(IConfigurationBuilder configurationBuilder) { }
}
