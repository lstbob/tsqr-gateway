namespace TSQR.Gateway.Application.Interfaces
{
    public interface IGatewayService
    {
        Task<TResponse> RouteRequest<TResponse>(RequestModel request);
        Task<TResponse> AggregateResponses<TResponse>(IEnumerable<TResponse> responses);
    }
}