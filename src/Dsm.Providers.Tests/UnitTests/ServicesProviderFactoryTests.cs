using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Dsm.Providers.ServicesProviders;
using Dsm.Providers.Options;
using Microsoft.Extensions.Configuration;

namespace Dsm.Providers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
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
            ServicesYamlFilePath = "/tmp/test.yaml",
            Hostname = "test-host"
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
            ServicesProviders = new List<ServicesProviderConfig>()
        };
        services.AddTransient<IOptions<ProviderOptions>>(_ => Microsoft.Extensions.Options.Options.Create(providerOptions));
    }
}
