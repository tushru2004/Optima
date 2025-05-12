using System.Text.Json.Serialization;

namespace Server.ConfigurationManagement.Elements;

public class Devices
{
    [JsonPropertyName("tcp_ip")] public List<TcpIpDevice> TcpIp { get; set; }

    [JsonPropertyName("modbus_rtu")] public List<ModbusRtuDevice> ModbusRtu { get; set; }
}