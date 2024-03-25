using DartSassHost;
using JavaScriptEngineSwitcher.Core.Resources;
using System.IO;
using System.Web;

namespace DartSassBuildWatcherTool;

/// <summary>
/// File manager
/// </summary>
public sealed class CustomFileManager : IFileManager
{
    /// <summary>
    /// Current working directory of the application
    /// </summary>
    readonly string _currentDirectory;
    readonly ProjectPackageRefManager? _projectPathResolver;
    readonly Dictionary<string, string> _pathMap;

    /// <summary>
    /// Private constructor for implementation Singleton pattern
    /// </summary>
    public CustomFileManager(ProjectPackageRefManager? projectPathResolver, IEnumerable<string>? pathMap)
    {
        _pathMap = CreatePathMap(pathMap);
        _projectPathResolver = projectPathResolver;
        _currentDirectory = Directory.GetCurrentDirectory();
    }


    #region IFileManager implementation

    public bool SupportsVirtualPaths
    {
        get => _projectPathResolver is not null || _pathMap is not null;
    }


    public string GetCurrentDirectory()
    {
        return _currentDirectory;
    }

    public bool FileExists(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(
                nameof(path),
                string.Format(Strings.Common_ArgumentIsNull, nameof(path))
            );
        }
        path = HttpUtility.UrlDecode(path);

        bool result = File.Exists(path);

        return result;
    }

    public bool IsAppRelativeVirtualPath(string path)
    {
        return path.StartsWith("_content") || OneOfMapKey(path);
    }

    public string ToAbsoluteVirtualPath(string path)
    {
        string mappedPath = GetMappedValue(path);
        if (path.StartsWith("_content") && _projectPathResolver is not null)
            return _projectPathResolver.ResolvePath(path);
        return mappedPath;
    }

    public string ReadFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(
                nameof(path),
                string.Format(Strings.Common_ArgumentIsNull, nameof(path))
            );
        }

        path = HttpUtility.UrlDecode(path);

        string content = File.ReadAllText(path);

        return content;
    }

    string GetMappedValue(string path)
    {
        foreach (var map in _pathMap)
        {
            if (path.StartsWith(map.Key))
                return path.Replace(map.Key, map.Value);
        }

        return path;

    }
    bool OneOfMapKey(string path)
    {
        foreach (var map in _pathMap)
        {
            if (path.StartsWith(map.Key))
                return true;
        }
        return false;
    }

    Dictionary<string, string> CreatePathMap(IEnumerable<string>? pathMap)
    {
        var result = new Dictionary<string, string>();
        if (pathMap is null)
            return result;
        foreach (var map in pathMap)
        {
            var splittedMap = map.Split('=');
            if (splittedMap.Length < 2)
                throw new BuilderException($"""Map value "{_pathMap}" must be in format <key>=<value>.""");

            var key = splittedMap[0];
            var value = splittedMap[1];

            result.Add(key, value);
        }
        return result;
    }
    #endregion
}
