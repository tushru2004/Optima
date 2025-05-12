namespace Server.ConfigurationManagement.Elements;
public class ModbusRtuDevice
{
    public string Name { get; set; }
    public string Address { get; set; }
    public int Register { get; set; }
}