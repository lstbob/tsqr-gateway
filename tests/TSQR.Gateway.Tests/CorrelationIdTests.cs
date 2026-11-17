using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using TSQR.Gateway.Api.Observability;

namespace TSQR.Gateway.Tests;

public class CorrelationIdTests
{
    [Fact]
    public async Task Middleware_EchoesSafeInboundCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new ImmediateResponseFeature());
        context.Request.Headers[CorrelationId.HeaderName] = "client-correlation-123";
        var nextCalled = false;

        var middleware = new CorrelationIdMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal("client-correlation-123", CorrelationId.Get(context));
        Assert.Equal(
            "client-correlation-123",
            context.Response.Headers[CorrelationId.HeaderName].ToString());
    }

    [Fact]
    public async Task Middleware_UsesAlternateHeaderWhenCanonicalHeaderIsMissing()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationId.AlternateHeaderName] = "proxy-request-456";

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("proxy-request-456", CorrelationId.Get(context));
    }

    [Fact]
    public async Task Middleware_GeneratesCorrelationIdWhenHeaderIsMissingOrUnsafe()
    {
        var missing = new DefaultHttpContext();
        var unsafeValue = new DefaultHttpContext();
        unsafeValue.Request.Headers[CorrelationId.HeaderName] = "contains spaces";

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(missing);
        await middleware.InvokeAsync(unsafeValue);

        Assert.True(Guid.TryParseExact(
            CorrelationId.Get(missing),
            "N",
            out _));
        Assert.True(Guid.TryParseExact(
            CorrelationId.Get(unsafeValue),
            "N",
            out _));
    }

    [Fact]
    public async Task Handler_AddsCorrelationIdToOutgoingRequest()
    {
        var context = new DefaultHttpContext();
        context.Items[CorrelationId.ItemKey] = "gateway-correlation-789";
        var accessor = new HttpContextAccessor { HttpContext = context };
        var inner = new CapturingHandler();
        var handler = new CorrelationIdHandler(accessor) { InnerHandler = inner };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://downstream/api");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(
            "gateway-correlation-789",
            string.Join(",", inner.Request!.Headers.GetValues(CorrelationId.HeaderName)));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private sealed class ImmediateResponseFeature : HttpResponseFeature
    {
        public override void OnStarting(Func<object, Task> callback, object state)
        {
            callback(state).GetAwaiter().GetResult();
        }
    }
}
