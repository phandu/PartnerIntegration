using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PartnerIntegration.Api.Exceptions
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            var statusCode = exception switch
            {
                TimeoutException => StatusCodes.Status504GatewayTimeout,
                _ => StatusCodes.Status500InternalServerError
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode == StatusCodes.Status504GatewayTimeout
                    ? "Request timeout"
                    : "Internal server error",
                Detail = statusCode == StatusCodes.Status504GatewayTimeout
                    ? "The downstream service did not respond in time."
                    : "An unexpected error occurred.",
                Instance = httpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] =
                httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken);

            return true;
        }
    }
}
