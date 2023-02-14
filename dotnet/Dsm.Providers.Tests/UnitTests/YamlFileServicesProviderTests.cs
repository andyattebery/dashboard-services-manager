using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers.ServicesProviders;
using Dsm.Shared.Options;
using Microsoft.Extensions.Options;
using Dsm.Shared.Models;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class YamlFileServicesProviderTests : BaseTest
{
    private YamlFileServicesProvider _yamlFileServicesProvider;

    [SetUp]
    public void SetUp()
    {
        _yamlFileServicesProvider = ServiceProvider.GetRequiredService<YamlFileServicesProvider>();
    }

    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);

        var providerOptions = new ProviderOptions()
        {
            ProviderType = "yaml",
            ServicesYamlFilePath = Path.Combine("UnitTests", "TestData", "services.yaml")
        };
        var options = Options.Create(providerOptions);
        services.AddTransient<IOptions<ProviderOptions>>((serviceProvider) => options);
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