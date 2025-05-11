using NATS.Client;
using System.Text;
using Utf8Json;

namespace Gateway;

internal class Program
{
    private static void Main(string[] args)
    {
        var gatewayId = Environment.GetEnvironmentVariable("GATEWAY_ID")
                        ?? throw new InvalidOperationException("Environment variable 'GATEWAY_ID' is not set.");
        Console.WriteLine("Gateway is subscribing to messages...");

        var options = ConnectionFactory.GetDefaultOptions();
        options.Url = "nats://nats:4222"; // NATS server URL

        using (var connection = new ConnectionFactory().CreateConnection(options))
        {
            var subject = $"gateway.{gatewayId}.messages";
            connection.SubscribeAsync(subject, (sender, msgArgs) =>
            {
                string prettyJson = JsonSerializer.PrettyPrint(msgArgs.Message.Data);
                var receivedMessage = Encoding.UTF8.GetString(msgArgs.Message.Data);
                Console.WriteLine($"Message received on subject '{subject}': {prettyJson}");
            });

            while (true) Thread.Sleep(5000); // Keep the application running
        }
    }
}