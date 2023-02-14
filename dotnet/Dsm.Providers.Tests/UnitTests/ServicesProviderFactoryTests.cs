using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers.ServicesProviders;
using Dsm.Shared.Options;
using Microsoft.Extensions.Options;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class ServicesProviderFactoryTests : BaseTest
{
    private ServicesProviderFactory _servicesProviderFactory;

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
        // var serviceProvider = ServicesProviderFactory.Create(
        //     (configuration) => {},
        //     (configuration, services) => {
        //         var providerOptions = new ProviderOptions()
        //         {
        //             ServicesProviderType = configurationProviderType
        //         };
        //         var options = Options.Create(providerOptions);
        //         services.AddTransient<IOptions<ProviderOptions>>((serviceProvider) => options);
        //     }
        // );

        // var servicesProvider = serviceProvider.GetRequiredService<IServicesProvider>();
        var servicesProvider = _servicesProviderFactory.Create(servicesProviderTypeString);
        Assert.IsInstanceOf(type, servicesProvider);
    }
}