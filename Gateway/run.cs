using NATS.Client;
using System.Text;
using Microsoft.Extensions.Configuration;
using Utf8Json;
namespace Gateway;

internal class run
{
    private static IConnection? _connection;

    private static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => DisposeConnection();
        var gatewayId = Environment.GetEnvironmentVariable("GATEWAY_ID")
                        ?? throw new InvalidOperationException("Environment variable 'GATEWAY_ID' is not set.");
        Console.WriteLine("Gateway is subscribing to messages...");
        
        InitializeConnection();
        await RequestConfigAsync(gatewayId);
        SubscribeToServerUpdates(gatewayId);
        await Task.Delay(Timeout.Infinite); // Keep the application running
    }

    private static void InitializeConnection()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build();
        var options = ConnectionFactory.GetDefaultOptions();
        var natsConfig = configuration.GetSection("NatsConfiguration").Get<NatsConfiguration>()
                         ?? throw new InvalidOperationException(
                             "NatsConfiguration section is missing in appsettings.json");
        options.Url = natsConfig.Url;

        options.DisconnectedEventHandler = (_, _) => Console.WriteLine("Disconnected from NATS server");
        options.ReconnectedEventHandler = (_, _) => Console.WriteLine("Reconnected to NATS server");
        options.ClosedEventHandler = (_, _) => Console.WriteLine("NATS connection closed");

        _connection = new ConnectionFactory().CreateConnection(options);
        Console.WriteLine("Connected to NATS server");
    }

    private static async Task RequestConfigAsync(string message)
    {
        if (_connection == null)
            throw new InvalidOperationException("NATS connection not initialized");

        const string subject = "gateway.config.pull";
        try
        {
            var response = await _connection.RequestAsync(subject, Encoding.UTF8.GetBytes(message), 5000); // 5-second timeout
            var responseMessage = Encoding.UTF8.GetString(response.Data);

            Console.WriteLine($"Sent request: {message}");
            Console.WriteLine($"Received response: {responseMessage}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send request: {ex.Message}");
        }
    }

    private static void SubscribeToServerUpdates(string gatewayId)
    {
        if (_connection == null)
            throw new InvalidOperationException("NATS connection not initialized");

        var subject = $"gateway.{gatewayId}.messages";

        _connection.SubscribeAsync(subject, (_, msgArgs) =>
        {
            var message = Encoding.UTF8.GetString(msgArgs.Message.Data);
            Console.WriteLine($"Message received on subject '{subject}': {message}");

            try
            {
                var formattedJson = JsonSerializer.PrettyPrint(msgArgs.Message.Data);
                Console.WriteLine($"Formatted JSON: {formattedJson}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to format message: {ex.Message}");
            }
        });

        Console.WriteLine($"Subscribed to subject: {subject}");
    }

    private static void DisposeConnection()
    {
        _connection?.Dispose();
        Console.WriteLine("NATS connection disposed.");
    }
}