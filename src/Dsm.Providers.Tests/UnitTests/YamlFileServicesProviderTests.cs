using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers.ServicesProviders;
using Dsm.Providers.Options;
using Microsoft.Extensions.Options;
using Dsm.Shared.Models;
using Dsm.Shared.Tests;

namespace Dsm.Providers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class YamlFileServicesProviderTests : BaseTest
{
    private IServicesProvider _yamlFileServicesProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var factory = ServiceProvider.GetRequiredService<ServicesProviderFactory>();
        var config = new ServicesProviderConfig
        {
            ServicesProviderType = ServicesProviderType.YamlFile,
            ServicesYamlFilePath = TestDataUtilities.GetTestDataPath("services.yaml")
        };
        _yamlFileServicesProvider = factory.Create(config);
    }

    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);

        var providerOptions = new ProviderOptions
        {
            ApiUrl = "http://dsm.test",
            ServicesProviders = new List<ServicesProviderConfig>()
        };
        services.AddTransient<IOptions<ProviderOptions>>((serviceProvider) => Microsoft.Extensions.Options.Options.Create(providerOptions));
    }

    [Test]
    public async Task Test()
    {
        var services = await _yamlFileServicesProvider.ListServices();
        Assert.Multiple(() => {
            Assert.That(services, Has.Count.EqualTo(2));

            Assert.That(services, Has.One.Property(nameof(Service.Name)).EqualTo("test1"));
            Assert.That(services.Single(s => s.Name == "test1"), Has.Property(nameof(Service.Url)).EqualTo("https://test1.example.com"));
            Assert.That(services, Has.One.Property(nameof(Service.Name)).EqualTo("test2"));
            Assert.That(services.Single(s => s.Name == "test2"), Has.Property(nameof(Service.Url)).EqualTo("https://test2.example.com"));
        });
    }
}
