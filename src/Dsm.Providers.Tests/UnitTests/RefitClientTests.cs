using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Dsm.Providers.ApiClients;
using Dsm.Providers.Options;
using Dsm.Providers.ServicesProviders.Traefik;
using Dsm.Shared.Models;

namespace Dsm.Providers.Tests.UnitTests;

// The provider and Traefik tests mock IDcmClient / ITraefikApiClient, so nothing else builds a
// real Refit client. These do, against a stub handler, so a Refit upgrade that breaks client
// creation or the request shape fails here instead of at provider startup.
[CancelAfter(TestTimeouts.HungThresholdMs)]
public class RefitClientTests
{
    [Test]
    public async Task DcmClient_PostsServicesAndReadsResponse()
    {
        var handler = new StubHandler(_ => Json("""{"Dashy":[{"name":"test1","url":"https://test1.example.com"}]}"""));
        var client = CreateDcmClient(handler);

        var response = await client.UpdateDashboard(new[] { new Service("test1", "https://test1.example.com", null, null, null, null, false) });

        using var body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Multiple(() =>
        {
            Assert.That(handler.Request!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Request.RequestUri!.AbsolutePath, Is.EqualTo("/dashboard-services"));
            Assert.That(body.RootElement[0].GetProperty("name").GetString(), Is.EqualTo("test1"));
            Assert.That(response["Dashy"].Single().Url, Is.EqualTo("https://test1.example.com"));
        });
    }

    // ProviderService.PostServices catches ApiRequestException to log a manager that is down or
    // slow and keep the loop running; Refit 11+ wraps transport errors in it.
    [Test]
    public async Task DcmClient_TransportFailure_SurfacesAsApiRequestException()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));
        var client = CreateDcmClient(handler);

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() => client.UpdateDashboard(Array.Empty<Service>()));

        Assert.That(exception!.InnerException, Is.InstanceOf<HttpRequestException>()
            .With.Message.EqualTo("connection refused"));
    }

    [Test]
    public async Task TraefikClient_GetsRouters()
    {
        var handler = new StubHandler(_ => Json("""[{"name":"whoami@docker","rule":"Host(`whoami.example.com`)","status":"enabled"}]"""));
        var services = new ServiceCollection();
        services.AddHttpClient(TraefikApiClientFactory.ClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        var factory = new TraefikApiClientFactory(services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>());

        var routers = await factory.Create("http://traefik.test").GetRouters();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Request!.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.Request.RequestUri!.AbsolutePath, Is.EqualTo("/api/http/routers"));
            Assert.That(routers.Single().Name, Is.EqualTo("whoami@docker"));
        });
    }

    private static IDcmClient CreateDcmClient(StubHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ProviderOptions
        {
            ApiUrl = "http://manager.test",
            ServicesProviders = new List<ServicesProviderConfig>()
        }));
        services.AddDcmClient();
        // AddRefitClient sets its own primary handler on the named client, which wins over
        // ConfigureHttpClientDefaults; configuring that name after it replaces the handler.
        services.AddHttpClient(UniqueName.ForType<IDcmClient>()).ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider().GetRequiredService<IDcmClient>();
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return _respond(request);
        }
    }
}
