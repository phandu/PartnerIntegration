using System.Net;

namespace PartnerIntegration.Tests.Helpers
{
    public sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public int CallCount { get; private set; }

        public SequenceHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;

            if (_responses.Count == 0)
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
