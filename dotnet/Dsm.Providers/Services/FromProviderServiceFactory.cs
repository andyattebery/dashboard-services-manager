using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dsm.Shared.Models;
using Dsm.Shared.Options;

namespace Dsm.Providers.Services;
public class FromProviderServiceFactory
{
    private static readonly Regex LabelKeyTraefikRouterRuleRegex = new Regex(@"^traefik\.http\.routers\.(.*)\.rule");

    private string DockerLabelPrefix => _providerOptions.DockerLabelPrefix;

    private readonly ILogger<FromProviderServiceFactory> _logger;
    private readonly ProviderOptions _providerOptions;

    public FromProviderServiceFactory(ILogger<FromProviderServiceFactory> logger,
        IOptions<ProviderOptions> providerOptions)
    {
        _logger = logger;
        _providerOptions = providerOptions.Value;
    }

    public Service CreateFromLabels(string name, IDictionary<string, string> labels)
    {
        var fromProviderServiceBuilder = new FromProviderServiceBuilder(name, _providerOptions.Hostname, _providerOptions.AreServiceHostsHttps);

        foreach (var label in labels)
        {
            _logger.LogDebug($"{label.Key}, {label.Value}");

            var traefikRouterRuleRegexMatch = LabelKeyTraefikRouterRuleRegex.Match(label.Key);
            if (traefikRouterRuleRegexMatch.Success)
            {
                fromProviderServiceBuilder.TraefikRouterNameToRule.Add(traefikRouterRuleRegexMatch.Value, label.Value);
            }
            else if (label.Key == $"{DockerLabelPrefix}.category")
            {
                fromProviderServiceBuilder.LabelCategory = label.Value;
            }
            else if (label.Key == $"{DockerLabelPrefix}.icon")
            {
                fromProviderServiceBuilder.LabelIcon = label.Value;
            }
            else if (label.Key == $"{DockerLabelPrefix}.image_path")
            {
                fromProviderServiceBuilder.LabelImagePath = label.Value;
            }
            else if (label.Key == $"{DockerLabelPrefix}.ignore" &&
                     bool.Parse(label.Value))
            {
                fromProviderServiceBuilder.LabelIgnore = true;
            }
            else if (label.Key == $"{DockerLabelPrefix}.name")
            {
                fromProviderServiceBuilder.LabelName = label.Value;
            }
            else if (label.Key == $"{DockerLabelPrefix}.traefik.router")
            {
                fromProviderServiceBuilder.LabelTraefikRouter = label.Value;
            }
            else if (label.Key == $"{DockerLabelPrefix}.url")
            {
                fromProviderServiceBuilder.LabelUrl = label.Value;
            }
        }
        var service = fromProviderServiceBuilder.Build();
        _logger.LogDebug(service.ToString());
        return service;
    }

    private class FromProviderServiceBuilder
    {
        public string DockerName { get; set; }
        public string Hostname { get; set; }
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

        private static readonly Regex LabelValueTraefikRulesUrlRegex = new Regex(@"^Host\((.+)\)");

        public FromProviderServiceBuilder(string dockerName, string hostname, bool areTraefikRulesHttps)
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

            if (string.IsNullOrEmpty(traefikRouterRule))
            {
                return null;
            }

            var ruleRegexMatch = LabelValueTraefikRulesUrlRegex.Match(traefikRouterRule);
            if (ruleRegexMatch.Success && !string.IsNullOrEmpty(ruleRegexMatch.Groups[1].Value))
            {
                var firstRuleUrl = ruleRegexMatch.Groups[1].Value.Replace("`", "").Split(",").FirstOrDefault();
                if (!string.IsNullOrEmpty(firstRuleUrl))
                {
                    return _areTraefikRulesHttps ?
                        $"https://{firstRuleUrl}" :
                        $"http://{firstRuleUrl}";
                }
            }

            return null;
        }
    }
}