using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NATS.Client;
using NATS.Client.JetStream;
using Server.Util;
using Server.Util.Models;

namespace Server.Nats;

public class NatsHelper
{
    public void PushConfigs()
    { 
        Console.WriteLine($"Exdec poush roginc");

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build();

        var options = ConnectionFactory.GetDefaultOptions();
        var natsConfig = configuration.GetSection("NatsConfiguration").Get<NatsConfiguration>()
                         ?? throw new InvalidOperationException(
                             "NatsConfiguration section is missing in appsettings.json");

       var allGatewayConfigs =  GatewayConfig.GetAll();
       options.Url = natsConfig.Url;

       Console.WriteLine($"Trying Message published to subject ");

       foreach (var gateway in allGatewayConfigs)
       {
           var gatewayId = gateway.GatewayId;
           try
           {
               using var connection = new ConnectionFactory().CreateConnection(options);
               var subject = $"gateway.{gatewayId}.messages";
               var message = JsonSerializer.Serialize(gateway);

               Console.WriteLine($"Attempting to publish message to gateway '{gatewayId}'");

               try
               {
                   connection.Publish(subject, Encoding.UTF8.GetBytes(message));
                   Console.WriteLine($"Message successfully published to gateway '{gatewayId}'");
               }
               catch (Exception ex)
               {
                   Console.WriteLine($"Failed to publish to gateway '{gatewayId}'");
                   throw new InvalidOperationException("Error occurred while publishing message to NATS server.", ex);
               }
           }
           catch (NATSConnectionException ex)
           {
               Console.WriteLine($"Failed to connect to the NATS server: {ex.Message}");
               throw; 
           }
           catch (Exception ex)
           {
               Console.WriteLine($"Unexpected error while creating NATS connection: {ex.Message}");
               throw;
           }

       }


    }
}