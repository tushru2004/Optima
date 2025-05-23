﻿using NATS.Client;
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

        var gatewayId = Environment.GetEnvironmentVariable("GATEWAY_ID")
                        ?? throw new InvalidOperationException("Environment variable 'GATEWAY_ID' is not set.");

        var configProvider = new AppConfigurationProvider();
        var gatewayConfigFile = configProvider.GetConfigFilePath();

        Log.Information("Starting gateway application... for gateway id {gatewayId}", gatewayId);

        var fileExists = File.Exists(gatewayConfigFile);
        var content = fileExists ? await File.ReadAllTextAsync(gatewayConfigFile) : string.Empty;

        if (content.Length == 0) Log.Warning("Configuration file {gatewayConfigFile} is empty ", gatewayConfigFile);
        Log.Information(
            "Gateway configuration file {FilePath} {FileStatus}. {FileContent}",
            gatewayConfigFile,
            fileExists ? "exists" : "does not exist",
            fileExists ? $"Content: {content}" : string.Empty
        );
        var serverNatsManager = new GatewayNatsManager();
        var connection = serverNatsManager.InitializeConnection(configProvider);
        AppDomain.CurrentDomain.ProcessExit += (s, e) => serverNatsManager.DisposeConnection();

        var updateManager = new UpdateManager(connection ?? throw new InvalidOperationException(), configProvider);
        // This will request config from the server. PULL
        await updateManager.RequestConfigAsync(gatewayId);
        // This will subscribe to server updates. For the use case where the server pushes updates
        updateManager.SubscribeToServerUpdates(gatewayId);
        await Task.Delay(Timeout.Infinite);
    }
}