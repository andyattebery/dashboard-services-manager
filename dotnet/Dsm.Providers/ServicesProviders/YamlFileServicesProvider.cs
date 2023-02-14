using System.Linq;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Dsm.Shared.Models;
using Dsm.Providers.Services;
using YamlDotNet.Serialization;
using Dsm.Shared.Options;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization.NamingConventions;

namespace Dsm.Providers.ServicesProviders;
public class YamlFileServicesProvider : IServicesProvider
{
    private readonly ILogger<YamlFileServicesProvider> _logger;
    private readonly FromProviderServiceFactory _fromProviderServiceFactory;
    private readonly ProviderOptions _providerOptions;

    public YamlFileServicesProvider(
        ILogger<YamlFileServicesProvider> logger,
        FromProviderServiceFactory fromProviderServiceFactory,
        IOptions<ProviderOptions> providerOptions
    )
    {
        _logger = logger;
        _fromProviderServiceFactory = fromProviderServiceFactory;
        _providerOptions = providerOptions.Value;
    }

    public async Task<List<Service>> ListServices()
    {
        if (!File.Exists(_providerOptions.ServicesYamlFilePath))
        {
            throw new Exception($"{_providerOptions.ServicesYamlFilePath} does not exist.");
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        using (var reader = File.OpenText(_providerOptions.ServicesYamlFilePath))
        {
            return await Task.Run<List<Service>>(() => {
                var services = deserializer.Deserialize<List<Service>>(reader);
                return services;
            });
        }
    }
}