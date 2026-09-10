namespace PartnerIntegration.Api.Models
{
    public class PartnerVerificationResponse
    {
        public string PartnerId { get; set; } = string.Empty;

        public bool IsValid { get; set; }
    }
}
