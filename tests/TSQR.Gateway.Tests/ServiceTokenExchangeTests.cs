using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TSQR.Gateway.Api.Security;

namespace TSQR.Gateway.Tests;

public class ServiceTokenExchangeTests
{
    [Fact]
    public async Task ExchangeAsync_SendsInternalRequestAndCachesToken()
    {
        var handler = new StubHandler(_ => Json(new
        {
            accessToken = "service-token",
            expiresAt = DateTime.UtcNow.AddMinutes(5),
        }));
        var exchange = CreateExchange(handler, "internal-key");

        var first = await exchange.ExchangeAsync("user-token", ServiceAudiences.ToolLib);
        var second = await exchange.ExchangeAsync("user-token", ServiceAudiences.ToolLib);

        Assert.Equal("service-token", first);
        Assert.Equal(first, second);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(ServiceTokenExchange.ExchangePath, request.Path);
        Assert.Equal("Bearer user-token", request.Authorization);
        Assert.Equal("internal-key", request.InternalApiKey);
        Assert.Contains("\"audience\":\"tsqr-tool-lib\"", request.Body);
    }

    [Fact]
    public async Task ExchangeAsync_WhenAutheoRejects_ReturnsNullAndDoesNotCache()
    {
        var handler = new StubHandler(
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var exchange = CreateExchange(handler, "internal-key");

        var result = await exchange.ExchangeAsync("user-token", ServiceAudiences.Support);

        Assert.Null(result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ExchangeAsync_WithoutInternalApiKey_FailsClosed()
    {
        var handler = new StubHandler(
            _ => Json(new
            {
                accessToken = "service-token",
                expiresAt = DateTime.UtcNow.AddMinutes(5),
            }));
        var exchange = CreateExchange(handler, internalApiKey: null);

        var result = await exchange.ExchangeAsync("user-token", ServiceAudiences.ToolLib);

        Assert.Null(result);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("autheo", ServiceAudiences.Autheo)]
    [InlineData("tool-lib", ServiceAudiences.ToolLib)]
    [InlineData("support", ServiceAudiences.Support)]
    [InlineData("identity", ServiceAudiences.Identity)]
    [InlineData("soup-kitchen", ServiceAudiences.SoupKitchen)]
    [InlineData("communities", ServiceAudiences.Communities)]
    [InlineData("learning", ServiceAudiences.Learning)]
    public void ForCluster_MapsClusterToServiceAudience(
        string clusterId,
        string expectedAudience)
    {
        Assert.Equal(expectedAudience, ServiceAudiences.ForCluster(clusterId));
    }

    private static ServiceTokenExchange CreateExchange(
        StubHandler handler,
        string? internalApiKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalApi:Key"] = internalApiKey,
            })
            .Build();
        var factory = new FakeHttpClientFactory(
            new HttpClient(handler) { BaseAddress = new Uri("http://autheo") });
        return new ServiceTokenExchange(
            factory,
            new MemoryCache(new MemoryCacheOptions()),
            configuration,
            NullLogger<ServiceTokenExchange>.Instance);
    }

    private static HttpResponseMessage Json(object value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed record CapturedRequest(
        string Path,
        string? Authorization,
        string? InternalApiKey,
        string Body);

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var internalApiKey = request.Headers.TryGetValues(
                ServiceTokenExchange.InternalApiKeyHeader,
                out var values)
                ? values.Single()
                : null;
            Requests.Add(new CapturedRequest(
                request.RequestUri?.AbsolutePath ?? string.Empty,
                request.Headers.Authorization?.ToString(),
                internalApiKey,
                body));
            return responder(request);
        }
    }
}
