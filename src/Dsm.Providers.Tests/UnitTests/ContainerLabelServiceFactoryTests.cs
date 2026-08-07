using Microsoft.Extensions.Logging.Abstractions;
using Dsm.Providers.Services;
using Dsm.Providers.Options;

namespace Dsm.Providers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class ContainerLabelServiceFactoryTests
{
    private static ContainerLabelServiceFactory CreateFactory() =>
        new(NullLogger<ContainerLabelServiceFactory>.Instance);

    private static ServicesProviderConfig CreateConfig() => new()
    {
        ServicesProviderType = ServicesProviderType.Docker,
        DockerLabelPrefix = "dsm",
        AreServiceHostsHttps = true,
        Hostname = "test-host"
    };

    [Test]
    public void ServiceDefaultsNameLabel_IsReadIntoService()
    {
        var factory = CreateFactory();
        var labels = new Dictionary<string, string>
        {
            ["dsm.service_defaults_name"] = "PiKVM"
        };

        var service = factory.CreateFromLabels(CreateConfig(), "test-host", "PiKVM HID", labels);

        Assert.That(service.ServiceDefaultsName, Is.EqualTo("PiKVM"));
    }

    [Test]
    public void ServiceDefaultsName_IsNullWhenLabelAbsent()
    {
        var factory = CreateFactory();
        var labels = new Dictionary<string, string>();

        var service = factory.CreateFromLabels(CreateConfig(), "test-host", "PiKVM HID", labels);

        Assert.That(service.ServiceDefaultsName, Is.Null);
    }

    // A container with two traefik routers, as minio has: one named after the
    // container and one suffixed. The flag controls insertion order, so a test can
    // present the "wrong" one first — which is what the old FirstOrDefault() picked.
    private static Dictionary<string, string> TwoRouterLabels(bool suffixedFirst)
    {
        var labels = new Dictionary<string, string>();
        if (suffixedFirst)
        {
            labels["traefik.http.routers.minio-s3.rule"] = "Host(`s3.example.com`)";
            labels["traefik.http.routers.minio.rule"] = "Host(`minio.example.com`)";
        }
        else
        {
            labels["traefik.http.routers.minio.rule"] = "Host(`minio.example.com`)";
            labels["traefik.http.routers.minio-s3.rule"] = "Host(`s3.example.com`)";
        }
        return labels;
    }

    // The regression this change exists to prevent. The Docker API does not
    // guarantee label ordering, so the same container must resolve to the same URL
    // regardless of the order its labels arrive in.
    [Test]
    public void MultipleRouters_UrlIsIndependentOfLabelOrder()
    {
        var factory = CreateFactory();

        var a = factory.CreateFromLabels(CreateConfig(), "test-host", "minio", TwoRouterLabels(suffixedFirst: true));
        var b = factory.CreateFromLabels(CreateConfig(), "test-host", "minio", TwoRouterLabels(suffixedFirst: false));

        Assert.That(a.Url, Is.EqualTo(b.Url));
    }

    [Test]
    public void MultipleRouters_PrefersRouterNamedAfterContainer()
    {
        var factory = CreateFactory();

        var service = factory.CreateFromLabels(CreateConfig(), "test-host", "minio", TwoRouterLabels(suffixedFirst: true));

        Assert.That(service.Url, Is.EqualTo("https://minio.example.com"));
    }

    [Test]
    public void MultipleRouters_ExplicitLabelWins()
    {
        var factory = CreateFactory();
        var labels = TwoRouterLabels(suffixedFirst: false);
        labels["dsm.traefik.router"] = "minio-s3";

        var service = factory.CreateFromLabels(CreateConfig(), "test-host", "minio", labels);

        Assert.That(service.Url, Is.EqualTo("https://s3.example.com"));
    }

    // No router matches the container name, so selection falls back to a stable
    // sort rather than enumeration order.
    [Test]
    public void MultipleRouters_NoNameMatch_SortsDeterministically()
    {
        var factory = CreateFactory();

        var forward = new Dictionary<string, string>
        {
            ["traefik.http.routers.zulu.rule"] = "Host(`zulu.example.com`)",
            ["traefik.http.routers.alpha.rule"] = "Host(`alpha.example.com`)"
        };
        var reverse = new Dictionary<string, string>
        {
            ["traefik.http.routers.alpha.rule"] = "Host(`alpha.example.com`)",
            ["traefik.http.routers.zulu.rule"] = "Host(`zulu.example.com`)"
        };

        var a = factory.CreateFromLabels(CreateConfig(), "test-host", "unrelated", forward);
        var b = factory.CreateFromLabels(CreateConfig(), "test-host", "unrelated", reverse);

        Assert.That(a.Url, Is.EqualTo("https://alpha.example.com"));
        Assert.That(b.Url, Is.EqualTo(a.Url));
    }

    // A typo'd or stale router label used to fall through to FirstOrDefault()
    // silently; it must now still land on a deterministic choice.
    [Test]
    public void MultipleRouters_UnmatchedExplicitLabel_FallsBackDeterministically()
    {
        var factory = CreateFactory();
        var labels = TwoRouterLabels(suffixedFirst: true);
        labels["dsm.traefik.router"] = "does-not-exist";

        var service = factory.CreateFromLabels(CreateConfig(), "test-host", "minio", labels);

        Assert.That(service.Url, Is.EqualTo("https://minio.example.com"));
    }

    [Test]
    public void SingleRouter_IsUsedWithoutSelection()
    {
        var factory = CreateFactory();
        var labels = new Dictionary<string, string>
        {
            ["traefik.http.routers.whatever.rule"] = "Host(`only.example.com`)"
        };

        var service = factory.CreateFromLabels(CreateConfig(), "test-host", "some-container", labels);

        Assert.That(service.Url, Is.EqualTo("https://only.example.com"));
    }

    [Test]
    public void UrlLabel_OverridesRouterSelection()
    {
        var factory = CreateFactory();
        var labels = TwoRouterLabels(suffixedFirst: true);
        labels["dsm.url"] = "https://explicit.example.com";

        var service = factory.CreateFromLabels(CreateConfig(), "test-host", "minio", labels);

        Assert.That(service.Url, Is.EqualTo("https://explicit.example.com"));
    }
}
