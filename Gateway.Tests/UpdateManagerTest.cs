using System.Text;
using Gateway.Configuration;
using Gateway.Core;
using Moq;
using NATS.Client;
using Xunit;

namespace Gateway.Tests;

public class UpdateManagerTests
{
    private readonly Mock<IConnection> _mockConnection;
    private readonly Mock<IAppConfigurationProvider> _mockConfigProvider;

    public UpdateManagerTests()
    {
        _mockConnection = new Mock<IConnection>();
        _mockConfigProvider = new Mock<IAppConfigurationProvider>();
    }

    [Fact]
    public async Task RequestConfigAsync_ShouldThrowException_WhenRequestTimesOut()
    {
        const string gatewayId = "test-gateway";
        const string configFilePath = "test_config.json";
        const string pullSubject = "gateway.config.pull";

        _mockConfigProvider.Setup(c => c.GetConfigFilePath())
            .Returns(configFilePath);
        _mockConfigProvider.Setup(c => c.GetSection<NatsConfiguration>("NatsConfiguration"))
            .Returns(new NatsConfiguration { PullSubject = pullSubject });

        _mockConnection
            .Setup(c => c.RequestAsync(pullSubject, It.IsAny<byte[]>(), It.IsAny<int>()))
            .ThrowsAsync(new NATSTimeoutException("Request timed out"));

        var updateManager = new UpdateManager(_mockConnection.Object, _mockConfigProvider.Object);

        var exception = await Assert.ThrowsAsync<NATSTimeoutException>(
            () => updateManager.RequestConfigAsync(gatewayId));

        Assert.Contains("timed out", exception.Message);

        _mockConnection.Verify(
            c => c.RequestAsync(pullSubject, It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == gatewayId), It.IsAny<int>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RequestConfigAsync_ShouldSucceed_WhenValidResponseReceived()
    {
        const string gatewayId = "test-gateway";
        const string configFilePath = "test_config.json";
        const string pullSubject = "gateway.config.pull";
        const string responseContent = "{ \"key\": \"value\" }";
        
        // Create actual response data instead of mocking Msg properties
        var responseData = Encoding.UTF8.GetBytes(responseContent);
        
        _mockConfigProvider.Setup(c => c.GetConfigFilePath())
            .Returns(configFilePath);
        _mockConfigProvider.Setup(c => c.GetSection<NatsConfiguration>("NatsConfiguration"))
            .Returns(new NatsConfiguration { PullSubject = pullSubject });
        
        // Setup the RequestAsync to return a real Msg object
        _mockConnection
            .Setup(c => c.RequestAsync(pullSubject, It.IsAny<byte[]>(), It.IsAny<int>()))
            .ReturnsAsync(new Msg { Data = responseData });

        var updateManager = new UpdateManager(_mockConnection.Object, _mockConfigProvider.Object);

        if (File.Exists(configFilePath))
            File.Delete(configFilePath);

        try
        {
            await updateManager.RequestConfigAsync(gatewayId);

            Assert.True(File.Exists(configFilePath));
            var content = await File.ReadAllTextAsync(configFilePath);
            Assert.Equal(responseContent, content);
        }
        finally
        {
            if (File.Exists(configFilePath))
                File.Delete(configFilePath);
        }
    }
}