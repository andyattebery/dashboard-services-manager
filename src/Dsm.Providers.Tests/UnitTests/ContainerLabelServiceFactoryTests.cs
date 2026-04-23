using Microsoft.Extensions.Logging.Abstractions;
using Dsm.Providers.Services;
using Dsm.Shared.Options;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class ContainerLabelServiceFactoryTests
{
    private static ContainerLabelServiceFactory CreateFactory() =>
        new(NullLogger<ContainerLabelServiceFactory>.Instance);

    private static ServicesProviderConfig CreateConfig() => new()
    {
        ServicesProviderType = ServicesProviderType.Docker,
        DockerLabelPrefix = "dsm",
        AreServiceHostsHttps = true,
        Hostname = "test-host"
    };

    [Test]
    public void ServiceDefaultsNameLabel_IsReadIntoService()
    {
        var factory = CreateFactory();
        var labels = new Dictionary<string, string>
        {
            ["dsm.service_defaults_name"] = "PiKVM"
        };

        var service = factory.CreateFromLabels(CreateConfig(), "test-host", "PiKVM HID", labels);

        Assert.That(service.ServiceDefaultsName, Is.EqualTo("PiKVM"));
    }

    [Test]
    public void ServiceDefaultsName_IsNullWhenLabelAbsent()
    {
        var factory = CreateFactory();
        var labels = new Dictionary<string, string>();

        var service = factory.CreateFromLabels(CreateConfig(), "test-host", "PiKVM HID", labels);

        Assert.That(service.ServiceDefaultsName, Is.Null);
    }
}
