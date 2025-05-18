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
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true)
            .CreateLogger();
        var configProvider = new AppConfigurationProvider();

        var serverNatsConfigManager = new ServerNatsConnectionManager();
        AppDomain.CurrentDomain.ProcessExit += (s, e) => serverNatsConfigManager.DisposeConnection();

        try
        {
            var connection = serverNatsConfigManager.InitializeConnection(configProvider);

            var natsManager = new NatsManager(connection ?? throw new InvalidOperationException(), configProvider);
            var updateManager = new UpdateManager(natsManager);
            updateManager.ListenForGatewayConfigRequest();

            var appConfig = configProvider.GetSection<AppConfiguration>("AppConfiguration");
            updateManager.PushUpdates(appConfig.AllGatewayConfigFile);
        }
        catch (Exception ex)
        {   
            Log.Error(ex, " Internal Server Error.");
            return;
        }

        Log.Information("Server is running. Press Ctrl+C to exit...");
        await Task.Delay(Timeout.Infinite);
    }
}