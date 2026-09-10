namespace PartnerIntegration.Api.Services
{
    public interface IPartnerVerificationService
    {
        Task<bool> VerifyAsync(string partnerId, CancellationToken cancellationToken = default);
    }
}
