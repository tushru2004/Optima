using Microsoft.Extensions.Configuration;
using NATS.Client;
using Server.ConfigurationManagement;
using Server.Core;
using Server.Nats;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Server;

internal class Run
{
    private static IConnection? _connection;
    private static IAppConfigurationProvider? _configProvider;

    private static void InitializeConnection()
    {
        _configProvider = new AppConfigurationProvider();
        var natsConfig = _configProvider.GetSection<ServerNatsConfiguration>("NatsConfiguration")
                         ?? throw new InvalidOperationException(
                             "NatsConfiguration section is missing in appsettings.json");

        var options = ConnectionFactory.GetDefaultOptions();
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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true)
            .CreateLogger();
        AppDomain.CurrentDomain.ProcessExit += (s, e) => DisposeConnection();

        InitializeConnection();
        if (_connection != null && _configProvider != null)
        {
            var natsManager = new NatsManager(_connection);
            var updateManager = new UpdateManager(natsManager);
            updateManager.ListenForGatewayConfigRequest();

            var appConfig = _configProvider.GetSection<AppConfiguration>("AppConfiguration");
            updateManager.PushUpdates(appConfig.AllGatewayConfigFile);
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