namespace TSQR.Gateway.Api.Security;

public interface IServiceTokenExchange
{
    /// <summary>
    /// Exchanges a user access token for a short-lived token scoped to
    /// <paramref name="audience"/>. Returns <c>null</c> when the exchange
    /// cannot be completed; callers must fail closed.
    /// </summary>
    Task<string?> ExchangeAsync(
        string userAccessToken,
        string audience,
        CancellationToken cancellationToken = default);
}
