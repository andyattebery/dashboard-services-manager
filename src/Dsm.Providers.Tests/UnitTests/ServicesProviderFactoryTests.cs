using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Dsm.Providers.ServicesProviders;
using Dsm.Shared.Options;
using Microsoft.Extensions.Configuration;

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

    [TestCase(ServicesProviderType.Docker, typeof(DockerServicesProvider))]
    [TestCase(ServicesProviderType.Swarm, typeof(SwarmServicesProvider))]
    [TestCase(ServicesProviderType.YamlFile, typeof(YamlFileServicesProvider))]
    public void Test(ServicesProviderType providerType, Type type)
    {
        var config = new ServicesProviderConfig
        {
            ServicesProviderType = providerType,
            DockerLabelPrefix = "dsm",
            ServicesYamlFilePath = "/tmp/test.yaml"
        };
        var servicesProvider = _servicesProviderFactory.Create(config);
        Assert.That(servicesProvider, Is.InstanceOf(type));
    }

    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);

        var providerOptions = new ProviderOptions
        {
            ApiUrl = "http://dsm.test",
            Hostname = "test-host",
            ServicesProviders = new List<ServicesProviderConfig>()
        };
        services.AddTransient<IOptions<ProviderOptions>>(_ => Options.Create(providerOptions));
    }
}
