using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NATS.Client;
using Server.ConfigurationManagement;
using Server.ConfigurationManagement.Elements;

namespace Server.Nats;

public class NatsManager
{
    private readonly IConnection _connection;
    private readonly Options _options;
    public NatsManager(IConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

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

    public void ListenToGatewayConfigRequest(Action<string>? onRequestReceived)
    {
        try
        {
            const string subject = "help.request";
            _connection.SubscribeAsync(subject, (_, args) =>
            {
                var requestMessage = Encoding.UTF8.GetString(args.Message.Data);
                Console.WriteLine($"Received request: {requestMessage}");
                onRequestReceived?.Invoke(requestMessage);
            });
            // using var connection = new ConnectionFactory().CreateConnection(_options);
            // const string subject = "help.request";

            // connection.SubscribeAsync(subject, (_, args) =>
            // {
            //     try
            //     {
            //         var requestMessage = Encoding.UTF8.GetString(args.Message.Data);
            //         Console.WriteLine($"Received request: {requestMessage}");
            //         onRequestReceived?.Invoke(requestMessage);
            //     }
            //     catch (Exception ex)
            //     {
            //         Console.WriteLine($"Error processing the request message: {ex.Message}");
            //         throw;
            //     }
            // });
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