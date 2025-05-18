using Gateway.Configuration;
using NATS.Client;
using Serilog;

namespace Gateway.Core;

public class GatewayNatsManager
{
    private static IConnection? _connection;
    internal void DisposeConnection() {
        _connection?.Dispose();
        Log.Information("NATS connection disposed.");
    }

    internal IConnection? InitializeConnection(IAppConfigurationProvider configProvider) {
        var options = ConnectionFactory.GetDefaultOptions();
        var natsConfig = configProvider.GetSection<NatsConfiguration>("NatsConfiguration");
        options.Url = natsConfig.Url;
        options.MaxReconnect = natsConfig.MaxReconnect;
        options.ReconnectWait = natsConfig.ReconnectWait;
        options.AllowReconnect = natsConfig.AllowReconnect;
    
        options.DisconnectedEventHandler = (sender, args) => {
            Log.Warning("Disconnected from NATS server: {Reason}", args.Error?.Message ?? "unknown reason");
            if (!args.Conn.IsClosed()) {
                Log.Information("NATS client will attempt automatic reconnection");
                return;
            }
            Log.Information("Connection is closed, attempting manual reconnection");
            Task.Run(() => {
                _connection = TryConnectWithRetry(options, natsConfig.MaxReconnectAttempts, natsConfig.ReconnectIntervalMs, isReconnect: true);
            });
        };
    
        options.ReconnectedEventHandler = (sender, args) => {
            Log.Information("Reconnected to NATS server: {ServerUrl}", args.Conn.ConnectedUrl);
        };
    
        options.ClosedEventHandler = (sender, args) => {
            Log.Warning("NATS connection closed: {Reason}", args.Error?.Message ?? "normal closure");
        };
        return TryConnectWithRetry(options,natsConfig.MaxInitialRetries, natsConfig.InitialRetryIntervalMs, isReconnect: false);
    }

    private IConnection? TryConnectWithRetry(Options options, int maxRetries, int retryIntervalMs, bool isReconnect) {
        string actionName = isReconnect ? "reconnect" : "connect";
        for (int attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                if (attempt > 1 || isReconnect) {
                    Log.Information("Attempt {Attempt} of {MaxRetries} to {Action} to NATS server at {Url}", 
                        attempt, maxRetries, actionName, options.Url);
                }
            
                var connection = new ConnectionFactory().CreateConnection(options);
                Log.Information("Successfully {Action}ed to NATS server at {Url}", 
                    actionName, options.Url);
                return connection;
            }
            catch (Exception ex) {
                if (attempt == maxRetries) {
                    Log.Error(ex, "Failed to {Action} to NATS server after {MaxRetries} attempts", 
                        actionName, maxRetries);
                    if (!isReconnect) {
                        throw;
                    }
                    return null;
                }
                Log.Warning("Failed to {Action} to NATS server: {ErrorMessage}. Retrying in {RetryInterval} seconds...", 
                    actionName, ex.Message, retryIntervalMs / 1000);
            
                Thread.Sleep(retryIntervalMs);
            }
        }
        return null;
    }
}