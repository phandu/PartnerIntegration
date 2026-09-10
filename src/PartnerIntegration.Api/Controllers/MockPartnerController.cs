using Microsoft.AspNetCore.Mvc;

namespace PartnerIntegration.Api.Controllers
{
    [ApiController]
    [Route("api/mock/partners")]
    public class MockPartnerController : ControllerBase
    {
        [HttpGet("{partnerId}/verify")]
        public IActionResult Verify(string partnerId)
        {
            var shouldTimeout = Random.Shared.Next(1, 101) <= 30;

            if (shouldTimeout)
            {
                throw new TimeoutException("Partner verification service timed out.");
            }

            return Ok(new
            {
                partnerId,
                isValid = true
            });
        }
    }
}
