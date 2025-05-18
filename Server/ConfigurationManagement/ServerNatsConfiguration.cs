namespace Server.ConfigurationManagement;

public class ServerNatsConfiguration
{
    public string Url { get; set; } = string.Empty;
    public int MaxReconnect { get; set; } = -1;
    public int ReconnectWait { get; set; } = 30000;
    public bool AllowReconnect { get; set; } = true;
    public int MaxReconnectAttempts { get; set; } = 100;
    public int ReconnectIntervalMs { get; set; } = 30000;
    public int MaxInitialRetries { get; set; } = 20;
    public int InitialRetryIntervalMs { get; set; } = 30000;
    public string GatewayConfigPullSubject { get; set; } = "gateway.config.pull";
    public string GatewayMessageSubjectTemplate { get; set; } = "messages.gateway.{0}";

}