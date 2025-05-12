using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NATS.Client;
using Server.ConfigurationManagement;
using Server.ConfigurationManagement.Elements;

namespace Server.Core.Pull;

public class ConfigPullListener
{
    private static IConnection? _connection;

    public static void ListenForGatewayConfigRequest()
    {
        try
        {
            var options = ConnectionFactory.GetDefaultOptions();
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();
            var natsConfig = configuration.GetSection("NatsConfiguration").Get<NatsConfiguration>()
                             ?? throw new InvalidOperationException(
                                 "NatsConfiguration section is missing in appsettings.json");
            options.Url = natsConfig.Url;
            _connection = new ConnectionFactory().CreateConnection(options);
            const string subject = "gateway.config.pull";

            _connection.SubscribeAsync(subject, (_, args) =>
            {
                var requestMessage = Encoding.UTF8.GetString(args.Message.Data);
                Console.WriteLine($"Received request: {requestMessage}");
                var gatewayId = requestMessage;
                var config = GatewayConfig.GetById(gatewayId);

                if (config != null && !string.IsNullOrEmpty(args.Message.Reply))
                {
                    var configJson = JsonSerializer.Serialize(config);
                    var responseMessage = $"Response to: {requestMessage}";
                    _connection.Publish(args.Message.Reply, Encoding.UTF8.GetBytes(configJson));
                    Console.WriteLine($"Sent response: {responseMessage}");
                }
                else
                {
                    Console.WriteLine("No ReplyTo subject found in the request.");
                }
            });

            Console.WriteLine("Listening for requests...");
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

    public static void DisposeConnection()
    {
        _connection?.Dispose();
        Console.WriteLine("NATS connection disposed.");
    }
}