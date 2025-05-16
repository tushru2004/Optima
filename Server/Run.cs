using Microsoft.Extensions.Configuration;
using NATS.Client;
using Server.ConfigurationManagement.Elements;
using Server.Core;
using Server.Nats;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Server;

internal class Run
{
    private static IConnection? _connection;

    private static void InitializeConnection()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true)
            .CreateLogger();
        
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
            Log.Information("Connected to NATS server");
        }
        catch (NATSConnectionException ex)
        {
            Log.Error(ex, "Failed to connect to NATS server");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while connecting to NATS server");
            throw;
        }
    }

    private static async Task Main(string[] args)
    {
        InitializeConnection();
        if (_connection != null)
        {
            var natsManager = new NatsManager(_connection);
            var updateManager = new UpdateManager(natsManager);
            updateManager.ListenForGatewayConfigRequest();
            updateManager.PushUpdates();
        }

        Log.Information("Server is running. Press Ctrl+C to exit...");
        await Task.Delay(Timeout.Infinite);
    }

    private static void DisposeConnection()
    {
        _connection?.Dispose();
        Log.Information("NATS connection disposed.");
    }
}