using System.Net.Http.Json;
using PartnerIntegration.Api.Models;

namespace PartnerIntegration.Api.Services
{
    public class PartnerVerificationService : IPartnerVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PartnerVerificationService> _logger;

        public PartnerVerificationService(
            HttpClient httpClient,
            ILogger<PartnerVerificationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> VerifyAsync(
            string partnerId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/mock/partners/{partnerId}/verify",
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var result =
                    await response.Content.ReadFromJsonAsync<PartnerVerificationResponse>(
                        cancellationToken);

                return result?.IsValid == true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Partner verification failed for {PartnerId}",
                    partnerId);

                return false;
            }
        }
    }
}
