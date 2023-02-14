namespace Dsm.Shared.Options;
public sealed class ProviderOptions
{
    public string ApiUrl { get; set; }
    public string Hostname { get; set; }
    public string DockerLabelPrefix { get; set; }
    public bool AreServiceHostsHttps { get; set; }
    public string ProviderType { get; set; }
    public string ServicesYamlFilePath {get; set;}
}