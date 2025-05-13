using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NATS.Client;
using Server.ConfigurationManagement;
using Server.ConfigurationManagement.Elements;

namespace Server.Nats;

public class NatsManager(IConnection connection)
{
    private readonly IConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public void Publish()
    {
        var allGatewayConfigs = GatewayConfig.GetAll();
        foreach (var gateway in allGatewayConfigs)
        {
            var gatewayId = gateway.GatewayId;
            var subject = $"gateway.{gatewayId}.messages";
            var message = JsonSerializer.Serialize(gateway);

            Console.WriteLine($"Attempting to publish message to gateway '{gatewayId}'");

            try
            {
                _connection.Publish(subject, Encoding.UTF8.GetBytes(message));
                Console.WriteLine($"Message successfully published to gateway '{gatewayId}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to publish to gateway '{gatewayId}'");
                throw new InvalidOperationException("Error occurred while publishing message to NATS server.", ex);
            }

        }
    }

    public void ListenForGatewayConfigRequest()
    {
        try
        {
            const string subject = "gateway.config.pull";
            connection.SubscribeAsync(subject, (_, args) =>
            {
                var requestMessage = Encoding.UTF8.GetString(args.Message.Data);
                Console.WriteLine($"Received request: {requestMessage}");
                var gatewayId = requestMessage;
                var config = GatewayConfig.GetById(gatewayId);

                if (config != null && !string.IsNullOrEmpty(args.Message.Reply))
                {
                    var configJson = JsonSerializer.Serialize(config);
                    var responseMessage = $"Response to: {requestMessage}";
                    connection.Publish(args.Message.Reply, Encoding.UTF8.GetBytes(configJson));
                    Console.WriteLine($"Sent response: {responseMessage}");
                }
                else
                {
                    Console.WriteLine("No ReplyTo subject found in the request.");
                }
            });
        }
        catch (NATSConnectionException ex)
        {
            Console.WriteLine($"Failed to connect to the NATS server: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            throw;
        }
    }

}