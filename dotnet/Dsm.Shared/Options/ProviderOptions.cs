namespace Dsm.Shared.Options;
public sealed class ProviderOptions
{
    public string? ApiUrl { get; set; }
    public string? Hostname { get; set; }
    public string? DockerLabelPrefix { get; set; }
    public bool AreServiceHostsHttps { get; set; }

    [Obsolete("Use ServicesProviderTypes (plural) to allow more than one provider per worker.")]
    public string? ServicesProviderType { get; set; }

    public List<string>? ServicesProviderTypes { get; set; }
    public string? ServicesYamlFilePath { get; set; }
    public string? TraefikApiUrl { get; set; }
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(60);
}
