using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Dsm.Providers.ServicesProviders.Traefik;
using Dsm.Shared.Models;
using Dsm.Shared.Options;

namespace Dsm.Providers.Services;
public class ContainerLabelServiceFactory
{
    private static readonly Regex LabelKeyTraefikRouterRuleRegex = new Regex(@"^traefik\.http\.routers\.([^.]+)\.rule");

    private readonly ILogger<ContainerLabelServiceFactory> _logger;

    public ContainerLabelServiceFactory(ILogger<ContainerLabelServiceFactory> logger)
    {
        _logger = logger;
    }

    public Service CreateFromLabels(ServicesProviderConfig config, string? hostname, string name, IDictionary<string, string> labels)
    {
        var dockerLabelPrefix = config.DockerLabelPrefix;
        var fromProviderServiceBuilder = new FromProviderServiceBuilder(name, hostname, config.AreServiceHostsHttps);

        foreach (var label in labels)
        {
            var traefikRouterRuleRegexMatch = LabelKeyTraefikRouterRuleRegex.Match(label.Key);
            if (traefikRouterRuleRegexMatch.Success &&
                !string.IsNullOrEmpty(traefikRouterRuleRegexMatch.Groups[1].Value))
            {
                var traefikRouter = traefikRouterRuleRegexMatch.Groups[1].Value;
                fromProviderServiceBuilder.TraefikRouterNameToRule.Add(traefikRouter, label.Value);
            }
            else if (label.Key == $"{dockerLabelPrefix}.category")
            {
                fromProviderServiceBuilder.LabelCategory = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.icon")
            {
                fromProviderServiceBuilder.LabelIcon = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.image_path")
            {
                fromProviderServiceBuilder.LabelImagePath = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.ignore")
            {
                if (bool.TryParse(label.Value, out var ignore))
                {
                    fromProviderServiceBuilder.LabelIgnore = ignore;
                }
                else
                {
                    _logger.LogWarning("Ignoring unparseable '{Key}' label value: '{Value}'", label.Key, label.Value);
                }
            }
            else if (label.Key == $"{dockerLabelPrefix}.name")
            {
                fromProviderServiceBuilder.LabelName = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.traefik.router")
            {
                fromProviderServiceBuilder.LabelTraefikRouter = label.Value;
            }
            else if (label.Key == $"{dockerLabelPrefix}.url")
            {
                fromProviderServiceBuilder.LabelUrl = label.Value;
            }
        }
        var service = fromProviderServiceBuilder.Build();
        _logger.LogDebug("{Service}", service);
        return service;
    }

    private class FromProviderServiceBuilder
    {
        public string DockerName { get; set; }
        public string? Hostname { get; set; }
        public string? LabelCategory { get; set; }
        public string? LabelIcon { get; set; }
        public string? LabelImagePath { get; set; }
        public bool   LabelIgnore { get; set; } = false;
        public string? LabelName { get; set; }
        public string? LabelUrl { get; set; }
        public string? LabelTraefikRouter { get; set; }
        public Dictionary<string, string> TraefikRouterNameToRule { get; set; } =
            new Dictionary<string, string>();

        private bool _areTraefikRulesHttps;

        public FromProviderServiceBuilder(string dockerName, string? hostname, bool areTraefikRulesHttps)
        {
            DockerName = dockerName;
            Hostname = hostname;
            _areTraefikRulesHttps = areTraefikRulesHttps;
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
                LabelIgnore
            );
        }

        private string? GetUrl()
        {
            if (!string.IsNullOrEmpty(LabelUrl))
            {
                return LabelUrl;
            }

            string? traefikRouterRule = null;

            if (TraefikRouterNameToRule.Any())
            {
                if (!string.IsNullOrEmpty(LabelTraefikRouter) &&
                    TraefikRouterNameToRule.TryGetValue(LabelTraefikRouter, out traefikRouterRule))
                {
                }
                else
                {
                    (_, traefikRouterRule) = TraefikRouterNameToRule.FirstOrDefault();
                }
            }

            var host = TraefikRuleParser.ExtractFirstHost(traefikRouterRule);
            return host is null ? null : TraefikRuleParser.BuildUrl(host, _areTraefikRulesHttps);
        }
    }
}
