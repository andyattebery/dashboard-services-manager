using Dsm.Providers.ServicesProviders.Traefik;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class TraefikRuleParserTests
{
    [TestCase("Host(`a.com`)", "a.com")]
    [TestCase("Host(`a.com`) && Path(`/x`)", "a.com")]
    [TestCase("PathPrefix(`/a`) && Host(`b.com`)", "b.com")]
    [TestCase("Host(`a.com`,`b.com`)", "a.com")]
    [TestCase("Host(`a.com`, `b.com`)", "a.com")]
    public void ExtractFirstHost_returns_first_host(string rule, string expected)
    {
        Assert.That(TraefikRuleParser.ExtractFirstHost(rule), Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("Path(`/x`)")]
    public void ExtractFirstHost_returns_null_when_no_host(string? rule)
    {
        Assert.That(TraefikRuleParser.ExtractFirstHost(rule), Is.Null);
    }

    [Test]
    public void BuildUrl_uses_scheme()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TraefikRuleParser.BuildUrl("a.com", isHttps: true), Is.EqualTo("https://a.com"));
            Assert.That(TraefikRuleParser.BuildUrl("a.com", isHttps: false), Is.EqualTo("http://a.com"));
        });
    }
}
