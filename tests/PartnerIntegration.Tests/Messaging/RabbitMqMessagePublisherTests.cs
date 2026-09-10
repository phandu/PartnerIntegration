using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PartnerIntegration.Api.Messaging;
using RabbitMQ.Client;
using Xunit;

namespace PartnerIntegration.Tests.Messaging
{
    public class RabbitMqMessagePublisherTests
    {
        [Fact]
        public async Task PublishAsync_Should_Publish_Message_To_RabbitMq()
        {
            // Arrange
            var queueName = $"test-partner-transactions-{Guid.NewGuid():N}";

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RabbitMq:HostName"] = "localhost",
                    ["RabbitMq:QueueName"] = queueName
                })
                .Build();

            var publisher = new RabbitMqMessagePublisher(configuration);

            var message = new
            {
                PartnerId = "P-1001",
                TransactionReference = "TXN-TEST-001",
                Amount = 250.00m,
                Currency = "USD"
            };

            // Act
            await publisher.PublishAsync(message);

            // Assert
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            var result = await channel.BasicGetAsync(
                queue: queueName,
                autoAck: true);

            Assert.NotNull(result);

            var json = Encoding.UTF8.GetString(result.Body.ToArray());

            using var document = JsonDocument.Parse(json);

            Assert.Equal(
                "P-1001",
                document.RootElement.GetProperty("PartnerId").GetString());

            Assert.Equal(
                "TXN-TEST-001",
                document.RootElement
                    .GetProperty("TransactionReference")
                    .GetString());

            // Cleanup
            await channel.QueueDeleteAsync(queueName);
        }
    }
}
