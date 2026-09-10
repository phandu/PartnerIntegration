using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using PartnerIntegration.Tests.Helpers;
using Polly;

namespace PartnerIntegration.Tests.Resilience
{
    public sealed class PartnerVerificationRetryTests
    {
        [Fact]
        public async Task Should_Retry_When_First_Request_Fails_And_Second_Succeeds()
        {
            var handler = new SequenceHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError),
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                    {
                        "partnerId": "P-1001",
                        "isValid": true
                    }
                    """,
                        Encoding.UTF8,
                        "application/json")
                });

            var services = new ServiceCollection();

            services
                .AddHttpClient("partner-verification", client =>
                {
                    client.BaseAddress = new Uri("http://localhost/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddResilienceHandler("retry-policy", pipeline =>
                {
                    pipeline.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromMilliseconds(10),
                        BackoffType = DelayBackoffType.Constant
                    });
                });

            await using var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("partner-verification");

            var response = await client.GetAsync(
                "api/mock/partners/P-1001/verify");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            handler.CallCount.Should().Be(2);
        }
    }
}
