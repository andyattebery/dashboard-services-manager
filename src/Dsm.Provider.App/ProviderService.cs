using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refit;
using Dsm.Providers.ServicesProviders;
using Dsm.Providers.ApiClients;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;
using Dsm.Providers.Options;

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
        if (_providerOptions.ServicesProviders.Count == 0)
        {
            _logger.LogError("No provider configured — set ProviderOptions.ServicesProviders. Shutting down.");
            return;
        }

        _logger.LogInformation(
            "Provider starting with {Count} sources [{Providers}], refresh interval {Interval}",
            _providerOptions.ServicesProviders.Count,
            string.Join(", ", _providerOptions.ServicesProviders.Select(p => p.ServicesProviderType)),
            _providerOptions.RefreshInterval);

        using var timer = new PeriodicTimer(_providerOptions.RefreshInterval);
        do
        {
            var aggregated = new List<Service>();
            foreach (var config in _providerOptions.ServicesProviders)
            {
                try
                {
                    var provider = _servicesProviderFactory.Create(config);
                    var services = await provider.ListServices();
                    _logger.LogDebug("Provider '{ProviderType}' returned {Count} services: {@Services}",
                        config.ServicesProviderType, services.Count, services);
                    aggregated.AddRange(services);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Provider '{ProviderType}' failed this cycle; continuing.", config.ServicesProviderType);
                }
            }

            if (aggregated.Count > 0)
            {
                await PostServices(aggregated);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PostServices(List<Service> services)
    {
        try
        {
            _logger.LogDebug("POST /dashboard-services payload: {@Services}", services);
            var response = await _dcmClient.UpdateDashboard(services);
            foreach (var (managerName, entries) in response)
            {
                _logger.LogDebug("Dashboard '{DashboardManager}' response: {Count} services {@Services}",
                    managerName, entries.Count, entries);
            }
        }
        catch (ApiException e)
        {
            _logger.LogError(e, "POST /dashboard-services failed with HTTP {StatusCode}: {Content}",
                (int)e.StatusCode, e.Content);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "POST /dashboard-services failed (transport)");
        }
        catch (TaskCanceledException e)
        {
            _logger.LogError(e, "POST /dashboard-services timed out");
        }
    }
}
