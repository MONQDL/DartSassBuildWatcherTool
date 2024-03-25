namespace DartSassBuildWatcherTool.Tests;

public class BuildScssTests : IDisposable
{
    /// <summary>
    /// This field is used for debugging tests. Allows you not to delete files after processing in the tmp folder.
    /// </summary>
#if DEBUG
    const bool ClearTempDirectory = false;
#else
    const bool ClearTempDirectory = true;
#endif
    const string SassFilesPath = "./bin/tmp-BuildScssTests";
    const string SassFileName = "test_file.scss";
    const string CssFileName = "test_file.css";

    string _sassFile;

    public BuildScssTests()
    {
        _sassFile = Path.Combine(SassFilesPath, SassFileName);
        if (!Directory.Exists(SassFilesPath))
        {
            Directory.CreateDirectory(SassFilesPath);
            if (!File.Exists(_sassFile))
            {
                File.WriteAllText(_sassFile, """
                    .myclass {
                      color: red;
                    }
                    """);
            }
        }
    }

    [Fact]
    public async Task ShouldBuildCssFromFiles()
    {
        var sassFile = new List<FileInfo>{ new FileInfo(_sassFile) };

        var builder = new SassBuilder(new SassBuilderOptions
          (
              SassFiles: sassFile,
              SassDirectory: null,
              ExcludeDirectories: null,
              ProjectPathResolver: null,
              PathMap: null,
              OutputStyle: DartSassHost.OutputStyle.Compressed
          ), new Logger(LogLevels.Default));
        await builder.Build();

        var resultFile = Path.Combine(SassFilesPath, CssFileName);
        Assert.True(File.Exists(resultFile));

        var resultFileContent = File.ReadAllText(resultFile);

        Assert.Equal("""
            .myclass{color:red}
            """, resultFileContent);
    }

    public void Dispose()
    {
        if (ClearTempDirectory)
        {
            var di = new DirectoryInfo(SassFilesPath);
            if (di.Exists)
            {
                foreach (var file in di.GetFiles())
                    file.Delete();

                foreach (var dir in di.GetDirectories())
                    dir.Delete(true);

                Directory.Delete(SassFilesPath);
            }
        }
    }
}