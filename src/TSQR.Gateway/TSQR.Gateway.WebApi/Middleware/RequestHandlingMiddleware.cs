using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TSQR.Gateway.WebApi.Middleware
{
    public class RequestHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Pre-processing logic can be added here

            await _next(context);

            // Post-processing logic can be added here
        }
    }
}