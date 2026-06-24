namespace Zincked.App;

/// <summary>
/// The outcome of parsing the command line: either the two resolved folders, a request for
/// help, or an error describing why parsing failed.
/// </summary>
/// <param name="GameFolder">The resolved game folder, or <see langword="null"/>.</param>
/// <param name="CloudFolder">The resolved cloud folder, or <see langword="null"/>.</param>
/// <param name="HelpRequested">Whether a help flag was supplied.</param>
/// <param name="ErrorMessage">The reason parsing failed, or <see langword="null"/> on success.</param>
public sealed record CommandLineParseResult(
    string? GameFolder,
    string? CloudFolder,
    bool HelpRequested,
    string? ErrorMessage)
{
    /// <summary>Whether parsing failed.</summary>
    public bool HasError => ErrorMessage is not null;

    /// <summary>Creates a result indicating that help was requested.</summary>
    public static CommandLineParseResult Help() => new(null, null, true, null);

    /// <summary>Creates a failed result carrying the given error message.</summary>
    public static CommandLineParseResult Failure(string message) => new(null, null, false, message);

    /// <summary>Creates a successful result carrying the two resolved folders.</summary>
    public static CommandLineParseResult Success(string gameFolder, string cloudFolder) =>
        new(gameFolder, cloudFolder, false, null);
}
