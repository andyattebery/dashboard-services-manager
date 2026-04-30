using Microsoft.Extensions.Options;

namespace Dsm.Providers.Options;

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
                    if (string.IsNullOrWhiteSpace(config.Hostname))
                        failures.Add($"ServicesProviders[{i}] (Traefik): Hostname is required.");
                    break;
                case ServicesProviderType.YamlFile:
                    if (string.IsNullOrWhiteSpace(config.ServicesYamlFilePath))
                        failures.Add($"ServicesProviders[{i}] (YamlFile): ServicesYamlFilePath is required.");
                    break;
                case ServicesProviderType.Docker:
                case ServicesProviderType.Swarm:
                    if (string.IsNullOrWhiteSpace(config.DockerLabelPrefix))
                        failures.Add($"ServicesProviders[{i}] ({config.ServicesProviderType}): DockerLabelPrefix is required.");
                    if (string.IsNullOrWhiteSpace(config.Hostname))
                        failures.Add($"ServicesProviders[{i}] ({config.ServicesProviderType}): Hostname is required.");
                    break;
            }
        }
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
