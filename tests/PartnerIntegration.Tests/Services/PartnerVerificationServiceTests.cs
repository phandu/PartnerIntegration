using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PartnerIntegration.Api.Services;
using PartnerIntegration.Tests.Helpers;

namespace PartnerIntegration.Tests.Services
{
    public sealed class PartnerVerificationServiceTests
    {
        [Fact]
        public async Task VerifyAsync_Should_Return_True_When_Api_Returns_Valid_Partner()
        {
            var json = """
        {
            "partnerId": "P-1001",
            "isValid": true
        }
        """;

            var handler = new SequenceHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };

            var service = new PartnerVerificationService(
                httpClient,
                NullLogger<PartnerVerificationService>.Instance);

            var result = await service.VerifyAsync("P-1001");

            result.Should().BeTrue();
            handler.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task VerifyAsync_Should_Return_False_When_Api_Fails()
        {
            var handler = new SequenceHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };

            var service = new PartnerVerificationService(
                httpClient,
                NullLogger<PartnerVerificationService>.Instance);

            var result = await service.VerifyAsync("P-1001");

            result.Should().BeFalse();
            handler.CallCount.Should().Be(1);
        }
    }
}
