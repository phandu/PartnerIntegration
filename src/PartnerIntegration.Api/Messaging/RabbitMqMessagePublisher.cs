using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace PartnerIntegration.Api.Messaging
{
    public sealed class RabbitMqMessagePublisher : IMessagePublisher
    {
        private readonly IConfiguration _configuration;

        public RabbitMqMessagePublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task PublishAsync<T>(       T message,       CancellationToken cancellationToken = default)
        {
            var hostName = _configuration["RabbitMq:HostName"] ?? "localhost";
            var queueName = _configuration["RabbitMq:QueueName"] ?? "partner-transactions";

            var factory = new ConnectionFactory
            {
                HostName = hostName
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body,
                cancellationToken: cancellationToken);
        }
    }
}
