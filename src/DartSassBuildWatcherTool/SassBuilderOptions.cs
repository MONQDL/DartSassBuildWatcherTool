using DartSassHost;

namespace DartSassBuildWatcherTool;

internal sealed record SassBuilderOptions(IEnumerable<FileInfo>? SassFiles,
        DirectoryInfo? SassDirectory,
        IEnumerable<DirectoryInfo>? ExcludeDirectories,
        ProjectPackageRefManager? ProjectPathResolver,
        IEnumerable<string>? PathMap,
        OutputStyle OutputStyle = OutputStyle.Compressed
    );
