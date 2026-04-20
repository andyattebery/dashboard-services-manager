using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers.ServicesProviders;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class ServicesProviderFactoryTests : BaseTest
{
    private ServicesProviderFactory _servicesProviderFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _servicesProviderFactory = ServiceProvider.GetRequiredService<ServicesProviderFactory>();
    }

    [TestCase("docker", typeof(DockerServicesProvider))]
    [TestCase("swarm", typeof(SwarmServicesProvider))]
    [TestCase("yaml", typeof(YamlFileServicesProvider))]
    [TestCase("yaml_file", typeof(YamlFileServicesProvider))]
    [TestCase("yamlfile", typeof(YamlFileServicesProvider))]
    public void Test(string servicesProviderTypeString, Type type)
    {
        var servicesProvider = _servicesProviderFactory.Create(servicesProviderTypeString);
        Assert.That(servicesProvider, Is.InstanceOf(type));
    }
}