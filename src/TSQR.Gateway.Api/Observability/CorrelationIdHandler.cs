using Microsoft.AspNetCore.Http;

namespace TSQR.Gateway.Api.Observability;

/// <summary>
/// Forwards the gateway request's correlation ID on BFF calls made through
/// <see cref="IHttpClientFactory"/>.
/// </summary>
public sealed class CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContextAccessor.HttpContext is { } context
            ? CorrelationId.Get(context)
            : null;

        if (!string.IsNullOrWhiteSpace(correlationId)
            && !request.Headers.Contains(CorrelationId.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(
                CorrelationId.HeaderName,
                correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
