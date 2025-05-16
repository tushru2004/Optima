using System.Text;
using Gateway.Configuration;
using NATS.Client;
using Serilog;

namespace Gateway.Core;

public class UpdateManager
{
    private readonly IConnection _connection;
    private readonly IAppConfigurationProvider _configProvider;

    public UpdateManager(IConnection connection, IAppConfigurationProvider configProvider)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
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
            var response = await _connection.RequestAsync(pullSubject, Encoding.UTF8.GetBytes(gatewayId), 5000);
            var gatewayConfig = Encoding.UTF8.GetString(response.Data);
            
            if (gatewayConfig.Length == 0) 
                throw new Exception("Received an empty config from the server");
                
            await File.WriteAllTextAsync(gatewayConfigFile, gatewayConfig);
            
            Log.Information("Gateway : {gatewayId} Requested Config from the server", gatewayId);
            Log.Information("Received Config from the server: {ResponseMessage}", gatewayConfig);
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
        var gatewayConfigFile = Environment.GetEnvironmentVariable("CONFIG_FILE")
                                ?? throw new InvalidOperationException("Environment variable 'CONFIG_FILE' is not set.");

        var pushSubject = $"gateway.{gatewayId}.messages";
        _connection.SubscribeAsync(pushSubject, (_, msgArgs) =>
        {
            var gatewayConfig = Encoding.UTF8.GetString(msgArgs.Message.Data);
            Log.Information("Received pushed update from the server: {gatewayConfig}", gatewayConfig);
            File.WriteAllText(gatewayConfigFile, gatewayConfig);
        });

        Log.Information("Subscribed to subject: {pushSubject} to receive pushed updates from the server", pushSubject);
    }
}