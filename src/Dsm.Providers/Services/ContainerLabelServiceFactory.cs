using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Dsm.Providers.ServicesProviders.Traefik;
using Dsm.Shared.Models;
using Dsm.Providers.Options;

namespace Dsm.Providers.Services;
public class ContainerLabelServiceFactory
{
    private static readonly Regex LabelKeyTraefikRouterRuleRegex = new Regex(@"^traefik\.http\.routers\.([^.]+)\.rule");

    private readonly ILogger<ContainerLabelServiceFactory> _logger;

    public ContainerLabelServiceFactory(ILogger<ContainerLabelServiceFactory> logger)
    {
        _logger = logger;
    }

    public Service CreateFromLabels(ServicesProviderConfig config, string? hostname, string name, IDictionary<string, string> labels, string? providerId = null)
    {
        var dockerLabelPrefix = config.DockerLabelPrefix;
        var state = new LabelParserState(name, hostname, config.AreServiceHostsHttps, providerId, dockerLabelPrefix, _logger);

        foreach (var label in labels)
        {
            var traefikRouterRuleRegexMatch = LabelKeyTraefikRouterRuleRegex.Match(label.Key);
            if (traefikRouterRuleRegexMatch.Success &&
                !string.IsNullOrEmpty(traefikRouterRuleRegexMatch.Groups[1].Value))
            {
                var traefikRouter = traefikRouterRuleRegexMatch.Groups[1].Value;
                state.TraefikRouterNameToRule.Add(traefikRouter, label.Value);
            }
            else if (label.Key == $"{dockerLabelPrefix}.category")
            {
                state.LabelCategory = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.icon")
            {
                state.LabelIcon = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.image_path")
            {
                state.LabelImagePath = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.ignore")
            {
                if (bool.TryParse(label.Value, out var ignore))
                {
                    state.LabelIgnore = ignore;
                }
                else
                {
                    _logger.LogWarning("Ignoring unparseable '{Key}' label value: '{Value}'", label.Key, label.Value);
                }
            }
            else if (label.Key == $"{dockerLabelPrefix}.name")
            {
                state.LabelName = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.service_defaults_name")
            {
                state.LabelServiceDefaultsName = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.traefik.router")
            {
                state.LabelTraefikRouter = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.url")
            {
                state.LabelUrl = label.Value;
            }
        }
        var service = state.Build();
        _logger.LogDebug("Created service from container labels: {@Service}", service);
        return service;
    }

    private class LabelParserState
    {
        public string DockerName { get; set; }
        public string? Hostname { get; set; }
        public string? LabelCategory { get; set; }
        public string? LabelIcon { get; set; }
        public string? LabelImagePath { get; set; }
        public bool   LabelIgnore { get; set; } = false;
        public string? LabelName { get; set; }
        public string? LabelServiceDefaultsName { get; set; }
        public string? LabelUrl { get; set; }
        public string? LabelTraefikRouter { get; set; }
        public Dictionary<string, string> TraefikRouterNameToRule { get; set; } =
            new Dictionary<string, string>();

        private bool _areTraefikRulesHttps;
        private string? _providerId;
        private readonly string? _dockerLabelPrefix;
        private readonly ILogger _logger;

        public LabelParserState(
            string dockerName,
            string? hostname,
            bool areTraefikRulesHttps,
            string? providerId,
            string? dockerLabelPrefix,
            ILogger logger)
        {
            DockerName = dockerName;
            Hostname = hostname;
            _areTraefikRulesHttps = areTraefikRulesHttps;
            _providerId = providerId;
            _dockerLabelPrefix = dockerLabelPrefix;
            _logger = logger;
        }

        public Service Build()
        {
            return new Service(
                LabelName ?? DockerName,
                GetUrl(),
                LabelCategory,
                LabelIcon,
                LabelImagePath,
                Hostname,
                LabelIgnore,
                serviceDefaultsName: LabelServiceDefaultsName,
                providerId: _providerId
            );
        }

        private string? GetUrl()
        {
            if (!string.IsNullOrEmpty(LabelUrl))
            {
                return LabelUrl;
            }

            var traefikRouterRule = SelectTraefikRouterRule();

            var host = TraefikRuleParser.ExtractFirstHost(traefikRouterRule);
            return host is null ? null : TraefikRuleParser.BuildUrl(host, _areTraefikRulesHttps);
        }

        // Picks which router's rule becomes the service URL when a container
        // declares more than one.
        //
        // This used to be TraefikRouterNameToRule.FirstOrDefault(). Dictionary
        // enumeration follows insertion order, and insertion order comes from the
        // Docker API's label map, which is not stable between responses — so a
        // two-router container advertised a different URL from one poll to the
        // next, and anything reconciling against it (DNS rewrites, dashboards)
        // flapped. Selection must depend only on the labels, never on their order.
        private string? SelectTraefikRouterRule()
        {
            if (TraefikRouterNameToRule.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(LabelTraefikRouter))
            {
                if (TraefikRouterNameToRule.TryGetValue(LabelTraefikRouter, out var requested))
                {
                    return requested;
                }

                _logger.LogWarning(
                    "Container '{Container}' requests traefik router '{Requested}' via '{Label}', but its routers are {Routers}; falling back to automatic selection",
                    DockerName,
                    LabelTraefikRouter,
                    $"{_dockerLabelPrefix}.traefik.router",
                    string.Join(", ", TraefikRouterNameToRule.Keys.OrderBy(k => k, StringComparer.Ordinal)));
            }

            // One router is unambiguous — nothing to choose or explain.
            if (TraefikRouterNameToRule.Count == 1)
            {
                return TraefikRouterNameToRule.Values.First();
            }

            var routerNames = TraefikRouterNameToRule.Keys
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            // Prefer the router named after the container — the convention every
            // multi-router container in practice follows — then a stable sort.
            var selected = TraefikRouterNameToRule.ContainsKey(DockerName)
                ? DockerName
                : routerNames[0];

            _logger.LogWarning(
                "Container '{Container}' has {Count} traefik routers ({Routers}) and no '{Label}' label; selected '{Selected}'. Set that label to choose explicitly.",
                DockerName,
                routerNames.Count,
                string.Join(", ", routerNames),
                $"{_dockerLabelPrefix}.traefik.router",
                selected);

            return TraefikRouterNameToRule[selected];
        }
    }
}
