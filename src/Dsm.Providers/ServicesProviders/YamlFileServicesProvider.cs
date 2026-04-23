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
            throw new FileNotFoundException($"{path} does not exist.");
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        using (var reader = File.OpenText(path))
        {
            return await Task.Run<List<Service>>(() => {
                var services = deserializer.Deserialize<List<Service>>(reader);
                return services;
            });
        }
    }
}
