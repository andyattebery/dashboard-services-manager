using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Dsm.Providers.Options;
using Dsm.Providers.Services;
using Dsm.Providers.ServicesProviders;

namespace Dsm.Providers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class SwarmServicesProviderTests
{
    [TestCase("mystack_api", "mystack", "api")]
    [TestCase("a.b_foo", "a.b", "foo")]
    [TestCase("a+b_bar", "a+b", "bar")]
    [TestCase("standalone", "otherstack", "standalone")]
    [TestCase("mystack_", "mystack", "mystack_")]
    [TestCase("api", null, "api")]
    [TestCase("api", "", "api")]
    public void StripStackPrefix_RemovesNamespacePrefix(string name, string? ns, string expected)
    {
        Assert.That(SwarmServicesProvider.StripStackPrefix(name, ns), Is.EqualTo(expected));
    }

    // The router named after the service sorts after the other one, so only a match on the raw,
    // stack-stripped name (adguard-home) picks it; the formatted name (Adguard Home) never matches.
    [Test]
    public async Task MultipleRouters_PrefersRouterNamedAfterStackStrippedServiceName()
    {
        var swarmService = new SwarmService
        {
            Spec = new ServiceSpec
            {
                Name = "media_adguard-home",
                Labels = new Dictionary<string, string>
                {
                    ["com.docker.stack.namespace"] = "media",
                    ["traefik.http.routers.adguard-dns.rule"] = "Host(`dns.example.com`)",
                    ["traefik.http.routers.adguard-home.rule"] = "Host(`adguard.example.com`)",
                }
            }
        };
        var dockerClient = new Mock<IDockerClient>();
        dockerClient
            .Setup(dc => dc.Swarm.ListServicesAsync(It.IsAny<ServicesListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { swarmService });
        var provider = new SwarmServicesProvider(
            new ContainerLabelServiceFactory(NullLogger<ContainerLabelServiceFactory>.Instance),
            dockerClient.Object,
            new ServicesProviderConfig
            {
                ServicesProviderType = ServicesProviderType.Swarm,
                DockerLabelPrefix = "dsm",
                AreServiceHostsHttps = true,
                Hostname = "swarm"
            });

        var service = (await provider.ListServices()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(service.Url, Is.EqualTo("https://adguard.example.com"));
            Assert.That(service.Name, Is.EqualTo("Adguard Home"));
        });
    }
}
