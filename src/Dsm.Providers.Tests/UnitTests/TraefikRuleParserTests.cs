using Dsm.Providers.ServicesProviders.Traefik;

namespace Dsm.Providers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class TraefikRuleParserTests
{
    [TestCase("Host(`a.com`)", "a.com")]
    [TestCase("Host(`a.com`) && Path(`/x`)", "a.com")]
    [TestCase("PathPrefix(`/a`) && Host(`b.com`)", "b.com")]
    [TestCase("Host(`a.com`,`b.com`)", "a.com")]
    [TestCase("Host(`a.com`, `b.com`)", "a.com")]
    [TestCase("Host( `a.com` )", "a.com")]
    [TestCase("Host( `a.com` , `b.com` )", "a.com")]
    [TestCase("Host(, `a.com`)", "a.com")]
    public void ExtractFirstHost_ReturnsFirstHost(string rule, string expected)
    {
        Assert.That(TraefikRuleParser.ExtractFirstHost(rule), Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("Path(`/x`)")]
    public void ExtractFirstHost_ReturnsNull_WhenNoHost(string? rule)
    {
        Assert.That(TraefikRuleParser.ExtractFirstHost(rule), Is.Null);
    }

    [Test]
    public void BuildUrl_UsesScheme()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TraefikRuleParser.BuildUrl("a.com", isHttps: true), Is.EqualTo("https://a.com"));
            Assert.That(TraefikRuleParser.BuildUrl("a.com", isHttps: false), Is.EqualTo("http://a.com"));
        });
    }
}
