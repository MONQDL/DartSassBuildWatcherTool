using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DartSassBuildWatcherTool;
internal class ProjectPackageRefManager
{
    const char Delimeter = '/';

    readonly FileInfo _csProjFile;
    readonly Logger _log;

    List<PackageReference> _packageReferences = new();
    List<PackageReference> _projectReferences = new();

    public ProjectPackageRefManager(FileInfo csProjFile, Logger log)
    {
        _log = log;
        _csProjFile = csProjFile;
        Load();
    }

    void Load()
    {
        var xml = File.ReadAllText(_csProjFile.FullName);

        const string versionName = "Version";
        const string includeName = "Include";
        const string packageReferenceName = "//PackageReference";
        const string projectReferenceName = "//ProjectReference";

        var doc = XDocument.Parse(xml);
        _packageReferences = doc.XPathSelectElements(packageReferenceName)
            .Select(pr => new PackageReference
            (
                Type: PackageTypes.Package,
                Include: pr.Attribute(includeName).Value,
                Version: new Version(pr.Attribute(versionName).Value)
            ))
            .ToList();

        _log.LogVerbose($"Loaded {_packageReferences.Count} Package references.");

        _projectReferences = doc.XPathSelectElements(projectReferenceName)
            .Select(pr => new PackageReference
            (
                Type: PackageTypes.Project,
                Include: pr.Attribute(includeName).Value
            ))
            .ToList();

        _log.LogVerbose($"Loaded {_projectReferences.Count} Project references.");
    }

    /// <summary>
    /// Resolves @use and @import path from sass files to ProjectReference nuget staticwebcontent path.
    /// </summary>
    /// <param name="path">The path from @use or @import in sass file.</param>
    /// <returns></returns>
    public string ResolvePath(string path)
    {
        if (!path.StartsWith("_content"))
            return string.Empty;

        // The path "_content/MyLibrary/styles/mixins.scss" should be resolved to
        // "<profile>/.nuget/packages/MyLibrary/1.1.0/staticwebassets/styles/mixins.scss"
        // if _csProjFile contains <PackageReference>
        // Pathes of nuget packages:
        // Windows: %userprofile%\.nuget\packages
        // Mac/Linux: ~/.nuget/packages

        // If the _csProjFile contains <ProjectReference> than the path "_content/MyLibrary/styles/mixins.scss"
        // will be resolved to <ProjectReferenceInclude>/wwwroot/styles/mixins.scss

        var splittedPathes = SplitPath(path);

        var packagePath = ResolvePackagePath(splittedPathes.PackageName);

        var result = Path.Combine(packagePath, splittedPathes.ContentPath);

        _log.LogVerbose($"""Path "{path}" resolved to "{result}".""");

        return result;
    }

    static SplittedContentPath SplitPath(string path)
    {
        var splittedPathes = path.Split(Delimeter, StringSplitOptions.RemoveEmptyEntries);
        if (splittedPathes.Length <= 1)
            throw new BuilderException($"""The path "{path}" has to be in format "_content/MyLibrary/<stylepath>".""");

        return new SplittedContentPath(splittedPathes[1], RemoveFileExtension(CreateSassContentPath(splittedPathes)));
    }

    static string CreateSassContentPath(string[] splittedPathes)
    {
        StringBuilder contentPathBuilder = new();
        for (int i = 2; i < splittedPathes.Length; i++)
        {
            contentPathBuilder.Append(splittedPathes[i]);
            if (i + 1 < splittedPathes.Length)
                contentPathBuilder.Append(Path.DirectorySeparatorChar);
        }
        return contentPathBuilder.ToString();
    }

    string ResolvePackagePath(string packageName)
    {
        var userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var nugetPackagesPath = Path.Combine(".nuget", "packages");

        foreach (var reference in _packageReferences)
        {
            if (reference.Include == packageName)
            {
                return Path.Combine(userprofile,
                           nugetPackagesPath,
                           packageName,
                           reference.Version.ToString(),
                           "staticwebassets");
            }
        }

        foreach (var reference in _projectReferences)
        {
            if (reference.Include.Contains(packageName))
            {
                // Calculating full path that will be returned ralative to _csProjFile.
                var projectDirectory = _csProjFile.Directory;
                var path = Path.Combine(projectDirectory.FullName, Path.GetDirectoryName(reference.Include.Replace("\\", "/")));
                return Path.Combine(path, "wwwroot");
            }
        }

        return string.Empty;
    }
    static string RemoveFileExtension(string contentPath)
    {
        // Removing file extension from content path due to SassHost auto resolve file extension for Library.
        var dir = Path.GetDirectoryName(contentPath);
        if (dir is null)
            return Path.GetFileNameWithoutExtension(contentPath);

        return Path.Combine(dir, Path.GetFileNameWithoutExtension(contentPath));
    }

    record SplittedContentPath(string PackageName, string ContentPath);
    record PackageReference(PackageTypes Type, string Include, Version? Version = default);
    enum PackageTypes
    {
        Package,
        Project,
    }
}