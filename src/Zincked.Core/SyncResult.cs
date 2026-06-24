using System.Collections.ObjectModel;

namespace Zincked.Core;

/// <summary>
/// Aggregates the per-file outcomes produced by a single synchronization run.
/// </summary>
public sealed class SyncResult
{
    private readonly List<SyncedFile> _files;

    /// <summary>Initializes a new instance from the supplied per-file outcomes.</summary>
    /// <param name="files">The outcome recorded for every file that was examined.</param>
    public SyncResult(IEnumerable<SyncedFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        _files = [.. files];
        Files = new ReadOnlyCollection<SyncedFile>(_files);
    }

    /// <summary>Every file that was examined, with the action taken for it.</summary>
    public IReadOnlyList<SyncedFile> Files { get; }

    /// <summary>The number of files copied from the first folder to the second.</summary>
    public int CopiedToSecond => _files.Count(f => f.Direction == SyncDirection.FirstToSecond);

    /// <summary>The number of files copied from the second folder to the first.</summary>
    public int CopiedToFirst => _files.Count(f => f.Direction == SyncDirection.SecondToFirst);

    /// <summary>The number of files that were already up to date on both sides.</summary>
    public int UpToDate => _files.Count(f => f.Direction == SyncDirection.None);
}
