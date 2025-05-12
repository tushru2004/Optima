using Server.Nats;

namespace Server.Core.Push;

using System.IO;


public class UpdateManager
{       
    private static DateTime _lastRead = DateTime.MinValue;

    public void WatchGatewayConfigs()
    {
        var filePath = "Util/ALLGatewayConfigs.json";
        var directoryPath = Path.GetDirectoryName(filePath);

        if (directoryPath == null || !Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Directory '{directoryPath}' does not exist.");
            return;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File '{filePath}' does not exist. Watching for changes when it is created...");
        }

        var watcher = new FileSystemWatcher
        {
            Path = directoryPath,
            Filter = Path.GetFileName(filePath),
            NotifyFilter = NotifyFilters.LastWrite
        };

        watcher.Changed += (sender, e) =>
        {
            var currentChange = File.GetLastWriteTime(e.FullPath);

            if (Math.Abs((currentChange - _lastRead).TotalMilliseconds) > 1000)
            {
                _lastRead = currentChange;
                Console.WriteLine($"File '{e.FullPath}' updated. Last write time: {currentChange}");
                Console.WriteLine($"Time to push updates to ALL gateways");
                new NatsManager().Publish();
            }

        };

        watcher.EnableRaisingEvents = true;
        Console.WriteLine($"Watching for changes in file: {filePath}");
    }
}





