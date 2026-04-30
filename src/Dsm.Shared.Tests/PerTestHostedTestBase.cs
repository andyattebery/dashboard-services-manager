using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Dsm.Shared.Tests;

public abstract class PerTestHostedTestBase
{
    private readonly List<IDisposable> _hosts = new();

    [TearDown]
    public void DisposeTrackedHosts()
    {
        foreach (var h in _hosts.AsEnumerable().Reverse()) h.Dispose();
        _hosts.Clear();
    }

    /// <summary>
    /// Build an <see cref="IHost"/> whose lifetime is bound to the current test.
    /// The host is disposed in <c>[TearDown]</c>.
    /// </summary>
    protected IHost CreateHost(
        Action<IConfigurationBuilder>? configureConfiguration = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var host = TestHost.Create(configureConfiguration, configureServices);
        _hosts.Add(host);
        return host;
    }
}
