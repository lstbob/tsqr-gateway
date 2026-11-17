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

        // Downstream services echo the same header. Use OnStarting so the
        // gateway only supplies it for responses that do not already have one
        // (BFF/error responses) and never duplicates the proxied header.
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationId.HeaderName))
                context.Response.Headers[CorrelationId.HeaderName] = correlationId;

            return Task.CompletedTask;
        });

        var activity = Activity.Current;
        activity?.SetTag("correlation.id", correlationId);
        activity?.SetBaggage("correlation.id", correlationId);

        using (logger.BeginScope(CorrelationId.CreateLogScope(context)))
        {
            await next(context);
        }
    }
}
