using Server.Core.Pull;

        
//new UpdateManager().WatchGatewayConfigs();
//NatsManager natsManager = new NatsManager();

// ConfigPuller.ListenForGatewayConfigRequest();
//
// Console.WriteLine("Server is running. Press Ctrl+C to exit...");
//
// while (true)
// {
//     Thread.Sleep(1000);
// }
// var options = ConnectionFactory.GetDefaultOptions();
// options.Url = "nats://nats:4222"; // NATS server URL
//
// using var connection = new ConnectionFactory().CreateConnection(options);
// const string subject = "help.request";
//
// connection.SubscribeAsync(subject, (_, args) =>
// {
//     var requestMessage = Encoding.UTF8.GetString(args.Message.Data);
//     Console.WriteLine($"Received request: {requestMessage}");
// });
ConfigPuller.ListenForGatewayConfigRequest();
Console.WriteLine("Server is running. Press Ctrl+C to exit...");
AppDomain.CurrentDomain.ProcessExit += (s, e) => ConfigPuller.DisposeConnection();
while (true)
{
    Thread.Sleep(1000);
}