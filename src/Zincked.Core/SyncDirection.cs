namespace Zincked.Core;

/// <summary>
/// Identifies which way a file was propagated during a synchronization.
/// </summary>
public enum SyncDirection
{
    /// <summary>The file was already up to date on both sides; nothing was copied.</summary>
    None,

    /// <summary>The file was copied from the first folder to the second folder.</summary>
    FirstToSecond,

    /// <summary>The file was copied from the second folder to the first folder.</summary>
    SecondToFirst
}
