using Dsm.Providers.Options;

namespace Dsm.Providers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class ProviderOptionsValidatorTests
{
    [Test]
    public void FailsWhenServicesProvidersIsNull()
    {
        var options = new ProviderOptions
        {
            ApiUrl = "http://localhost",
            ServicesProviders = null!,
        };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("ServicesProviders is required"));
    }

    [Test]
    public void FailsWhenServicesProvidersIsEmpty()
    {
        var options = new ProviderOptions
        {
            ApiUrl = "http://localhost",
            ServicesProviders = new List<ServicesProviderConfig>(),
        };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("must contain at least one entry"));
    }

    [Test]
    public void FailsWhenDockerEntryMissingDockerLabelPrefixAndHostname()
    {
        var options = new ProviderOptions
        {
            ApiUrl = "http://localhost",
            ServicesProviders = new List<ServicesProviderConfig>
            {
                new() { ServicesProviderType = ServicesProviderType.Docker },
            },
        };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("DockerLabelPrefix"));
        Assert.That(result.Failures, Has.Some.Contains("Hostname"));
    }

    [Test]
    public void FailsWhenTraefikEntryMissingApiUrlAndHostname()
    {
        var options = new ProviderOptions
        {
            ApiUrl = "http://localhost",
            ServicesProviders = new List<ServicesProviderConfig>
            {
                new() { ServicesProviderType = ServicesProviderType.Traefik },
            },
        };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("TraefikApiUrl"));
        Assert.That(result.Failures, Has.Some.Contains("Hostname"));
    }

    [Test]
    public void FailsWhenYamlFileEntryMissingPath()
    {
        var options = new ProviderOptions
        {
            ApiUrl = "http://localhost",
            ServicesProviders = new List<ServicesProviderConfig>
            {
                new() { ServicesProviderType = ServicesProviderType.YamlFile },
            },
        };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("ServicesYamlFilePath"));
    }

    [Test]
    public void SucceedsForValidDockerEntry()
    {
        var options = new ProviderOptions
        {
            ApiUrl = "http://localhost",
            ServicesProviders = new List<ServicesProviderConfig>
            {
                new()
                {
                    ServicesProviderType = ServicesProviderType.Docker,
                    DockerLabelPrefix = "dsm",
                    Hostname = "media-01",
                },
            },
        };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.That(result.Succeeded, Is.True);
    }
}
