using System.ComponentModel.DataAnnotations;

namespace Dsm.Providers.Options;

public sealed class ServicesProviderConfig
{
    [EnumDataType(typeof(ServicesProviderType))]
    public required ServicesProviderType ServicesProviderType { get; set; }

    public bool AreServiceHostsHttps { get; set; }

    public string? Hostname { get; set; }

    public string? TraefikApiUrl { get; set; }
    public string? ServicesYamlFilePath { get; set; }
    public string? DockerLabelPrefix { get; set; }
}
