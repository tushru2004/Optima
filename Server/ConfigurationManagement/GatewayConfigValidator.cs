namespace Server.ConfigurationManagement;

using System.Collections.Generic;

public static class GatewayConfigValidator
{
    public static bool Validate(GatewayConfig config, out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.GatewayId))
            errors.Add("GatewayId is missing or empty.");

        if (string.IsNullOrWhiteSpace(config.GatewayName))
            errors.Add("GatewayName is missing or empty.");

        if (string.IsNullOrWhiteSpace(config.FacilityName))
            errors.Add("FacilityName is missing or empty.");

        if (config.Devices.TcpIp == null || config.Devices.TcpIp.Count == 0)
            errors.Add("TCP/IP devices are missing or empty.");

        if (config.Devices.ModbusRtu == null || config.Devices.ModbusRtu.Count == 0)
            errors.Add("Modbus RTU devices are missing or empty.");

        return errors.Count == 0;
    }
}