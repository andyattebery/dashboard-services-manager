using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Dsm.Shared.Options;

public sealed class ProviderOptions
{
    [Required(AllowEmptyStrings = false)]
    public required string ApiUrl { get; set; }

    [Required(AllowEmptyStrings = false)]
    public required string Hostname { get; set; }

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(60);

    [MinLength(1)]
    public required List<ServicesProviderConfig> ServicesProviders { get; set; }
}

public sealed class ServicesProviderConfig
{
    [EnumDataType(typeof(ServicesProviderType))]
    public required ServicesProviderType ServicesProviderType { get; set; }

    public bool AreServiceHostsHttps { get; set; }

    public string? TraefikApiUrl { get; set; }
    public string? ServicesYamlFilePath { get; set; }
    public string? DockerLabelPrefix { get; set; }
}

public sealed class ProviderOptionsValidator : IValidateOptions<ProviderOptions>
{
    public ValidateOptionsResult Validate(string? name, ProviderOptions options)
    {
        var failures = new List<string>();
        foreach (var (config, i) in options.ServicesProviders.Select((c, i) => (c, i)))
        {
            switch (config.ServicesProviderType)
            {
                case ServicesProviderType.Traefik:
                    if (string.IsNullOrWhiteSpace(config.TraefikApiUrl))
                        failures.Add($"ServicesProviders[{i}] (Traefik): TraefikApiUrl is required.");
                    break;
                case ServicesProviderType.YamlFile:
                    if (string.IsNullOrWhiteSpace(config.ServicesYamlFilePath))
                        failures.Add($"ServicesProviders[{i}] (YamlFile): ServicesYamlFilePath is required.");
                    break;
                case ServicesProviderType.Docker:
                case ServicesProviderType.Swarm:
                    if (string.IsNullOrWhiteSpace(config.DockerLabelPrefix))
                        failures.Add($"ServicesProviders[{i}] ({config.ServicesProviderType}): DockerLabelPrefix is required.");
                    break;
            }
        }
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
