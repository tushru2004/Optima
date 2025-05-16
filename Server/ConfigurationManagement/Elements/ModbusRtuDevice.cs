namespace Server.ConfigurationManagement.Elements;

public class ModbusRtuDevice
{
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required int Register { get; set; }
}