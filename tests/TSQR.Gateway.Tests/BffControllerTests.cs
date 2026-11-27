using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TSQR.Gateway.Api.Controllers;
using TSQR.Gateway.Api.Dtos;
using TSQR.Gateway.Api.Security;

namespace TSQR.Gateway.Tests;

public class BffControllerTests
{
    [Fact]
    public async Task Dashboard_ForwardsTokenAndCombinesResponses()
    {
        var user = new UserInfo(Guid.NewGuid(), "ada@example.com", "Ada Lovelace", "Admin");
        var stats = new DashboardStats(10, 20, 3, 1, 2);
        var soupKitchenStats = new SoupKitchenDashboardStats(5, 2, 8, 4, 30);
        var communitiesStats = new CommunitiesDashboardStats(6, 4, 1, 1, 0);

        var autheo = new StubHandler(_ => Json(user));
        var toolLib = new StubHandler(_ => Json(stats));
        var soupKitchen = new StubHandler(_ => Json(soupKitchenStats));
        var communities = new StubHandler(_ => Json(communitiesStats));
        var (controller, factory, exchange) = CreateController(
            autheo,
            toolLib,
            soupKitchen,
            communities);

        var result = await controller.Dashboard(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DashboardResponse>(ok.Value);
        Assert.Equal(user, response.User);
        Assert.Equal(stats, response.Stats);
        Assert.Equal(soupKitchenStats, response.SoupKitchenStats);
        Assert.Equal(communitiesStats, response.CommunitiesStats);

        Assert.Equal(
            new[] { "autheo", "tool-lib", "soup-kitchen", "communities" },
            factory.RequestedNames);
        Assert.Equal(
            new[]
            {
                ServiceAudiences.Autheo,
                ServiceAudiences.ToolLib,
                ServiceAudiences.SoupKitchen,
                ServiceAudiences.Communities,
            },
            exchange.Calls.Select(call => call.Audience).ToArray());
        Assert.All(exchange.Calls, call => Assert.Equal("test-token", call.UserToken));
        AssertRequest(autheo, "/api/auth/me", "service-tsqr-autheo");
        AssertRequest(toolLib, "/api/dashboard/stats", "service-tsqr-tool-lib");
        AssertRequest(soupKitchen, "/api/dashboard/stats", "service-tsqr-soup-kitchen");
        AssertRequest(
            communities,
            "/api/communities/dashboard/stats",
            "service-tsqr-communities");
    }

    [Fact]
    public async Task Dashboard_AutheoUnauthorized_ReturnsUnauthorized()
    {
        var autheo = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var toolLib = new StubHandler(_ => Json(new DashboardStats(1, 2, 3, 4, 5)));
        var soupKitchen = new StubHandler(_ => Json(new SoupKitchenDashboardStats(1, 2, 3, 4, 5)));
        var communities = new StubHandler(_ => Json(new CommunitiesDashboardStats(1, 2, 3, 4, 5)));
        var (controller, _, _) = CreateController(autheo, toolLib, soupKitchen, communities);

        var result = await controller.Dashboard(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Single(autheo.Requests);
        Assert.Single(toolLib.Requests);
        Assert.Single(soupKitchen.Requests);
        Assert.Single(communities.Requests);
    }

    [Fact]
    public async Task Dashboard_WhenAutheoExchangeFails_ReturnsUnauthorizedWithoutCallingDownstreams()
    {
        var autheo = new StubHandler(_ => Json(new UserInfo(Guid.NewGuid(), "a@b.c", "A", "Member")));
        var toolLib = new StubHandler(_ => Json(new DashboardStats(1, 2, 3, 4, 5)));
        var soupKitchen = new StubHandler(_ => Json(new SoupKitchenDashboardStats(1, 2, 3, 4, 5)));
        var communities = new StubHandler(_ => Json(new CommunitiesDashboardStats(1, 2, 3, 4, 5)));
        var exchange = new FakeServiceTokenExchange();
        exchange.FailingAudiences.Add(ServiceAudiences.Autheo);
        var (controller, factory, _) = CreateController(
            autheo,
            toolLib,
            soupKitchen,
            communities,
            exchange);

        var result = await controller.Dashboard(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Empty(factory.RequestedNames);
        Assert.Empty(autheo.Requests);
        Assert.Empty(toolLib.Requests);
        Assert.Empty(soupKitchen.Requests);
        Assert.Empty(communities.Requests);
    }

    [Fact]
    public async Task Dashboard_DownstreamFailures_ReturnZeroFilledStats()
    {
        var user = new UserInfo(Guid.NewGuid(), "ada@example.com", "Ada Lovelace", "Member");
        var soupKitchenStats = new SoupKitchenDashboardStats(5, 2, 8, 4, 30);
        var autheo = new StubHandler(_ => Json(user));
        var toolLib = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var soupKitchen = new StubHandler(_ => Json(soupKitchenStats));
        var communities = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var (controller, _, _) = CreateController(autheo, toolLib, soupKitchen, communities);

        var result = await controller.Dashboard(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DashboardResponse>(ok.Value);
        Assert.Equal(user, response.User);
        Assert.Equal(new DashboardStats(0, 0, 0, 0, 0), response.Stats);
        Assert.Equal(soupKitchenStats, response.SoupKitchenStats);
        Assert.Equal(new CommunitiesDashboardStats(0, 0, 0, 0, 0), response.CommunitiesStats);
    }

    private static (
        BffController Controller,
        FakeHttpClientFactory Factory,
        FakeServiceTokenExchange Exchange) CreateController(
        StubHandler autheo,
        StubHandler toolLib,
        StubHandler soupKitchen,
        StubHandler communities,
        FakeServiceTokenExchange? exchange = null)
    {
        var factory = new FakeHttpClientFactory(new Dictionary<string, HttpClient>
        {
            ["autheo"] = Client(autheo, "http://autheo"),
            ["tool-lib"] = Client(toolLib, "http://tool-lib"),
            ["soup-kitchen"] = Client(soupKitchen, "http://soup-kitchen"),
            ["communities"] = Client(communities, "http://communities"),
        });
        exchange ??= new FakeServiceTokenExchange();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer test-token";
        var controller = new BffController(factory, exchange)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, factory, exchange);
    }

    private static HttpClient Client(StubHandler handler, string baseAddress) =>
        new(handler) { BaseAddress = new Uri(baseAddress) };

    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private static void AssertRequest(
        StubHandler handler,
        string path,
        string expectedToken)
    {
        var request = Assert.Single(handler.Requests);
        Assert.Equal(path, request.RequestUri?.AbsolutePath);
        Assert.Equal(expectedToken, request.Headers.Authorization?.Parameter);
    }

    private sealed class FakeServiceTokenExchange : IServiceTokenExchange
    {
        public List<(string UserToken, string Audience)> Calls { get; } = [];

        public HashSet<string> FailingAudiences { get; } = [];

        public Task<string?> ExchangeAsync(
            string userAccessToken,
            string audience,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((userAccessToken, audience));
            return Task.FromResult(
                FailingAudiences.Contains(audience)
                    ? null
                    : $"service-{audience}");
        }
    }

    private sealed class FakeHttpClientFactory(Dictionary<string, HttpClient> clients)
        : IHttpClientFactory
    {
        public List<string> RequestedNames { get; } = [];

        public HttpClient CreateClient(string name)
        {
            RequestedNames.Add(name);
            return clients[name];
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
