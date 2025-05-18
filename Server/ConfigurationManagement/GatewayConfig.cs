using System.Text.Json;
using System.Text.Json.Serialization;
using Server.ConfigurationManagement.Elements;

namespace Server.ConfigurationManagement;

public class GatewayConfig
{
    [JsonPropertyName("gateway_id")] public required string GatewayId { get; set; }

    [JsonPropertyName("gateway_name")] public required string GatewayName { get; set; }

    [JsonPropertyName("facility_name")] public required string FacilityName { get; set; }

    public required Devices Devices { get; set; }

    public static List<GatewayConfig> GetAll()
    {
        try {
            var json = File.ReadAllText("ConfigurationManagement/AllGatewayConfigs.json");
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            };

            var root = JsonSerializer.Deserialize<GatewayRoot>(json, options);
            if (root?.Gateways == null)
                throw new Exception("Failed to deserialize GatewayConfig: Gateways list is null");

            return root.Gateways;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to deserialize GatewayConfig: {ex.Message}");
        }
    }

    public static GatewayConfig? GetById(string gatewayId) {
        try {
            var json = File.ReadAllText("ConfigurationManagement/AllGatewayConfigs.json");
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            };

            var root = JsonSerializer.Deserialize<GatewayRoot>(json, options);
            if (root?.Gateways == null)
                throw new Exception("Failed to deserialize GatewayConfig: Gateways list is null");
            foreach (var gateway in root.Gateways)
                if (gateway.GatewayId == gatewayId)
                    return gateway;

            return null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to deserialize GatewayConfig: {ex.Message}");
        }
    }
}