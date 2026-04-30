using NetEscapades.Configuration.Yaml;

namespace Dsm.Shared.Configuration;

public class NormalizedYamlConfigurationProvider : YamlConfigurationProvider
{
    public NormalizedYamlConfigurationProvider(YamlConfigurationSource source) : base(source) { }

    public override void Load(Stream stream)
    {
        base.Load(stream);
        if (Data.Count == 0) return;
        var normalized = new Dictionary<string, string?>(Data.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in Data)
        {
            normalized[kvp.Key.Replace("_", string.Empty)] = kvp.Value;
        }
        Data = normalized;
    }
}
