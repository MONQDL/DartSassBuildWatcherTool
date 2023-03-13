namespace DartSassBuildWatcherTool;

internal class SassWatcher
{
    readonly FileSystemWatcher _fileSystemWatcher;
    readonly ISassBuilder _sassBuilder;

    static readonly AutoResetEvent Closing = new(false);

    public SassWatcher(string rootDirectory, ISassBuilder sassBuilder, CancellationToken cancellationToken)
    {
        _sassBuilder = sassBuilder;
        _fileSystemWatcher = new FileSystemWatcher(rootDirectory);

        cancellationToken.Register(() => Closing.Set());
    }

    public void StartWatching()
    {
        ConfigureFilters();
        ConfigureEvents();
        Closing.WaitOne();
    }

    void ConfigureFilters()
    {
        _fileSystemWatcher.Filters.Add("*.scss");
        _fileSystemWatcher.Filters.Add("*.sass");
        _fileSystemWatcher.NotifyFilter = NotifyFilters.Attributes
                                | NotifyFilters.CreationTime
                                | NotifyFilters.DirectoryName
                                | NotifyFilters.FileName
                                | NotifyFilters.LastAccess
                                | NotifyFilters.LastWrite
                                | NotifyFilters.Security
                                | NotifyFilters.Size;
        _fileSystemWatcher.IncludeSubdirectories = true;

    }

    void ConfigureEvents()
    {
        _fileSystemWatcher.Created += HandleCreated;
        _fileSystemWatcher.Deleted += HandleDeleted;
        _fileSystemWatcher.Changed += HandleChanged;
        _fileSystemWatcher.Renamed += HandleRenamed;
        _fileSystemWatcher.Error += HandleError;

        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    void HandleCreated(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Create: {e.FullPath}");
        _sassBuilder.Build().GetAwaiter().GetResult();
    }

    void HandleDeleted(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Delete: {e.FullPath}");
        _sassBuilder.Build().GetAwaiter().GetResult();
    }

    void HandleChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Change: {Enum.GetName(e.ChangeType)} {e.FullPath}");
        _sassBuilder.Build().GetAwaiter().GetResult();
    }

    void HandleRenamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"Rename: {e.OldName} => {e.Name}");
        _sassBuilder.Build().GetAwaiter().GetResult();
    }

    void HandleError(object sender, ErrorEventArgs e)
    {
        Console.WriteLine($"Error: {e.GetException().Message}");
        _sassBuilder.Build().GetAwaiter().GetResult();
    }
}
