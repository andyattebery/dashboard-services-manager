using Dsm.Managers.Factories;
using Dsm.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dsm.Managers.Tests.UnitTests;

public class ServicesCombinerTests : BaseTest
{
    private ServicesCombiner _servicesCombiner;

    public override void OneTimeSetUp()
    {
        base.OneTimeSetUp();
        
        _servicesCombiner = ServiceProvider.GetRequiredService<ServicesCombiner>();
    }

    private static readonly List<Service> ExistingServices = new List<Service>()
    {
        new Service("Service A", "https://service-a.example.com", "media", "favicon", null, "host1", false),
        new Service("Service B", "https://service-b.example.com", "utilities", "favicon", null, "host1", false),
    };
    
    [Test]
    public void Test()
    {
        
    }
}