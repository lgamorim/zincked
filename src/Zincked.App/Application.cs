using System.IO.Abstractions;
using Zincked.Core;

namespace Zincked.App;

/// <summary>
/// The console host: parses command-line arguments, drives an <see cref="IFolderSynchronizer"/>,
/// and reports the outcome. Kept free of static <c>Console</c> calls so it can be unit tested.
/// </summary>
public sealed class Application
{
    /// <summary>Exit code returned on a successful synchronization.</summary>
    public const int SuccessExitCode = 0;

    /// <summary>Exit code returned when synchronization could not be attempted.</summary>
    public const int ErrorExitCode = 1;

    /// <summary>Exit code returned for invalid usage (bad arguments or <c>--help</c>).</summary>
    public const int UsageExitCode = 2;

    private readonly IFolderSynchronizer _synchronizer;
    private readonly IFileSystem _fileSystem;
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="synchronizer">The engine that performs the synchronization.</param>
    /// <param name="fileSystem">The file system used to validate the supplied folders.</param>
    /// <param name="output">Where normal output is written.</param>
    /// <param name="error">Where error and usage messages are written.</param>
    public Application(
        IFolderSynchronizer synchronizer,
        IFileSystem fileSystem,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(synchronizer);
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        _synchronizer = synchronizer;
        _fileSystem = fileSystem;
        _output = output;
        _error = error;
    }

    /// <summary>Runs the application for the given command-line arguments.</summary>
    /// <param name="args">The raw command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    public int Run(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        CommandLineParseResult parsed = CommandLineParser.Parse(args);

        if (parsed.HelpRequested)
        {
            WriteUsage(_output);
            return UsageExitCode;
        }

        if (parsed.HasError)
        {
            _error.WriteLine(parsed.ErrorMessage);
            WriteUsage(_error);
            return UsageExitCode;
        }

        string gameFolder = parsed.GameFolder!;
        string cloudFolder = parsed.CloudFolder!;

        if (!_fileSystem.Directory.Exists(gameFolder))
        {
            _error.WriteLine($"Game folder not found: {gameFolder}");
            return ErrorExitCode;
        }

        // The cloud folder may not exist yet on a brand-new machine; create it so the first
        // run succeeds.
        _fileSystem.Directory.CreateDirectory(cloudFolder);

        SyncResult result = _synchronizer.Synchronize(gameFolder, cloudFolder, parsed.Mode);
        WriteSummary(result, gameFolder, cloudFolder, parsed.Mode);
        return SuccessExitCode;
    }

    private void WriteSummary(SyncResult result, string gameFolder, string cloudFolder, SyncMode mode)
    {
        _output.WriteLine($"Synchronized '{gameFolder}' {DirectionArrow(mode)} '{cloudFolder}'.");
        _output.WriteLine($"  Copied to cloud: {result.CopiedToSecond}");
        _output.WriteLine($"  Copied to game:  {result.CopiedToFirst}");
        _output.WriteLine($"  Already in sync: {result.UpToDate}");
    }

    // The game folder is on the left and the cloud folder on the right, so '->' reads as
    // "into the cloud" and '<-' as "into the game folder".
    private static string DirectionArrow(SyncMode mode) => mode switch
    {
        SyncMode.FirstToSecond => "->",
        SyncMode.SecondToFirst => "<-",
        _ => "<->",
    };

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("Zincked - two-way sync for game save folders.");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  Zincked <gameFolder> <cloudFolder> [--mode <both|up|down>]");
        writer.WriteLine("  Zincked --game <gameFolder> --cloud <cloudFolder> [--mode <both|up|down>]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  -g, --game <path>         The local game folder.");
        writer.WriteLine("  -c, --cloud <path>        The shared cloud folder.");
        writer.WriteLine("  -m, --mode <both|up|down> Sync direction: both (default, two-way),");
        writer.WriteLine("                            up (game -> cloud), down (cloud -> game).");
        writer.WriteLine("  -h, --help                Show this help.");
        writer.WriteLine();
        writer.WriteLine("The folders may be given positionally or by option, in any order.");
        writer.WriteLine("The newer copy of each file wins, and files are never deleted.");
    }
}
