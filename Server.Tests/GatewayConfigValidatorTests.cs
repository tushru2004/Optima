using Server.ConfigurationManagement;
using Server.ConfigurationManagement.Elements;

namespace Server.Tests;

public class GatewayConfigValidatorTests
{
    [Fact]
    public void Validate_ShouldReturnTrue_WhenConfigIsValid()
    {
        var config = new GatewayConfig
        {
            GatewayId = "00:1A:2B:3C:4D:5E",
            GatewayName = "Valid Gateway",
            FacilityName = "Valid Facility",
            Devices = new Devices
            {
                TcpIp = new List<TcpIpDevice>
                {
                    new TcpIpDevice { Name = "Device1", Ip = "192.168.1.1", Port = 502 }
                },
                ModbusRtu = new List<ModbusRtuDevice>
                {
                    new ModbusRtuDevice { Name = "Modbus1", Address = "1", Register = 40001 }
                }
            }
        };

        var isValid = GatewayConfigValidator.Validate(config, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenConfigIsInvalid()
    {
        var config = new GatewayConfig
        {
            GatewayId = "",
            GatewayName = "",
            FacilityName = "",
            Devices = new Devices
            {
                TcpIp = new List<TcpIpDevice>(),
                ModbusRtu = new List<ModbusRtuDevice>()
            }
        };

        var isValid = GatewayConfigValidator.Validate(config, out var errors);

        Assert.False(isValid);
        Assert.Contains("GatewayId is missing or empty.", errors);
        Assert.Contains("GatewayName is missing or empty.", errors);
        Assert.Contains("FacilityName is missing or empty.", errors);
        Assert.Contains("TCP/IP devices are missing or empty.", errors);
        Assert.Contains("Modbus RTU devices are missing or empty.", errors);
    }
}