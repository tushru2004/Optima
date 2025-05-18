namespace Gateway.Configuration;

public class NatsConfiguration
{
    public string Url { get; set; } = string.Empty;
    public string PullSubject { get; set; } = string.Empty;
    public string MessageSubjectTemplate { get; set; } = string.Empty;
    public int MaxRetryAttempts { get; set; } = 2;
    public int RetryBackoffSeconds { get; set; } = 2;
    public int MaxReconnect { get; set; } = -1;
    public int ReconnectWait { get; set; } = 30000;
    public bool AllowReconnect { get; set; } = true;
    public int MaxReconnectAttempts { get; set; } = 100;
    public int ReconnectIntervalMs { get; set; } = 30000;
    public int MaxInitialRetries { get; set; } = 20;
    public int InitialRetryIntervalMs { get; set; } = 30000;
}