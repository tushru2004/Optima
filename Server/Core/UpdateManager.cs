using Server.Nats;
using Serilog;

namespace Server.Core;

public class UpdateManager(NatsManager updateManager)
{
    private static DateTime _lastRead = DateTime.MinValue;

    public void PushUpdates(string filePath)
    {
        var directoryPath = Path.GetDirectoryName(filePath);

        if (directoryPath == null || !Directory.Exists(directoryPath))
        {
            Log.Warning("Directory does not exist: {DirectoryPath}", directoryPath);
            return;
        }

        if (!File.Exists(filePath))
            Log.Information("File does not exist. Watching for changes when it is created: {FilePath}", filePath);

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
                Log.Information("File updated. Last write time: {LastWriteTime}, Path: {FilePath}", currentChange, e.FullPath);
                Log.Information("Pushing updates to all gateways");
                updateManager.Publish();
            }
        };

        watcher.EnableRaisingEvents = true;
        Log.Information("Watching for changes in file: {FilePath}", filePath);
    }

    public void ListenForGatewayConfigRequest()
    {
        updateManager.ListenForGatewayConfigRequest();
    }
}