using System.Text;
using NATS.Client;
using Newtonsoft.Json;
using Serilog;
using Server.ConfigurationManagement;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Server.Nats;

public class NatsManager(IConnection connection)
{
    private readonly IConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public void Publish()
    {
        try
        {
            var allGatewayConfigs = GatewayConfig.GetAll();
            foreach (var gateway in allGatewayConfigs)
            {
                PublishGatewayConfig(gateway);
            }
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "JSON serialization error during gateway config publishing");
            throw new InvalidOperationException("Failed to serialize gateway configuration.", ex);
        }
        catch (NATSConnectionException ex)
        {
            Log.Error(ex, "NATS connection error during gateway config publishing");
            throw new InvalidOperationException("Failed to connect to NATS server.", ex);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during gateway config publishing");
            throw new InvalidOperationException("Error occurred while publishing message to NATS server.", ex);
        }
    }

    private void PublishGatewayConfig(GatewayConfig gateway)
    {
        var gatewayId = gateway.GatewayId;
        var subject = $"gateway.{gatewayId}.messages";
        
        Log.Information("Attempting to publish message to gateway '{GatewayId}'", gatewayId);
        
        // Validate before attempting to publish
        GatewayConfigValidator.Validate(gateway, out var errors);
        if (errors.Count > 0)
        {
            var errorMessage = $"Validation errors for gateway '{gatewayId}': {string.Join(", ", errors)}";
            Log.Warning(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var message = JsonSerializer.Serialize(gateway);
        _connection.Publish(subject, Encoding.UTF8.GetBytes(message));
        Log.Information("Message successfully published to gateway '{GatewayId}'", gatewayId);
    }

    public void ListenForGatewayConfigRequest()
    {
        try
        {
            const string subject = "gateway.config.pull";
            connection.SubscribeAsync(subject, (_, args) =>
            {
                var requestMessage = Encoding.UTF8.GetString(args.Message.Data);
                Log.Information("Received request from Gateway Id : {RequestMessage}", requestMessage);
                var gatewayId = requestMessage;
                var config = GatewayConfig.GetById(gatewayId);

                if (config == null)
                {
                    Log.Warning("No configuration found for gateway ID: {GatewayId}", gatewayId);
                    return;
                }

                if (!string.IsNullOrEmpty(args.Message.Reply))
                {
                    var configJson = JsonSerializer.Serialize(config);
                    var responseMessage = $"Response to Gateway Id : {requestMessage}";
                    connection.Publish(args.Message.Reply, Encoding.UTF8.GetBytes(configJson));
                    Log.Information("Sent Config/{ResponseMessage}", responseMessage);
                }
                else
                {
                    throw new InvalidOperationException("No ReplyTo subject found in the request.");
                }
            });
        }
        catch (NATSConnectionException ex)
        {
            Log.Error(ex, "Failed to connect to the NATS server: {ErrorMessage}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}