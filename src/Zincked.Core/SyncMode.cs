namespace Zincked.Core;

/// <summary>
/// Selects which directions a synchronization is allowed to copy in.
/// </summary>
public enum SyncMode
{
    /// <summary>Copy in both directions; the newer copy of each file wins.</summary>
    Bidirectional,

    /// <summary>Copy from the first folder to the second only; never write back to the first.</summary>
    FirstToSecond,

    /// <summary>Copy from the second folder to the first only; never write back to the second.</summary>
    SecondToFirst
}
