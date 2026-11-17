using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TSQR.Gateway.Api.Observability;

/// <summary>
/// Assigns or echoes a correlation ID, adds it to the response and the current
/// OpenTelemetry activity, and includes it in the gateway's logging scope.
/// </summary>
public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationId.Resolve(context);
        context.Items[CorrelationId.ItemKey] = correlationId;
        context.Response.Headers[CorrelationId.HeaderName] = correlationId;

        var activity = Activity.Current;
        activity?.SetTag("correlation.id", correlationId);
        activity?.SetBaggage("correlation.id", correlationId);

        using (logger.BeginScope(CorrelationId.CreateLogScope(context)))
        {
            await next(context);
        }
    }
}
