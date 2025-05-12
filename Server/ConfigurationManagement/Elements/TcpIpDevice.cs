namespace Server.ConfigurationManagement.Elements;
public class TcpIpDevice
{
    public required string Name { get; set; }
    public required string Ip { get; set; }
    public required int Port { get; set; }
}