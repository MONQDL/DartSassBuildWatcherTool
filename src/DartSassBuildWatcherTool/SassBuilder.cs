using DartSassHost;
using DartSassHost.Helpers;
using JavaScriptEngineSwitcher.V8;

namespace DartSassBuildWatcherTool;

internal sealed class SassBuilder : ISassBuilder
{
    readonly IEnumerable<FileInfo>? _sassFiles;
    readonly DirectoryInfo? _sassDirectory;
    readonly IEnumerable<DirectoryInfo>? _excludeDirectories;
    readonly IFileManager _customFileManager;
    readonly OutputStyle _outputStyle;
    readonly Logger _log;

    public SassBuilder(SassBuilderOptions options, Logger log)
    {
        _sassFiles = options.SassFiles;
        _sassDirectory = options.SassDirectory;
        _excludeDirectories = options.ExcludeDirectories;
        _outputStyle = options.OutputStyle;
        _log = log;

        _customFileManager = new CustomFileManager(options.ProjectPathResolver, options.PathMap);
    }

    public async Task Build()
    {
        if (_sassFiles is not null)
            await BuildFiles(_sassFiles);
        if (_sassDirectory is not null)
            await BuildDirectory(_sassDirectory, _excludeDirectories);
    }

    async Task BuildFiles(IEnumerable<FileInfo> sassFiles)
    {
        try
        {
            using var sassCompiler = new SassCompiler(() => new V8JsEngineFactory().CreateEngine(), _customFileManager);

            foreach (var file in sassFiles)
            {
                if (file.Name.StartsWith("_"))
                {
                    WriteVerbose($"Skipping: {file.FullName}");
                    continue;
                }

                WriteVerbose($"Processing: {file.FullName}");

                var result = sassCompiler.CompileFile(file.FullName, options: new CompilationOptions
                {
                    OutputStyle = _outputStyle,
                });

                var newFile = file.FullName.Replace(file.Extension, ".css");

                if (File.Exists(newFile) && result.CompiledContent == await File.ReadAllTextAsync(newFile))
                    continue;

                await File.WriteAllTextAsync(newFile, result.CompiledContent);
            }
        }
        catch (SassCompilerLoadException e)
        {
            Console.WriteLine("During loading of Sass compiler an error occurred. See details:");
            Console.WriteLine();
            Console.WriteLine(SassErrorHelpers.GenerateErrorDetails(e));
        }
        catch (SassCompilationException e)
        {
            Console.WriteLine("During compilation of SCSS code an error occurred. See details:");
            Console.WriteLine();
            Console.WriteLine(SassErrorHelpers.GenerateErrorDetails(e));
        }
        catch (SassException e)
        {
            Console.WriteLine("During working of Sass compiler an unknown error occurred. See details:");
            Console.WriteLine();
            Console.WriteLine(SassErrorHelpers.GenerateErrorDetails(e));
        }
    }

    async Task BuildDirectory(DirectoryInfo sassDirectory, IEnumerable<DirectoryInfo>? excludedDirectories)
    {
        var sassFiles = Directory.EnumerateFiles(sassDirectory.FullName)
                .Where(file => file.EndsWith(".scss", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".sass", StringComparison.OrdinalIgnoreCase))
                .Select(file => new FileInfo(file));

        await BuildFiles(sassFiles);

        var subDirectories = Directory.EnumerateDirectories(sassDirectory.FullName);
        foreach (var subDirectory in subDirectories)
        {
            var directoryName = new DirectoryInfo(subDirectory).Name;
            if (excludedDirectories?.Any(dir => string.Equals(dir.Name, directoryName, StringComparison.OrdinalIgnoreCase)) == true)
                continue;

            await BuildDirectory(new DirectoryInfo(subDirectory), excludedDirectories);
        }
    }

    void WriteVerbose(string line) => _log.LogVerbose(line);
}
