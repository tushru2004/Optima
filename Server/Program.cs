using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NATS.Client;
using Server.Core;
using Server.Util;
using Server.Util.Models;

namespace Server;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Server is publishing a message...");
        
        new UpdateManager().WatchGatewayConfigs();
        // Keep the application alive
        Console.WriteLine("Server is running. Press Ctrl+C to exit...");
        while (true)
        {
            Thread.Sleep(1000); // Keeps the app alive
        }
    }
}