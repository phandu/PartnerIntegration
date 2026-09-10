using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PartnerIntegration.Api.Controllers;
using PartnerIntegration.Api.Messaging;
using PartnerIntegration.Api.Models;
using PartnerIntegration.Api.Services;
namespace PartnerIntegration.Tests.Controllers
{
    public sealed class PartnerTransactionControllerTests
    {
        private readonly Mock<IPartnerVerificationService> _verificationService = new();
        private readonly Mock<IMessagePublisher> _publisher = new();

        private PartnerTransactionsController CreateController()
        {
            return new PartnerTransactionsController(
                _verificationService.Object,
                _publisher.Object);
        }

        private static PartnerTransactionRequest CreateRequest()
        {
            return new PartnerTransactionRequest
            {
                PartnerId = "P-1001",
                TransactionReference = "TXN-TEST-001",
                Amount = 250m,
                Currency = "USD",
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        [Fact]
        public async Task CreateTransaction_Should_Return_Accepted_When_Partner_Is_Valid()
        {
            var request = CreateRequest();

            _verificationService
                .Setup(x => x.VerifyAsync(
                    request.PartnerId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var controller = CreateController();

            var result = await controller.CreateTransaction(
                request,
                CancellationToken.None);

            var acceptedResult = Assert.IsType<AcceptedResult>(result);

            Assert.Equal(
                StatusCodes.Status202Accepted,
                acceptedResult.StatusCode);

            _publisher.Verify(
                x => x.PublishAsync(
                    request,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateTransaction_Should_Return_503_When_Partner_Verification_Fails()
        {
            var request = CreateRequest();

            _verificationService
                .Setup(x => x.VerifyAsync(
                    request.PartnerId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var controller = CreateController();

            var result = await controller.CreateTransaction(
                request,
                CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status503ServiceUnavailable,
                objectResult.StatusCode);

            _publisher.Verify(
                x => x.PublishAsync(
                    It.IsAny<PartnerTransactionRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
