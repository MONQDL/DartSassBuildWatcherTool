# DartSassWatchBuilderTool

The tool was build to add support of the SASS(SCSS) files compilation in Razor Client Libraries (RCL) to CSS.
Also there is a `--watch` option to detect `*.sass` and `*.scss` file changes in the directory and automatically trigger build operations.

This tool is used separatly of the .NET project files cause of Blazor WASM can't run watchers from the csproj libraries (.net 7).

## How to install

The tool installes as [global net tool](https://www.nuget.org/packages/DartSassBuildWatcherTool).

```bash
dotnet tool install --global DartSassBuildWatcherTool
```

## How to use

### Watch

You can use the tool to automatically trigger build commands on files changes in specified directory.

All you need is to specify the wathing directory to the `--watch` option.

General use:

```bash
  dsbw --files /projects/scssproj/myfile.scss --watch /projects/scssproj
```

The command will use filesystem watch mechanism to detect file changes (create, delete, rename, move, content changes) of the `*.sass` and `*.scss`
at `/projects/scssproj` directory and will trigger the build-file command of the /projects/scssproj/myfile.scss.

### Razor Client Library (RCL)

The `DartSassWatchBuilderTool` bring the support of using the pattern when you want 
to separate you core SASS files into the RCL (variables, mixins) and then use the RCL
in your frontend client applications. This pattern is moslty used in the microfrontends.

To use the tool you must specify the pathes in `@import` or `@use` or other path directives in the specific way.

#### Example of RCL Configuration

##### Step 1. Configure basic RCL wwwroot file structure

Create an "Razor Client Library" (RCL) library. Say the name will be `BlazorWebKit`.
And the absolute path at the filesystem will be `D:\Projects\BlazorWebKit`. 
So the wwwroot directory will be at `D:\Projects\BlazorWebKit\wwwroot`. 
In the `wwwroot` directory create basic SASS structure.

```
| wwwroot
|  |  blazorwebkit.scss
|  |  img
|  |  |  add.svg
|  |  styles
|  |  |  _variables.scss
|  |  |  _mixins.scss
|  |  |  _components.scss
|  |  |  components
|  |  |  |  _button.scss
```

##### Step 2. Configure file imports at the scss files

The main goal in configuring the `import` pathes is to use "absolute" path, but relative to the `_content/BlazorWebKit`.

*blazorwebkit.scss*
```sass
@use "_content/BlazorWebKit/styles/_variables";
@use "_content/BlazorWebKit/styles/_mixins";
@use "_content/BlazorWebKit/styles/_components";

.alert {
  border: 1px solid $border-dark;
}

nav ul {
  @include horizontal-list;
}

img
```

*styles/variables.scss*
```sass
$base-color: #c6538c;
$border-dark: rgba($base-color, 0.88);
```

*styles/mixins.scss*
```sass
@mixin reset-list {
  margin: 0;
  padding: 0;
  list-style: none;
}

@mixin horizontal-list {
  @include reset-list;

  li {
    display: inline-block;
    margin: {
      left: -2px;
      right: 2em;
    }
  }
}
```

*styles/_components.scss*
```sass
@use "_content/BlazorWebKit/styles/components/_button";
```

*styles/components/_button.scss*
```sass
@use "_content/BlazorWebKit/styles/_variables";

.button {
  font-size: 14px;
  font-family: inherit;
  background-color: $base-color;
  border: $border-dark;
  mask-image: url('img/add.svg');
}
```

##### Step 3. Run the DartSassWatchBuilderTool and build the main theme blazorwebkit.css

```powershell
dsbw --files D:\Projects\BlazorWebKit\wwwroot\blazorwebkit.scss \
--map _content/BlazorWebKit=D:\Projects\BlazorWebKit\wwwroot
```

Option `--files` will compile the file `blazorwebkit.scss` to the `blazorwebkit.css`.

Option `--map` will replace the path `_content/BlazorWebKit` to the `D:\Projects\BlazorWebKit\wwwroot` when resolving the `@import` and `@use` directives.

**!Note** that the image path (and fonts) from the `img` directory must not include the full path (`_content/BlazorWebKit`).
When you consume the library in the client app the Blazor css processor will automatically map library resources to the path `_content/BlazorWebKit`.

##### Step 4. Pack and send the library to your nuget server.

At this stage you will receive ready to use library in your nuget storage.

##### Additional information

If you're using .NET 7 Blazor, the `*.csproj` configuration of the RCL is pretty standard and doesn't require any additional configuration.

Example of the `BlazorWebKit.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <ItemGroup>
    <SupportedPlatform Include="browser" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="7.0.0" />
  </ItemGroup>

</Project>
```

If you are in active library development than you can use `--watch` option.

```powershell
dsbw --files D:\Projects\BlazorWebKit\wwwroot\blazorwebkit.scss \
--map _content/BlazorWebKit=D:\Projects\BlazorWebKit\wwwroot \
--watch D:\Projects\BlazorWebKit\wwwroot
```

#### Example of Frontend Client Application Configuration

##### Step 1. Consume BlazorWebKit library in your Blazor Client App

Create or use existing Blazor client application. Say it will be `BlazorFrontend` at `D:\Projects\BlazorFrontend\BlazorFrontend.csproj`
If you are using Blazor WebAssembley (WASM) include in the `wwwroot/index.html` the link to the `BlazorWebKit` library style.

```html
<html>
<head>
	<!-- inside of head section -->
	<link href="_content/BlazorWebKit/blazorwebkit.css" rel="stylesheet" />
</head>
<body>
	<div id="app"></div>
</body>
</html>
```

##### Step 2. Create the component MyButton

Create the component `MyButton`.

*Components/MyButton.razor*
```razor
<button @onclick="() => Clicked.InvokeAsync()" class="mybutton">
  @ChildContent
</button>

@code {
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  /// <summary>
  /// Occurs when the button is clicked.
  /// </summary>
  [Parameter]
  public EventCallback<MouseEventArgs> Clicked { get; set; }
}
```

Create the style file for `MyButton` component. And import the variables from the library.

*Components/MyButton.razor.scss*

```sass
@use "_content/BlazorWebKit/styles/_variables";

.mybutton {
  border: $border-dark;
}
```

##### Step 3. Compile styles from directory Components to css

```powershell
dsbw --dir D:\Projects\BlazorFrontend\Components \
--proj D:\Projects\BlazorFrontend\BlazorFrontend.csproj
```

Option `--dir` will compile all `*.scss` and `*.sass` files corresponsive `css` files.

Option `--proj` will parse the csproj file and try to map installed directories to the path to installed nuget directory.
The tool will try to map libraries from `<PackageReference>` to standard pathes:

- Windows: `%userprofile%\.nuget\packages`
- Mac/Linux: `~/.nuget/packages`

If the Client App references the library project by `<ProjectReference>`, than the tool will use the path from `<ProjectReference Include="<path-to-library>">`.

If you want to use Blazor WASM hot reload, than you can run the `DartSassWatchBuilderTool` in the `--watch` mode.

```powershell
dsbw --dir D:\Projects\BlazorFrontend\Components \
--proj D:\Projects\BlazorFrontend\BlazorFrontend.csproj
--watch D:\Projects\BlazorFrontend\Components
```