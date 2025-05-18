namespace Gateway.Configuration;

public class NatsConfiguration
{
    public string Url { get; set; } = string.Empty;
    public string PullSubject { get; set; } = string.Empty;
    public string MessageSubjectTemplate { get; set; } = string.Empty;

}