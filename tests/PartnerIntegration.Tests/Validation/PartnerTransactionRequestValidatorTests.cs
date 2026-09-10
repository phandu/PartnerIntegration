using FluentAssertions;
using PartnerIntegration.Api.Models;
using PartnerIntegration.Api.Validation;

namespace PartnerIntegration.Tests.Validation
{
    public sealed class PartnerTransactionRequestValidatorTests
    {
        private readonly PartnerTransactionRequestValidator _validator = new();

        [Fact]
        public async Task Should_Be_Valid_When_Request_Is_Correct()
        {
            var request = new PartnerTransactionRequest
            {
                PartnerId = "P-1001",
                TransactionReference = "TXN-99823",
                Amount = 250m,
                Currency = "USD",
                Timestamp = DateTimeOffset.UtcNow
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Should_Be_Invalid_When_Amount_Is_Zero()
        {
            var request = new PartnerTransactionRequest
            {
                PartnerId = "P-1001",
                TransactionReference = "TXN-99823",
                Amount = 0m,
                Currency = "USD",
                Timestamp = DateTimeOffset.UtcNow
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should()
                .Contain(x => x.PropertyName == nameof(request.Amount));
        }

        [Fact]
        public async Task Should_Be_Invalid_When_Currency_Is_Invalid()
        {
            var request = new PartnerTransactionRequest
            {
                PartnerId = "P-1001",
                TransactionReference = "TXN-99823",
                Amount = 250m,
                Currency = "XXX",
                Timestamp = DateTimeOffset.UtcNow
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should()
                .Contain(x => x.PropertyName == nameof(request.Currency));
        }

        [Fact]
        public async Task Should_Be_Invalid_When_Required_Fields_Are_Missing()
        {
            var request = new PartnerTransactionRequest
            {
                PartnerId = "",
                TransactionReference = "",
                Amount = 100m,
                Currency = "",
                Timestamp = default
            };

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();

            result.Errors.Should()
                .Contain(x => x.PropertyName == nameof(request.PartnerId));

            result.Errors.Should()
                .Contain(x => x.PropertyName == nameof(request.TransactionReference));

            result.Errors.Should()
                .Contain(x => x.PropertyName == nameof(request.Currency));

            result.Errors.Should()
                .Contain(x => x.PropertyName == nameof(request.Timestamp));
        }
    }
}
