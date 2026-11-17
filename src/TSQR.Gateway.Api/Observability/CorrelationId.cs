using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace TSQR.Gateway.Api.Observability;

/// <summary>
/// Shared correlation-ID constants and helpers for the gateway. The gateway
/// intentionally has no dependency on TSQR.Common.WebApi so it can build as a
/// standalone repository; the header contract matches the one used by every
/// downstream service.
/// </summary>
public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";
    public const string AlternateHeaderName = "X-Request-ID";
    public const string ItemKey = "TSQR.CorrelationId";
    public const string LogScopeKey = "CorrelationId";
    public const string TraceIdScopeKey = "TraceId";
    public const string SpanIdScopeKey = "SpanId";

    private const int MaxCorrelationIdLength = 128;

    public static string? Get(HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value) ? value as string : null;

    public static string Resolve(HttpContext context)
    {
        var candidate = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = context.Request.Headers[AlternateHeaderName].FirstOrDefault();

        return IsSafe(candidate) ? candidate!.Trim() : Guid.NewGuid().ToString("N");
    }

    public static Dictionary<string, object?> CreateLogScope(HttpContext context)
    {
        var scope = new Dictionary<string, object?>(StringComparer.Ordinal);

        var correlationId = Get(context);
        if (!string.IsNullOrWhiteSpace(correlationId))
            scope[LogScopeKey] = correlationId;

        var activity = Activity.Current;
        if (activity is not null)
        {
            scope[TraceIdScopeKey] = activity.TraceId.ToHexString();
            scope[SpanIdScopeKey] = activity.SpanId.ToHexString();
        }

        return scope;
    }

    private static bool IsSafe(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxCorrelationIdLength
        && value.All(IsAllowedCharacter);

    private static bool IsAllowedCharacter(char value) =>
        char.IsLetterOrDigit(value)
        || value is '-' or '_' or '.' or ':' or '+' or '/' or '=';
}
