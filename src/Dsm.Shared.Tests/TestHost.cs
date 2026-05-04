using Dsm.Shared.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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
            .UseSerilog((ctx, services, lc) => lc.ConfigureDsmDefaults(ctx.Configuration, services))
            .Build();
    }
}
