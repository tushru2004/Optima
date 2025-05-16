using Microsoft.Extensions.Configuration;

namespace Server.ConfigurationManagement;

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
        return Environment.GetEnvironmentVariable("CONFIG_FILE")
               ?? throw new InvalidOperationException("Environment variable 'CONFIG_FILE' is not set.");
    }
}