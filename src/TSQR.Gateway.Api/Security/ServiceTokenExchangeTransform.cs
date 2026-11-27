using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TSQR.Gateway.Api.Security;

public static class ServiceTokenExchangeTransform
{
    public const string AuthenticatedPolicy = "Authenticated";

    /// <summary>
    /// Replaces the user token with a short-lived service-scoped token before
    /// YARP forwards the request. Public routes are left untouched because
    /// their downstream endpoints do not require a token.
    /// </summary>
    public static void Add(TransformBuilderContext context)
    {
        if (!string.Equals(
                context.Route.AuthorizationPolicy,
                AuthenticatedPolicy,
                StringComparison.Ordinal))
        {
            return;
        }

        var clusterId = context.Route.ClusterId;
        if (string.IsNullOrWhiteSpace(clusterId)
            || string.Equals(clusterId, "ui", StringComparison.Ordinal))
        {
            return;
        }

        var audience = ServiceAudiences.ForCluster(clusterId);
        context.AddRequestTransform(async transformContext =>
        {
            var authorization = transformContext.HttpContext.Request.Headers.Authorization.ToString();
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return;

            var userToken = authorization["Bearer ".Length..].Trim();
            var exchange = transformContext.HttpContext.RequestServices
                .GetRequiredService<IServiceTokenExchange>();
            var serviceToken = await exchange.ExchangeAsync(
                userToken,
                audience,
                transformContext.CancellationToken);

            if (string.IsNullOrWhiteSpace(serviceToken))
            {
                // Fail closed. Never forward a gateway-audience user token to
                // a downstream service that expects its own audience.
                transformContext.ProxyRequest.Headers.Remove("Authorization");
                return;
            }

            transformContext.ProxyRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", serviceToken);
        });
    }
}
