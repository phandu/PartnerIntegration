namespace PartnerIntegration.Api.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default);
    }
}
