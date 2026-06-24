namespace Zincked.Core;

/// <summary>
/// Tunable settings that control how <see cref="FolderSynchronizer"/> compares files.
/// </summary>
public sealed class SyncOptions
{
    /// <summary>
    /// Maximum difference in last-write time for which two copies of a file are still
    /// considered identical. Defaults to two seconds so that drives with coarse timestamp
    /// resolution (FAT/exFAT on USB sticks and some network shares) do not trigger
    /// spurious copies back and forth.
    /// </summary>
    public TimeSpan TimestampTolerance { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// When <see langword="true"/>, empty subdirectories present on one side are recreated
    /// on the other so the folder structures match exactly. Defaults to <see langword="true"/>.
    /// </summary>
    public bool ReplicateEmptyDirectories { get; init; } = true;

    /// <summary>The default options.</summary>
    public static SyncOptions Default { get; } = new();
}
