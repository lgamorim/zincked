namespace Zincked.Core;

/// <summary>
/// Describes the outcome of synchronizing a single file, identified by its path relative
/// to the two folder roots.
/// </summary>
/// <param name="RelativePath">The file's path relative to each folder root.</param>
/// <param name="Direction">Which way the file was copied, or <see cref="SyncDirection.None"/>.</param>
public sealed record SyncedFile(string RelativePath, SyncDirection Direction);
