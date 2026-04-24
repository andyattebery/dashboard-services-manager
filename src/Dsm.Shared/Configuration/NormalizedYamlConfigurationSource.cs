using Microsoft.Extensions.Configuration;
using NetEscapades.Configuration.Yaml;

namespace Dsm.Shared.Configuration;

public class NormalizedYamlConfigurationSource : YamlConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new NormalizedYamlConfigurationProvider(this);
    }
}

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

public static class NormalizedYamlConfigurationExtensions
{
    public static IConfigurationBuilder AddNormalizedYamlFile(
        this IConfigurationBuilder builder,
        string path,
        bool optional = false,
        bool reloadOnChange = false)
    {
        var source = new NormalizedYamlConfigurationSource
        {
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange,
        };
        source.ResolveFileProvider();
        builder.Add(source);
        return builder;
    }
}
