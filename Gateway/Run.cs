using NATS.Client;
using Gateway.Core;
using Gateway.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Gateway;

internal class Run
{
    private static IConnection? _connection;
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true)
            .CreateLogger();
        AppDomain.CurrentDomain.ProcessExit += (s, e) => DisposeConnection();

        var gatewayId = Environment.GetEnvironmentVariable("GATEWAY_ID")
                        ?? throw new InvalidOperationException("Environment variable 'GATEWAY_ID' is not set.");
                        
        var configProvider = new AppConfigurationProvider();
        var gatewayConfigFile = configProvider.GetConfigFilePath();
        
        Log.Information("Starting gateway application... for gateway id {gatewayId}", gatewayId);

        bool fileExists = File.Exists(gatewayConfigFile);
        string content = fileExists ? await File.ReadAllTextAsync(gatewayConfigFile) : string.Empty;

        if (content.Length == 0){
            Log.Warning("Configuration file {gatewayConfigFile} is empty ", gatewayConfigFile);
        }
        Log.Information(
            "Gateway configuration file {FilePath} {FileStatus}. {FileContent}", 
            gatewayConfigFile,
            fileExists ? "exists" : "does not exist",
            fileExists ? $"Content: {content}" : string.Empty
        );
                
        InitializeConnection(configProvider);
        if (_connection != null)
        {
            var updateManager = new UpdateManager(_connection, configProvider);
            await updateManager.RequestConfigAsync(gatewayId);
            updateManager.SubscribeToServerUpdates(gatewayId);
            
            await Task.Delay(Timeout.Infinite);
        }
        else
        {
            Log.Error("Failed to establish NATS connection. Exiting...");
        }
    }

    private static void InitializeConnection(IAppConfigurationProvider configProvider)
    {
        var options = ConnectionFactory.GetDefaultOptions();
        var natsConfig = configProvider.GetSection<NatsConfiguration>("NatsConfiguration");
        options.Url = natsConfig.Url;
        try
        {
            _connection = new ConnectionFactory().CreateConnection(options);
            Log.Information("Connected to NATS server");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to NATS server: {ErrorMessage}", ex.Message);
        }
    }

    private static void DisposeConnection()
    {
        _connection?.Dispose();
        Log.Information("NATS connection disposed.");
    }
}