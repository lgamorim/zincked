namespace Zincked.Core;

/// <summary>
/// Synchronizes the contents of two folders so that each ends up holding the newest copy
/// of every file found in either.
/// </summary>
public interface IFolderSynchronizer
{
    /// <summary>
    /// Synchronizes <paramref name="firstFolder"/> and <paramref name="secondFolder"/>,
    /// recursively, in both directions. For each file the copy with the newer last-write
    /// time is propagated to the other side. Files are never deleted.
    /// </summary>
    /// <param name="firstFolder">The first folder root (for example, the local game folder).</param>
    /// <param name="secondFolder">The second folder root (for example, the cloud folder).</param>
    /// <returns>A <see cref="SyncResult"/> describing what was copied.</returns>
    SyncResult Synchronize(string firstFolder, string secondFolder);
}
