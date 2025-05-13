using Microsoft.Extensions.Configuration;
using NATS.Client;
using Server.ConfigurationManagement.Elements;
using Server.Core.Pull;
using Server.Core.Push;
using Server.Nats;

namespace Server;

internal class Run
{
    private static IConnection? _connection;

    private static void InitializeConnection()
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => DisposeConnection();
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build();
        var options = ConnectionFactory.GetDefaultOptions();
        var natsConfig = configuration.GetSection("NatsConfiguration").Get<NatsConfiguration>()
                         ?? throw new InvalidOperationException(
                             "NatsConfiguration section is missing in appsettings.json");
        options.Url = natsConfig.Url;
        try
        {
            _connection = new ConnectionFactory().CreateConnection(options);
            Console.WriteLine("Connected to NATS server");
        }
        catch (NATSConnectionException ex)
        {
            Console.WriteLine($"Failed to connect to NATS server: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error while connecting to NATS server: {ex.Message}");
            throw;
        }
    }

    private static Task Main(string[] args)
    {
        InitializeConnection();
        if (_connection != null)
        {
            var natsManager = new NatsManager(_connection);
            new UpdateManager(natsManager).PushUpdates();
            new ConfigPullListener(_connection).ListenForGatewayConfigRequest();
        }

        //NatsManager natsManager = new NatsManager();

        // ConfigPuller.ListenForGatewayConfigRequest();
        //
        // Console.WriteLine("Server is running. Press Ctrl+C to exit...");
        //
        // while (true)
        // {
        //     Thread.Sleep(1000);
        // }
        // var options = ConnectionFactory.GetDefaultOptions();
        // options.Url = "nats://nats:4222"; // NATS server URL
        //
        // using var connection = new ConnectionFactory().CreateConnection(options);
        // const string subject = "help.request";
        //
        // connection.SubscribeAsync(subject, (_, args) =>
        // {
        //     var requestMessage = Encoding.UTF8.GetString(args.Message.Data);
        //     Console.WriteLine($"Received request: {requestMessage}");
        // });
        Console.WriteLine("Server is running. Press Ctrl+C to exit...");
        while (true)
        {
            Thread.Sleep(1000);
        }
    }
    private static void DisposeConnection()
    {
        _connection?.Dispose();
        Console.WriteLine("NATS connection disposed.");
    }
}