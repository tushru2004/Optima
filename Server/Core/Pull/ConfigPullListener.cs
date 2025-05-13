using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NATS.Client;
using Server.ConfigurationManagement;
using Server.ConfigurationManagement.Elements;

namespace Server.Core.Pull;

public class ConfigPullListener(IConnection connection)
{
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