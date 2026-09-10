using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.Api.Messaging;
using PartnerIntegration.Api.Models;
using PartnerIntegration.Api.Services;


namespace PartnerIntegration.Api.Controllers
{
    [ApiController]
    [Route("api/v1/partner/transactions")]
    public class PartnerTransactionsController : ControllerBase
    {
        private readonly IPartnerVerificationService _partnerVerificationService;
        private readonly IMessagePublisher _messagePublisher;

        public PartnerTransactionsController(     IPartnerVerificationService partnerVerificationService, IMessagePublisher messagePublisher)
        {
            _partnerVerificationService = partnerVerificationService;
            _messagePublisher = messagePublisher;
        }
        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] PartnerTransactionRequest request, CancellationToken cancellationToken)
        {
            var isValidPartner = await _partnerVerificationService.VerifyAsync(request.PartnerId, cancellationToken);

            if (!isValidPartner)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        message = "Partner verification is currently unavailable."
                    });
            }
            await _messagePublisher.PublishAsync(request, cancellationToken);
            return Accepted(new
            {
                message = "Transaction accepted.",
                transactionReference = request.TransactionReference
            });
           
        }
    }
}
