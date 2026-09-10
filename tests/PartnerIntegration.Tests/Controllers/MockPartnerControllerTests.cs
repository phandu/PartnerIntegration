using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.Api.Controllers;
using Xunit;

namespace PartnerIntegration.Tests.Controllers
{
    public class MockPartnerControllerTests
    {
        [Fact]
        public void Verify_Should_Eventually_Cover_Success_And_Timeout()
        {
            // Arrange
            var controller = new MockPartnerController();

            var successObserved = false;
            var timeoutObserved = false;

            // Act
            // Probability of not seeing both outcomes after 100 attempts
            // is extremely small.
            for (var i = 0; i < 100; i++)
            {
                try
                {
                    var result = controller.Verify("P-1001");

                    var okResult = Assert.IsType<OkObjectResult>(result);

                    Assert.Equal(200, okResult.StatusCode);

                    successObserved = true;
                }
                catch (TimeoutException ex)
                {
                    Assert.Equal(
                        "Partner verification service timed out.",
                        ex.Message);

                    timeoutObserved = true;
                }

                if (successObserved && timeoutObserved)
                    break;
            }

            // Assert
            Assert.True(
                successObserved,
                "Expected at least one successful response.");

            Assert.True(
                timeoutObserved,
                "Expected at least one timeout.");
        }
    }
}
