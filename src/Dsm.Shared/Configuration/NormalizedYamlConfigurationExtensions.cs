using Microsoft.Extensions.Configuration;

namespace Dsm.Shared.Configuration;

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
