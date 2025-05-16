using Microsoft.Extensions.Configuration;

namespace Gateway.Configuration;

public interface IAppConfigurationProvider
{
    T GetSection<T>(string sectionName) where T : class, new();
    string GetConfigFilePath();
}

public class AppConfigurationProvider : IAppConfigurationProvider
{
    private readonly IConfiguration _configuration;

    public AppConfigurationProvider()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build();
    }

    public T GetSection<T>(string sectionName) where T : class, new()
    {
        return _configuration.GetSection(sectionName).Get<T>()
               ?? throw new InvalidOperationException($"{sectionName} section is missing in appsettings.json");
    }

    public string GetConfigFilePath()
    {
        var gatewayId = Environment.GetEnvironmentVariable("GATEWAY_ID")
                        ?? throw new InvalidOperationException("Environment variable 'GATEWAY_ID' is not set.");

        var settings = GetSection<GatewayAppSettings>("GatewayAppSettings");
        var baseFile = settings.ConfigFilePathBase;
        var baseFileExt = settings.ConfigFilePathExt;
        
        return $"{baseFile}_{gatewayId}.{baseFileExt}";
    }
}