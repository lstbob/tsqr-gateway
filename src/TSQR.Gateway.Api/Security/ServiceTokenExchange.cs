using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TSQR.Gateway.Api.Security;

public sealed class ServiceTokenExchange(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IConfiguration configuration,
    ILogger<ServiceTokenExchange> logger) : IServiceTokenExchange
{
    public const string HttpClientName = "autheo-token-exchange";
    public const string InternalApiKeyHeader = "X-Internal-Api-Key";
    public const string ExchangePath = "/internal/auth/exchange";

    private readonly string _internalApiKey = configuration["InternalApi:Key"] ?? string.Empty;

    public async Task<string?> ExchangeAsync(
        string userAccessToken,
        string audience,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userAccessToken) || string.IsNullOrWhiteSpace(audience))
            return null;

        if (string.IsNullOrWhiteSpace(_internalApiKey))
        {
            logger.LogError(
                "InternalApi:Key is not configured; refusing to exchange user tokens.");
            return null;
        }

        var cacheKey = BuildCacheKey(userAccessToken, audience);
        if (cache.TryGetValue(cacheKey, out string? cachedToken)
            && !string.IsNullOrWhiteSpace(cachedToken))
        {
            return cachedToken;
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, ExchangePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken);
        request.Headers.Add(InternalApiKeyHeader, _internalApiKey);
        request.Content = JsonContent.Create(new ExchangeTokenRequest(audience));

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Token exchange for audience {Audience} failed with status {StatusCode}.",
                    audience,
                    (int)response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<ExchangeTokenResponse>(
                cancellationToken);
            if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                logger.LogWarning(
                    "Token exchange for audience {Audience} returned an empty token.",
                    audience);
                return null;
            }

            var expiresAt = payload.ExpiresAt.Kind == DateTimeKind.Utc
                ? payload.ExpiresAt
                : payload.ExpiresAt.ToUniversalTime();
            var ttl = expiresAt - DateTime.UtcNow - TimeSpan.FromSeconds(30);
            if (ttl > TimeSpan.Zero)
                cache.Set(cacheKey, payload.AccessToken, ttl);

            return payload.AccessToken;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Token exchange for audience {Audience} failed.", audience);
            return null;
        }
    }

    private static string BuildCacheKey(string userAccessToken, string audience)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(userAccessToken)));
        return $"service-token:{audience}:{hash}";
    }

    internal sealed record ExchangeTokenRequest(string Audience);

    internal sealed record ExchangeTokenResponse(string AccessToken, DateTime ExpiresAt);
}
