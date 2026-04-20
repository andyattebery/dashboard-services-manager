using System.Text.Json.Serialization;

namespace Dsm.Providers.ServicesProviders.Traefik;

public record class TraefikRouter
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("service")]
    public string? Service { get; init; }

    [JsonPropertyName("rule")]
    public string? Rule { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("provider")]
    public string? Provider { get; init; }
}
