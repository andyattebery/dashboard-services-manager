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
