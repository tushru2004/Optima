using System.Text;
using NATS.Client;

namespace Gateway.Core;

public class UpdateManager(IConnection connection)
{
    private readonly IConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public async Task RequestConfigAsync(string message)
    {
        if (_connection == null)
            throw new InvalidOperationException("NATS connection not initialized");

        const string subject = "gateway.config.pull";
        try
        {
            var response = await _connection.RequestAsync(subject, Encoding.UTF8.GetBytes(message), 5000); // 5-second timeout
            var responseMessage = Encoding.UTF8.GetString(response.Data);
            await File.WriteAllTextAsync("response.json", responseMessage);
            Console.WriteLine($"Sent request: {message}");
            Console.WriteLine($"Received response: {responseMessage}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send request: {ex.Message}");
        }
    }
}