using NATS.Client;
using System.Text;
using Gateway.Core;
using Microsoft.Extensions.Configuration;
using Utf8Json;
namespace Gateway;

internal class Run
{
    private static IConnection? _connection;

    private static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => DisposeConnection();
        var gatewayId = Environment.GetEnvironmentVariable("GATEWAY_ID")
                        ?? throw new InvalidOperationException("Environment variable 'GATEWAY_ID' is not set.");
        Console.WriteLine("Gateway is subscribing to messages...");
        if (File.Exists("response.json"))
        {
            var jsonContent = await File.ReadAllTextAsync("response.json");
            Console.WriteLine($"JSON Content: {jsonContent}");
        }
        else
        {
            Console.WriteLine("File 'response.json' does not exist.");
        }
        InitializeConnection();
        if (_connection != null)
        {
            var updateManager = new UpdateManager(_connection);
            await updateManager.RequestConfigAsync(gatewayId);
        }

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
        _connection = new ConnectionFactory().CreateConnection(options);
        Console.WriteLine("Connected to NATS server");
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