using Dsm.Shared.Configuration;
using Dsm.Providers.Options;
using Microsoft.Extensions.Configuration;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class NormalizedYamlConfigurationTests
{
    private string _tempPath = null!;

    [TearDown]
    public void TearDown()
    {
        if (_tempPath is not null && File.Exists(_tempPath))
        {
            File.Delete(_tempPath);
        }
    }

    private IConfiguration LoadYaml(string yaml)
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"normalized-yaml-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(_tempPath, yaml);
        return new ConfigurationBuilder()
            .AddNormalizedYamlFile(_tempPath, optional: false)
            .Build();
    }

    [Test]
    public void SnakeCaseKeys_BindToPascalCaseProperty()
    {
        var yaml = """
            provider_options:
              api_url: http://dsm.test
              services_providers:
                - services_provider_type: Docker
                  docker_label_prefix: dsm
                  are_service_hosts_https: true
                  hostname: host-01
            """;

        var config = LoadYaml(yaml);
        var options = config.GetSection("ProviderOptions").Get<ProviderOptions>();

        Assert.That(options, Is.Not.Null);
        Assert.That(options!.ApiUrl, Is.EqualTo("http://dsm.test"));
        Assert.That(options.ServicesProviders, Has.Count.EqualTo(1));
        Assert.That(options.ServicesProviders[0].ServicesProviderType, Is.EqualTo(ServicesProviderType.Docker));
        Assert.That(options.ServicesProviders[0].DockerLabelPrefix, Is.EqualTo("dsm"));
        Assert.That(options.ServicesProviders[0].AreServiceHostsHttps, Is.True);
        Assert.That(options.ServicesProviders[0].Hostname, Is.EqualTo("host-01"));
    }

    [Test]
    public void PascalCaseKeys_StillBind()
    {
        var yaml = """
            ProviderOptions:
              ApiUrl: http://dsm.test
              ServicesProviders:
                - ServicesProviderType: Docker
                  DockerLabelPrefix: dsm
                  Hostname: host-01
            """;

        var config = LoadYaml(yaml);
        var options = config.GetSection("ProviderOptions").Get<ProviderOptions>();

        Assert.That(options!.ApiUrl, Is.EqualTo("http://dsm.test"));
        Assert.That(options.ServicesProviders[0].DockerLabelPrefix, Is.EqualTo("dsm"));
    }

    [Test]
    public void CamelCaseKeys_StillBind()
    {
        var yaml = """
            providerOptions:
              apiUrl: http://dsm.test
              servicesProviders:
                - servicesProviderType: Docker
                  dockerLabelPrefix: dsm
                  hostname: host-01
            """;

        var config = LoadYaml(yaml);
        var options = config.GetSection("ProviderOptions").Get<ProviderOptions>();

        Assert.That(options!.ApiUrl, Is.EqualTo("http://dsm.test"));
        Assert.That(options.ServicesProviders[0].DockerLabelPrefix, Is.EqualTo("dsm"));
    }

    [Test]
    public void DashInDictKey_IsPreserved()
    {
        var yaml = """
            Root:
              calibre-web: media
              home assistant: smart home
            """;

        var config = LoadYaml(yaml);
        var section = config.GetSection("Root");

        Assert.That(section["calibre-web"], Is.EqualTo("media"));
        Assert.That(section["home assistant"], Is.EqualTo("smart home"));
    }

    [Test]
    public void UnderscoreInDictKey_IsStripped()
    {
        var yaml = """
            Root:
              my_service: hit
            """;

        var config = LoadYaml(yaml);
        var section = config.GetSection("Root");

        Assert.That(section["myservice"], Is.EqualTo("hit"));
        Assert.That(section["my_service"], Is.Null);
    }
}
