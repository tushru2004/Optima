using System.Text;
using System.Text.Json;
using Moq;
using NATS.Client;
using Server.ConfigurationManagement;
using Server.Nats;
using Xunit;
using Server;
using Server.Core;

namespace Server.Tests;

public class UpdateManagerTests
{
    private readonly Mock<IConnection> _mockConnection;
    private readonly NatsManager _natsManager;

    public UpdateManagerTests()
    {
        _mockConnection = new Mock<IConnection>();
        _natsManager = new NatsManager(_mockConnection.Object);
    }

    [Fact]
    public void Publish_ShouldPublishMessagesToAllGateways()
    {
        // Use the real GetAll method
        var gatewayConfigs = GatewayConfig.GetAll();

        _natsManager.Publish();

        _mockConnection.Verify(
            c => c.Publish(It.Is<string>(s => s.StartsWith("gateway.")), It.IsAny<byte[]>()), 
            Times.Exactly(gatewayConfigs.Count));
    }

    [Fact]
    public void Publish_ShouldThrowException_WhenPublishFails()
    {
        var gatewayConfigs = GatewayConfig.GetAll();

        _mockConnection
            .Setup(c => c.Publish(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Throws(new Exception("Publish failed"));

        var exception = Assert.Throws<InvalidOperationException>(() => _natsManager.Publish());

        Assert.Equal("Error occurred while publishing message to NATS server.", exception.Message);
    }

    [Fact]
    public void ListenForGatewayConfigRequest_ShouldProcessValidRequests()
    {
        var subject = "gateway.config.pull";
        var requestMessage = "00:1A:2B:3C:4D:5E";
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
            .Callback<string, EventHandler<MsgHandlerEventArgs>>((_, handler) => 
            {
                handler?.Invoke(this, args);
            });
        
        _natsManager.ListenForGatewayConfigRequest();

        _mockConnection.Verify(
            c => c.Publish(It.Is<string>(s => s == replyTo), It.IsAny<byte[]>()), 
            Times.Once);
    }

    [Fact]
    public void ListenForGatewayConfigRequest_ShouldThrowException_WhenNoReplyTo()
    {
        var subject = "gateway.config.pull";
        var requestMessage = "00:1A:2B:3C:4D:5E";
        var messageData = Encoding.UTF8.GetBytes(requestMessage);
        var args = new MsgHandlerEventArgs(new Msg
        {
            Subject = subject,
            Data = messageData,
            Reply = null
        });

        _mockConnection
            .Setup(c => c.SubscribeAsync(subject, It.IsAny<EventHandler<MsgHandlerEventArgs>>()))
            .Callback<string, EventHandler<MsgHandlerEventArgs>>((_, handler) => 
            {
                handler?.Invoke(this, args);
            });
        
        var exception = Assert.Throws<InvalidOperationException>(() => _natsManager.ListenForGatewayConfigRequest());

        Assert.Equal("No ReplyTo subject found in the request.", exception.Message);
    }
}