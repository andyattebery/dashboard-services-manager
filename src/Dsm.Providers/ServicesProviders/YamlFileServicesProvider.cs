using Microsoft.Extensions.Logging;
using Dsm.Shared.Models;
using YamlDotNet.Serialization;
using Dsm.Shared.Options;
using YamlDotNet.Serialization.NamingConventions;

namespace Dsm.Providers.ServicesProviders;
public class YamlFileServicesProvider : IServicesProvider
{
    private readonly ILogger<YamlFileServicesProvider> _logger;
    private readonly ServicesProviderConfig _config;

    public YamlFileServicesProvider(
        ILogger<YamlFileServicesProvider> logger,
        ServicesProviderConfig config
    )
    {
        if (string.IsNullOrWhiteSpace(config.ServicesYamlFilePath))
        {
            throw new InvalidOperationException($"{nameof(ServicesProviderConfig)}.{nameof(ServicesProviderConfig.ServicesYamlFilePath)} must be set for a YamlFile provider.");
        }
        _logger = logger;
        _config = config;
    }

    public async Task<List<Service>> ListServices()
    {
        var path = _config.ServicesYamlFilePath!;
        if (!File.Exists(path))
        {
            _logger.LogWarning("YAML services file '{Path}' does not exist; returning no services.", path);
            return new List<Service>();
        }

        var text = await File.ReadAllTextAsync(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        return deserializer.Deserialize<List<Service>>(text) ?? new List<Service>();
    }
}
