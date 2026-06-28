using System.IO.Abstractions;

namespace Zincked.Core;

/// <summary>
/// Additive folder synchronizer. For every file found under either root the copy with the
/// newer last-write time is propagated to the other side, subject to the requested
/// <see cref="SyncMode"/>; files are never deleted. All file-system access goes through
/// <see cref="IFileSystem"/> so the engine is fully testable without touching the real disk.
/// </summary>
public sealed class FolderSynchronizer : IFolderSynchronizer
{
    // Windows paths are case-insensitive, and Steam targets Windows, so relative paths are
    // matched ignoring case.
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IFileSystem _fileSystem;
    private readonly SyncOptions _options;

    /// <summary>Initializes a new instance using the default <see cref="SyncOptions"/>.</summary>
    /// <param name="fileSystem">The file-system abstraction to operate against.</param>
    public FolderSynchronizer(IFileSystem fileSystem)
        : this(fileSystem, SyncOptions.Default)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="fileSystem">The file-system abstraction to operate against.</param>
    /// <param name="options">The comparison settings to use.</param>
    public FolderSynchronizer(IFileSystem fileSystem, SyncOptions options)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _options = options;
    }

    /// <inheritdoc />
    public SyncResult Synchronize(string firstFolder, string secondFolder, SyncMode mode = SyncMode.Bidirectional)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(secondFolder);

        string firstRoot = _fileSystem.Path.GetFullPath(firstFolder);
        string secondRoot = _fileSystem.Path.GetFullPath(secondFolder);

        _fileSystem.Directory.CreateDirectory(firstRoot);
        _fileSystem.Directory.CreateDirectory(secondRoot);

        // Synchronizing a folder with itself is a no-op: every file is trivially up to date.
        if (PathComparer.Equals(firstRoot, secondRoot))
        {
            return BuildSelfSyncResult(firstRoot);
        }

        bool allowFirstToSecond = mode is SyncMode.Bidirectional or SyncMode.FirstToSecond;
        bool allowSecondToFirst = mode is SyncMode.Bidirectional or SyncMode.SecondToFirst;

        Dictionary<string, string> firstFiles = EnumerateRelativeFiles(firstRoot);
        Dictionary<string, string> secondFiles = EnumerateRelativeFiles(secondRoot);

        var outcomes = new List<SyncedFile>();
        foreach (string relativePath in UnionOfKeys(firstFiles, secondFiles))
        {
            bool inFirst = firstFiles.TryGetValue(relativePath, out string? firstPath);
            bool inSecond = secondFiles.TryGetValue(relativePath, out string? secondPath);

            SyncDirection direction;
            if (inFirst && !inSecond)
            {
                direction = CopyIfAllowed(
                    firstPath!, _fileSystem.Path.Combine(secondRoot, relativePath),
                    SyncDirection.FirstToSecond, allowFirstToSecond);
            }
            else if (!inFirst && inSecond)
            {
                direction = CopyIfAllowed(
                    secondPath!, _fileSystem.Path.Combine(firstRoot, relativePath),
                    SyncDirection.SecondToFirst, allowSecondToFirst);
            }
            else
            {
                direction = Reconcile(firstPath!, secondPath!, allowFirstToSecond, allowSecondToFirst);
            }

            outcomes.Add(new SyncedFile(relativePath, direction));
        }

        if (_options.ReplicateEmptyDirectories)
        {
            if (allowFirstToSecond)
            {
                ReplicateDirectories(firstRoot, secondRoot);
            }

            if (allowSecondToFirst)
            {
                ReplicateDirectories(secondRoot, firstRoot);
            }
        }

        return new SyncResult(outcomes);
    }

    private SyncResult BuildSelfSyncResult(string root)
    {
        IEnumerable<SyncedFile> files = EnumerateRelativeFiles(root)
            .Keys
            .Select(relativePath => new SyncedFile(relativePath, SyncDirection.None));
        return new SyncResult(files);
    }

    private Dictionary<string, string> EnumerateRelativeFiles(string root)
    {
        var map = new Dictionary<string, string>(PathComparer);
        foreach (string fullPath in _fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            string relativePath = _fileSystem.Path.GetRelativePath(root, fullPath);
            map[relativePath] = fullPath;
        }

        return map;
    }

    private static IEnumerable<string> UnionOfKeys(
        Dictionary<string, string> first,
        Dictionary<string, string> second)
    {
        var keys = new HashSet<string>(first.Keys, PathComparer);
        keys.UnionWith(second.Keys);
        return keys.OrderBy(k => k, PathComparer);
    }

    private SyncDirection Reconcile(
        string firstPath,
        string secondPath,
        bool allowFirstToSecond,
        bool allowSecondToFirst)
    {
        DateTime firstTime = _fileSystem.File.GetLastWriteTimeUtc(firstPath);
        DateTime secondTime = _fileSystem.File.GetLastWriteTimeUtc(secondPath);

        TimeSpan difference = firstTime - secondTime;
        if (difference.Duration() <= _options.TimestampTolerance)
        {
            return SyncDirection.None;
        }

        return firstTime > secondTime
            ? CopyIfAllowed(firstPath, secondPath, SyncDirection.FirstToSecond, allowFirstToSecond)
            : CopyIfAllowed(secondPath, firstPath, SyncDirection.SecondToFirst, allowSecondToFirst);
    }

    private SyncDirection CopyIfAllowed(
        string sourcePath,
        string destinationPath,
        SyncDirection direction,
        bool allowed)
    {
        if (!allowed)
        {
            return SyncDirection.None;
        }

        CopyFile(sourcePath, destinationPath);
        return direction;
    }

    private void CopyFile(string sourcePath, string destinationPath)
    {
        string? destinationDirectory = _fileSystem.Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            _fileSystem.Directory.CreateDirectory(destinationDirectory);
        }

        _fileSystem.File.Copy(sourcePath, destinationPath, overwrite: true);

        // Preserve the source timestamp so the next run sees the pair as identical and does
        // not copy it again. This is what keeps repeated runs idempotent.
        _fileSystem.File.SetLastWriteTimeUtc(
            destinationPath,
            _fileSystem.File.GetLastWriteTimeUtc(sourcePath));
    }

    private void ReplicateDirectories(string sourceRoot, string destinationRoot)
    {
        foreach (string directory in _fileSystem.Directory.EnumerateDirectories(
                     sourceRoot, "*", SearchOption.AllDirectories))
        {
            string relativePath = _fileSystem.Path.GetRelativePath(sourceRoot, directory);
            _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(destinationRoot, relativePath));
        }
    }
}
