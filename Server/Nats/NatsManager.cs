using System.Text;
using System.Text.Json;
using NATS.Client;
using Serilog;
using Server.ConfigurationManagement;

namespace Server.Nats;

public class NatsManager(IConnection connection)
{
    private readonly IConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public void Publish()
    {
        var allGatewayConfigs = GatewayConfig.GetAll();
        foreach (var gateway in allGatewayConfigs)
        {
            var gatewayId = gateway.GatewayId;
            var subject = $"gateway.{gatewayId}.messages";
            var message = JsonSerializer.Serialize(gateway);

            Log.Information("Attempting to publish message to gateway '{GatewayId}'", gatewayId);

            try
            {
                _connection.Publish(subject, Encoding.UTF8.GetBytes(message));
                Log.Information("Message successfully published to gateway '{GatewayId}'", gatewayId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to publish to gateway '{GatewayId}'", gatewayId);
                throw new InvalidOperationException("Error occurred while publishing message to NATS server.", ex);
            }
        }
    }

    public void ListenForGatewayConfigRequest()
    {
        try
        {
            const string subject = "gateway.config.pull";
            connection.SubscribeAsync(subject, (_, args) =>
            {
                var requestMessage = Encoding.UTF8.GetString(args.Message.Data);
                Log.Information("Received request: {RequestMessage}", requestMessage);
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
                    var responseMessage = $"Response to: {requestMessage}";
                    connection.Publish(args.Message.Reply, Encoding.UTF8.GetBytes(configJson));
                    Log.Information("Sent response: {ResponseMessage}", responseMessage);
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