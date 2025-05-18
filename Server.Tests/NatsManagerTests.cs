using System.Text;
using Moq;
using NATS.Client;
using Server.ConfigurationManagement;
using Server.Nats;


namespace Server.Tests;

public class NatsManagerTests
{
    private readonly Mock<IConnection> _mockConnection;
    private readonly Mock<IAppConfigurationProvider> _mockConfigProvider;
    private readonly NatsManager _natsManager;
    private readonly ServerNatsConfiguration _natsConfig;

    public NatsManagerTests()
    {
        _mockConnection = new Mock<IConnection>();
        _mockConfigProvider = new Mock<IAppConfigurationProvider>();
        
        // Setup the mock configuration provider to return a NatsConfiguration
        _natsConfig = new ServerNatsConfiguration
        {
            GatewayConfigPullSubject = "gateway.config.pull",
            GatewayMessageSubjectTemplate = "messages.gateway.{0}"
        };
        
        _mockConfigProvider
            .Setup(c => c.GetSection<ServerNatsConfiguration>("ServerNatsConfiguration"))
            .Returns(_natsConfig);
            
        _natsManager = new NatsManager(_mockConnection.Object, _mockConfigProvider.Object);
    }

    [Fact]
    public void Publish_ShouldPublishMessagesToAllGateways()
    {
        var gatewayConfigs = GatewayConfig.GetAll();
        _natsManager.Publish();
        
        // Verify each gateway has a message published using the template from config
        foreach (var config in gatewayConfigs)
        {
            var expectedSubject = string.Format(_natsConfig.GatewayMessageSubjectTemplate, config.GatewayId);
            _mockConnection.Verify(
                c => c.Publish(It.Is<string>(s => s == expectedSubject), It.IsAny<byte[]>()),
                Times.Once);
        }
    }

    [Fact]
    public void Publish_ShouldThrowException_WhenPublishFails()
    {
        _mockConnection
            .Setup(c => c.Publish(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Throws(new Exception("Publish failed"));

        var exception = Assert.Throws<InvalidOperationException>(() => _natsManager.Publish());

        Assert.Equal("Error occurred while publishing message to NATS server.", exception.Message);
    }

    [Fact]
    public void ListenForGatewayConfigRequest_ShouldProcessValidRequests()
    {
        var subject = _natsConfig.GatewayConfigPullSubject;
        var requestMessage = "1";
        var replyTo = "reply-subject";
        var messageData = Encoding.UTF8.GetBytes(requestMessage);
        var args = new MsgHandlerEventArgs(new Msg
        {
            Subject = subject,
            Data = messageData,
            Reply = replyTo
        });

        _mockConnection
            .Setup(c => c.SubscribeAsync(subject, It.IsAny<EventHandler<MsgHandlerEventArgs>>()))
            .Callback<string, EventHandler<MsgHandlerEventArgs>>((_, handler) => { handler?.Invoke(this, args); });

        _natsManager.ListenForGatewayConfigRequest();

        _mockConnection.Verify(
            c => c.Publish(It.Is<string>(s => s == replyTo), It.IsAny<byte[]>()),
            Times.Once);
    }

    [Fact]
    public void ListenForGatewayConfigRequest_ShouldThrowException_WhenNoReplyTo()
    {
        var subject = _natsConfig.GatewayConfigPullSubject;
        var requestMessage = "2";
        var messageData = Encoding.UTF8.GetBytes(requestMessage);
        var args = new MsgHandlerEventArgs(new Msg
        {
            Subject = subject,
            Data = messageData,
            Reply = null
        });

        _mockConnection
            .Setup(c => c.SubscribeAsync(subject, It.IsAny<EventHandler<MsgHandlerEventArgs>>()))
            .Callback<string, EventHandler<MsgHandlerEventArgs>>((_, handler) => { handler?.Invoke(this, args); });

        var exception = Assert.Throws<InvalidOperationException>(() => _natsManager.ListenForGatewayConfigRequest());

        Assert.Equal("No ReplyTo subject found in the request.", exception.Message);
    }
}