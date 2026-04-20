using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refit;
using Dsm.Providers.ServicesProviders;
using Dsm.Shared.ApiClients;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;
using Dsm.Shared.Options;

namespace Dsm.Provider.App;

public class ProviderService : BackgroundService
{
    private readonly ILogger<ProviderService> _logger;
    private readonly ProviderOptions _providerOptions;
    private readonly ServicesProviderFactory _servicesProviderFactory;
    private readonly IDcmClient _dcmClient;

    public ProviderService(ILogger<ProviderService> logger, IOptions<ProviderOptions> providerOptions, ServicesProviderFactory servicesProviderFactory, IDcmClient dcmClient)
    {
        _logger = logger;
        _providerOptions = providerOptions.Value;
        _servicesProviderFactory = servicesProviderFactory;
        _dcmClient = dcmClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var providerTypes = ResolveProviderTypes();
        if (providerTypes.Count == 0)
        {
            _logger.LogError("No provider configured — set ProviderOptions.ServicesProviderTypes. Shutting down.");
            return;
        }

        using var timer = new PeriodicTimer(_providerOptions.RefreshInterval);
        do
        {
            var aggregated = new List<Service>();
            foreach (var providerType in providerTypes)
            {
                try
                {
                    var provider = _servicesProviderFactory.Create(providerType);
                    aggregated.AddRange(await provider.ListServices());
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Provider '{ProviderType}' failed this cycle; continuing.", providerType);
                }
            }

            if (aggregated.Count > 0)
            {
                await PostServices(aggregated);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private List<string> ResolveProviderTypes()
    {
        if (_providerOptions.ServicesProviderTypes is { Count: > 0 })
        {
            return _providerOptions.ServicesProviderTypes;
        }

#pragma warning disable CS0618 // legacy singular option; fallback for one release
        var legacy = _providerOptions.ServicesProviderType;
#pragma warning restore CS0618
        return string.IsNullOrWhiteSpace(legacy) ? new List<string>() : new List<string> { legacy };
    }

    private async Task PostServices(List<Service> services)
    {
        try
        {
            var response = await _dcmClient.UpdateDashboard(services);
            foreach (var (managerName, entries) in response)
            {
                _logger.LogDebug("{Manager}: {Count} services", managerName, entries.Count);
                foreach (var service in entries)
                {
                    _logger.LogDebug("  {Service}", service);
                }
            }
        }
        catch (ApiException e)
        {
            _logger.LogError(e, "{Exception}: Content={Content}", nameof(ApiException), e.Content);
        }
    }
}
