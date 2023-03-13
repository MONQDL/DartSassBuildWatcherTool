using DartSassBuildWatcherTool;
using DartSassHost;
using System.CommandLine;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

#if DEBUG
//args = new[] { "--files", @"d:\Projects\monq\libs\Monq.BlazorWebKit\src\Monq.BlazorWebKit\wwwroot\blazorwebkit.scss",
//"--watch", @"d:\Projects\monq\libs\Monq.BlazorWebKit\src\Monq.BlazorWebKit"};
args = new[] { "--files", @"d:\Projects\monq\libs\Monq.BlazorWebKit\src\Monq.BlazorWebKit\wwwroot\blazorwebkit.scss",
"--map", @"_content/Monq.BlazorWebKit=d:\Projects\monq\libs\Monq.BlazorWebKit\src\Monq.BlazorWebKit\wwwroot",
};
//args = new[] { "--files", @"d:\Projects\monq\libs\Monq.BlazorWebKit\src\Monq.BlazorWebKit.Client\wwwroot\css\app.scss",
//"--proj", @"d:\Projects\monq\libs\Monq.BlazorWebKit\src\Monq.BlazorWebKit.Client\Monq.BlazorWebKit.Client.csproj"};
//args = new[] { "--files", @"d:\Projects\monq\saas\saas-frontend-b-service\src\Saas.Service.Frontend\wwwroot\css\app.scss",
//"--proj", @"d:\Projects\monq\saas\saas-frontend-b-service\src\Saas.Service.Frontend\Saas.Service.Frontend.csproj"};
#endif

var filesOption = new Option<IEnumerable<FileInfo>?>(
            name: "--files",
            description: "The sass files that will be compiled to css.");
filesOption.Arity = ArgumentArity.OneOrMore;
filesOption.IsRequired = false;

var directoryOption = new Option<DirectoryInfo?>(
            name: "--dir",
            description: "The directory that contains files to be compiled to css.");
filesOption.Arity = ArgumentArity.ExactlyOne;
filesOption.IsRequired = false;

var excludeDirectoriesOption = new Option<IEnumerable<DirectoryInfo>?>(
            name: "--exlude-dirs",
            description: "The file to read and display on the console.");
filesOption.Arity = ArgumentArity.OneOrMore;
filesOption.IsRequired = false;

var watchOption = new Option<DirectoryInfo?>(
    name: "--watch",
    description: "Watch for changes on *.scss files in watch folder."
    );
watchOption.Arity = ArgumentArity.ExactlyOne;
watchOption.IsRequired = false;

var projOption = new Option<FileInfo?>(
    name: "--proj",
    description: "Get additional directories from the *.csproj."
    );
projOption.Arity = ArgumentArity.ExactlyOne;
projOption.IsRequired = false;

var mapOption = new Option<IEnumerable<string>?>(
    name: "--map",
    description: """Maps prefix path in @use or @import path to custom path. Example "--map _content/MyLib=."."""
    );
mapOption.Arity = ArgumentArity.OneOrMore;
mapOption.IsRequired = false;

var outputStyleOption = new Option<OutputStyle>(
    name: "--outputstyle",
    description: """The css output style. Can be "Compressed", "Expanded"."""
    );

outputStyleOption.Arity = ArgumentArity.ExactlyOne;
outputStyleOption.IsRequired = false;

var rootCommand = new RootCommand("Compile scss files using Dart Sass Compiller.");
rootCommand.AddOption(filesOption);
rootCommand.AddOption(directoryOption);
rootCommand.AddOption(excludeDirectoriesOption);
rootCommand.AddOption(watchOption);
rootCommand.AddOption(projOption);
rootCommand.AddOption(mapOption);
rootCommand.AddOption(outputStyleOption);

rootCommand.SetHandler(async (context) =>
{
    var sassFiles = context.ParseResult.GetValueForOption(filesOption);
    var sassDirectory = context.ParseResult.GetValueForOption(directoryOption);
    var excludeDirectories = context.ParseResult.GetValueForOption(excludeDirectoriesOption);
    var watchDirectory = context.ParseResult.GetValueForOption(watchOption);
    var project = context.ParseResult.GetValueForOption(projOption);
    var pathMap = context.ParseResult.GetValueForOption(mapOption);
    var outputStyle = context.ParseResult.GetValueForOption(outputStyleOption);
    var cancellationToken = context.GetCancellationToken();

    ProjectPackageRefManager? projectPathResolver = null;
    if (project is not null)
        projectPathResolver = new ProjectPackageRefManager(project);

    var builder = new SassBuilder(new SassBuilderOptions
    (
        SassFiles: sassFiles,
        SassDirectory: sassDirectory,
        ExcludeDirectories: excludeDirectories,
        ProjectPathResolver: projectPathResolver,
        PathMap: pathMap,
        OutputStyle: outputStyle
    ));
    await builder.Build();

    if (watchDirectory is not null)
        StartWatchingDirectory(watchDirectory, builder, cancellationToken);
});

return await rootCommand.InvokeAsync(args);

void StartWatchingDirectory(DirectoryInfo watchDirectory, ISassBuilder sassBuilder, CancellationToken cancellationToken)
{
    var sass = new SassWatcher(watchDirectory.FullName, sassBuilder, cancellationToken);
    sass.StartWatching();
}
