using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dsm.Shared.Tests;

public static class TestHost
{
    public static IHost Create(
        Action<IConfigurationBuilder>? configureConfiguration = null,
        Action<IServiceCollection>? configureServices = null)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, b) => configureConfiguration?.Invoke(b))
            .ConfigureServices((_, s) => configureServices?.Invoke(s))
            .Build();
    }
}
