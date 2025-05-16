using System.Text;
using Gateway.Configuration;
using NATS.Client;
using Serilog;
using Polly;
using Polly.Retry;

namespace Gateway.Core;

public class UpdateManager
{
    private readonly IConnection _connection;
    private readonly IAppConfigurationProvider _configProvider;
    private readonly AsyncRetryPolicy _retryPolicy;

    public UpdateManager(IConnection connection, IAppConfigurationProvider configProvider)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        
        _retryPolicy = Policy
            .Handle<NATSNoRespondersException>()
            .WaitAndRetryAsync(
                2,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timespan, retryCount, context) =>
                {
                    Log.Warning(
                        "Retry {RetryCount} for request config after {RetrySeconds}s delay due to no responders",
                        retryCount, timespan.TotalSeconds);
                });
    }

    public async Task RequestConfigAsync(string gatewayId)
    {
        if (_connection == null)
            throw new InvalidOperationException("NATS connection not initialized");

        var gatewayConfigFile = _configProvider.GetConfigFilePath();
        var natsConfig = _configProvider.GetSection<NatsConfiguration>("NatsConfiguration");
        var pullSubject = natsConfig.PullSubject;

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await _connection.RequestAsync(pullSubject, Encoding.UTF8.GetBytes(gatewayId), 5000);
                var gatewayConfig = Encoding.UTF8.GetString(response.Data);

                if (gatewayConfig.Length == 0)
                    throw new Exception("Received an empty config from the server");

                await File.WriteAllTextAsync(gatewayConfigFile, gatewayConfig);

                Log.Information("Gateway : {gatewayId} Requested Config from the server", gatewayId);
                Log.Information("Received Config from the server: {ResponseMessage}", gatewayConfig);
            });
        }
        catch (NATSNoRespondersException ex)
        {
            Log.Warning("No responders available after all retry attempts. Configuration on disk will be used");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during the request to the server.");
            throw;
        }
    }

    public void SubscribeToServerUpdates(string gatewayId)
    {
        if (_connection == null)
            throw new InvalidOperationException("NATS connection not initialized");
            
        var gatewayConfigFile = _configProvider.GetConfigFilePath();
        var pushSubject = $"gateway.{gatewayId}.messages";
        
        _connection.SubscribeAsync(pushSubject, (_, msgArgs) =>
        {
            try
            {
                var gatewayConfig = Encoding.UTF8.GetString(msgArgs.Message.Data);
                Log.Information("Received pushed update from the server: {gatewayConfig}", gatewayConfig);
                File.WriteAllText(gatewayConfigFile, gatewayConfig);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing message from server");
            }
        });

        Log.Information("Subscribed to subject: {pushSubject} to receive pushed updates from the server", pushSubject);
    }
}