using Dsm.Providers.ServicesProviders;

namespace Dsm.Providers.Tests.UnitTests;

public class SwarmServicesProviderTests
{
    [TestCase("mystack_api", "mystack", "api")]
    [TestCase("a.b_foo", "a.b", "foo")]
    [TestCase("a+b_bar", "a+b", "bar")]
    [TestCase("standalone", "otherstack", "standalone")]
    [TestCase("mystack_", "mystack", "mystack_")]
    [TestCase("api", null, "api")]
    [TestCase("api", "", "api")]
    public void StripStackPrefix(string name, string? ns, string expected)
    {
        Assert.That(SwarmServicesProvider.StripStackPrefix(name, ns), Is.EqualTo(expected));
    }
}
