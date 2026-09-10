using FluentValidation;
using PartnerIntegration.Api.Models;

namespace PartnerIntegration.Api.Validation
{
    public sealed class PartnerTransactionRequestValidator : AbstractValidator<PartnerTransactionRequest>
    {
        private static readonly HashSet<string> SupportedCurrencies =
       new(StringComparer.OrdinalIgnoreCase)
       {
            "USD",
            "EUR",
            "GBP",
            "AUD",
            "JPY",
            "VND"
       };

        public PartnerTransactionRequestValidator()
        {
            RuleFor(x => x.PartnerId)
                .NotEmpty()
                .WithMessage("PartnerId is required.");

            RuleFor(x => x.TransactionReference)
                .NotEmpty()
                .WithMessage("TransactionReference is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency is required.")
                .Must(BeSupportedCurrency)
                .WithMessage("Currency is invalid.");

            RuleFor(x => x.Timestamp)
                .NotEmpty()
                .WithMessage("Timestamp is required.");
        }

        private static bool BeSupportedCurrency(string currency)
        {
            return SupportedCurrencies.Contains(currency);
        }
    }
}
