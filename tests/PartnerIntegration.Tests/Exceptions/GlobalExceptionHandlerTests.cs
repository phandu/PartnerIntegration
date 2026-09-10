using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using PartnerIntegration.Api.Exceptions;
using Xunit;

namespace PartnerIntegration.Tests.Exceptions
{
    public class GlobalExceptionHandlerTests
    {
        [Fact]
        public async Task TryHandleAsync_Should_Return_504_For_TimeoutException()
        {
            // Arrange
            var logger = new Mock<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(logger.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/api/v1/partner/transactions";
            context.TraceIdentifier = "TRACE-001";

            var exception = new TimeoutException("Partner timed out.");

            // Act
            var handled = await handler.TryHandleAsync(
                context,
                exception,
                CancellationToken.None);

            // Assert
            Assert.True(handled);
            Assert.Equal(
                StatusCodes.Status504GatewayTimeout,
                context.Response.StatusCode);

            context.Response.Body.Position = 0;

            using var document =
                await JsonDocument.ParseAsync(context.Response.Body);

            var root = document.RootElement;

            Assert.Equal(
                504,
                root.GetProperty("status").GetInt32());

            Assert.Equal(
                "Request timeout",
                root.GetProperty("title").GetString());

            Assert.Equal(
                "The downstream service did not respond in time.",
                root.GetProperty("detail").GetString());

            Assert.Equal(
                "/api/v1/partner/transactions",
                root.GetProperty("instance").GetString());

            Assert.Equal(
                "TRACE-001",
                root.GetProperty("traceId").GetString());
        }

        [Fact]
        public async Task TryHandleAsync_Should_Return_500_For_Unhandled_Exception()
        {
            // Arrange
            var logger = new Mock<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(logger.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/api/v1/partner/transactions";
            context.TraceIdentifier = "TRACE-002";

            var exception = new InvalidOperationException(
                "Unexpected error.");

            // Act
            var handled = await handler.TryHandleAsync(
                context,
                exception,
                CancellationToken.None);

            // Assert
            Assert.True(handled);
            Assert.Equal(
                StatusCodes.Status500InternalServerError,
                context.Response.StatusCode);

            context.Response.Body.Position = 0;

            using var document =
                await JsonDocument.ParseAsync(context.Response.Body);

            var root = document.RootElement;

            Assert.Equal(
                500,
                root.GetProperty("status").GetInt32());

            Assert.Equal(
                "Internal server error",
                root.GetProperty("title").GetString());

            Assert.Equal(
                "An unexpected error occurred.",
                root.GetProperty("detail").GetString());

            Assert.Equal(
                "/api/v1/partner/transactions",
                root.GetProperty("instance").GetString());

            Assert.Equal(
                "TRACE-002",
                root.GetProperty("traceId").GetString());
        }
    }
}
