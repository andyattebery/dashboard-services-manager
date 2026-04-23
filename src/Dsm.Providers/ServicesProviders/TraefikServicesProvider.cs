using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dsm.Providers.ServicesProviders.Traefik;
using Dsm.Shared.Models;
using Dsm.Shared.Options;

namespace Dsm.Providers.ServicesProviders;

public class TraefikServicesProvider : IServicesProvider
{
    private const string EnabledStatus = "enabled";
    private const string InternalProviderSuffix = "@internal";
    private const string DockerComposeSuffix = "-docker-compose";
    private const string TraefikApiService = "api@internal";
    private const string TraefikServiceName = "traefik";

    private readonly ILogger<TraefikServicesProvider> _logger;
    private readonly ITraefikApiClient _traefikApiClient;
    private readonly ProviderOptions _providerOptions;
    private readonly ServicesProviderConfig _config;

    public TraefikServicesProvider(
        ILogger<TraefikServicesProvider> logger,
        IOptions<ProviderOptions> providerOptions,
        ITraefikApiClientFactory traefikApiClientFactory,
        ServicesProviderConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.TraefikApiUrl))
        {
            throw new InvalidOperationException($"{nameof(ServicesProviderConfig)}.{nameof(ServicesProviderConfig.TraefikApiUrl)} must be set for a Traefik provider.");
        }
        _logger = logger;
        _providerOptions = providerOptions.Value;
        _config = config;
        _traefikApiClient = traefikApiClientFactory.Create(config.TraefikApiUrl);
    }

    public async Task<List<Service>> ListServices()
    {
        var routers = await _traefikApiClient.GetRouters();
        return routers
            .Where(IsIncluded)
            .Select(TryBuildService)
            .OfType<Service>()
            .ToList();
    }

    private static bool IsIncluded(TraefikRouter router)
    {
        if (router.Status != EnabledStatus)
        {
            return false;
        }
        if (router.Name?.EndsWith(InternalProviderSuffix, StringComparison.Ordinal) ?? true)
        {
            return false;
        }
        return true;
    }

    private Service? TryBuildService(TraefikRouter router)
    {
        var host = TraefikRuleParser.ExtractFirstHost(router.Rule);
        if (host is null)
        {
            _logger.LogDebug("Skipping router '{Name}': no Host(...) clause in rule '{Rule}'", router.Name, router.Rule);
            return null;
        }

        var serviceName = string.Equals(router.Service, TraefikApiService, StringComparison.Ordinal)
            ? TraefikServiceName
            : CleanServiceName(router.Service ?? router.Name);
        if (string.IsNullOrEmpty(serviceName))
        {
            _logger.LogDebug("Skipping router '{Name}': no service or name", router.Name);
            return null;
        }

        var url = TraefikRuleParser.BuildUrl(host, _config.AreServiceHostsHttps);

        return new Service(
            name: serviceName,
            url: url,
            category: null,
            icon: null,
            imageUrl: null,
            hostname: _providerOptions.Hostname,
            ignore: false);
    }

    private static string CleanServiceName(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        var name = raw.Split('@')[0];
        if (name.EndsWith(DockerComposeSuffix, StringComparison.Ordinal))
        {
            name = name[..^DockerComposeSuffix.Length];
        }
        return ServicesProviderUtilities.GetFormattedServiceName(name);
    }
}
