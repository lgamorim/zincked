using System.IO.Abstractions.TestingHelpers;
using NSubstitute;
using Xunit;
using Zincked.App;
using Zincked.Core;

namespace Zincked.App.Tests;

public sealed class ApplicationTests
{
    private const string GameRoot = @"C:\game";
    private const string CloudRoot = @"C:\cloud";

    [Fact]
    public void Run_ValidArguments_InvokesSynchronizerAndSucceeds()
    {
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        synchronizer.Synchronize(GameRoot, CloudRoot).Returns(new SyncResult([]));
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(GameRoot);
        var application = NewApplication(synchronizer, fileSystem, out _, out _);

        int exitCode = application.Run([GameRoot, CloudRoot]);

        Assert.Equal(Application.SuccessExitCode, exitCode);
        synchronizer.Received(1).Synchronize(GameRoot, CloudRoot);
    }

    [Fact]
    public void Run_NamedOptions_InvokeSynchronizerWithResolvedFolders()
    {
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        synchronizer.Synchronize(GameRoot, CloudRoot).Returns(new SyncResult([]));
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(GameRoot);
        var application = NewApplication(synchronizer, fileSystem, out _, out _);

        int exitCode = application.Run(["--cloud", CloudRoot, "--game", GameRoot]);

        Assert.Equal(Application.SuccessExitCode, exitCode);
        synchronizer.Received(1).Synchronize(GameRoot, CloudRoot);
    }

    [Fact]
    public void Run_ModeOption_IsForwardedToSynchronizer()
    {
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        synchronizer.Synchronize(GameRoot, CloudRoot, SyncMode.FirstToSecond).Returns(new SyncResult([]));
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(GameRoot);
        var application = NewApplication(synchronizer, fileSystem, out _, out _);

        int exitCode = application.Run([GameRoot, CloudRoot, "--mode", "up"]);

        Assert.Equal(Application.SuccessExitCode, exitCode);
        synchronizer.Received(1).Synchronize(GameRoot, CloudRoot, SyncMode.FirstToSecond);
    }

    [Fact]
    public void Run_MissingCloudFolder_IsCreatedBeforeSync()
    {
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        synchronizer.Synchronize(Arg.Any<string>(), Arg.Any<string>()).Returns(new SyncResult([]));
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(GameRoot);
        var application = NewApplication(synchronizer, fileSystem, out _, out _);

        application.Run([GameRoot, CloudRoot]);

        Assert.True(fileSystem.Directory.Exists(CloudRoot));
    }

    [Fact]
    public void Run_SummaryIncludesCounts()
    {
        var result = new SyncResult(
        [
            new SyncedFile("a", SyncDirection.FirstToSecond),
            new SyncedFile("b", SyncDirection.SecondToFirst),
            new SyncedFile("c", SyncDirection.None),
        ]);
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        synchronizer.Synchronize(GameRoot, CloudRoot).Returns(result);
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(GameRoot);
        var application = NewApplication(synchronizer, fileSystem, out var output, out _);

        application.Run([GameRoot, CloudRoot]);

        string text = output.ToString();
        Assert.Contains("Copied to cloud: 1", text);
        Assert.Contains("Copied to game:  1", text);
        Assert.Contains("Already in sync: 1", text);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Run_WrongArgumentCount_ReturnsUsageCodeAndDoesNotSync(int argumentCount)
    {
        var args = Enumerable.Range(0, argumentCount).Select(i => $"arg{i}").ToArray();
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        var application = NewApplication(synchronizer, new MockFileSystem(), out _, out var error);

        int exitCode = application.Run(args);

        Assert.Equal(Application.UsageExitCode, exitCode);
        synchronizer.DidNotReceiveWithAnyArgs().Synchronize(default!, default!);
        Assert.Contains("Usage:", error.ToString());
    }

    [Fact]
    public void Run_ParseError_WritesReasonAndUsageToError()
    {
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        var application = NewApplication(synchronizer, new MockFileSystem(), out _, out var error);

        int exitCode = application.Run(["--bogus", GameRoot, CloudRoot]);

        Assert.Equal(Application.UsageExitCode, exitCode);
        string text = error.ToString();
        Assert.Contains("Unknown option", text);
        Assert.Contains("Usage:", text);
        synchronizer.DidNotReceiveWithAnyArgs().Synchronize(default!, default!);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void Run_HelpFlag_PrintsUsageAndDoesNotSync(string flag)
    {
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        var application = NewApplication(synchronizer, new MockFileSystem(), out var output, out _);

        int exitCode = application.Run([flag]);

        Assert.Equal(Application.UsageExitCode, exitCode);
        synchronizer.DidNotReceiveWithAnyArgs().Synchronize(default!, default!);
        Assert.Contains("Usage:", output.ToString());
    }

    [Fact]
    public void Run_GameFolderMissing_ReturnsErrorAndDoesNotSync()
    {
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        var fileSystem = new MockFileSystem(); // game folder does not exist
        var application = NewApplication(synchronizer, fileSystem, out _, out var error);

        int exitCode = application.Run([GameRoot, CloudRoot]);

        Assert.Equal(Application.ErrorExitCode, exitCode);
        synchronizer.DidNotReceiveWithAnyArgs().Synchronize(default!, default!);
        Assert.Contains("Game folder not found", error.ToString());
    }

    [Fact]
    public void Run_SynchronizerThrows_ExceptionPropagates()
    {
        var synchronizer = Substitute.For<IFolderSynchronizer>();
        synchronizer.Synchronize(GameRoot, CloudRoot).Returns(_ => throw new IOException("disk full"));
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(GameRoot);
        var application = NewApplication(synchronizer, fileSystem, out _, out _);

        Assert.Throws<IOException>(() => application.Run([GameRoot, CloudRoot]));
    }

    [Fact]
    public void Run_NullArgs_Throws()
    {
        var application = NewApplication(
            Substitute.For<IFolderSynchronizer>(), new MockFileSystem(), out _, out _);

        Assert.Throws<ArgumentNullException>(() => application.Run(null!));
    }

    [Fact]
    public void Constructor_NullSynchronizer_Throws()
    {
        var writer = new StringWriter();
        Assert.Throws<ArgumentNullException>(
            () => new Application(null!, new MockFileSystem(), writer, writer));
    }

    [Fact]
    public void Constructor_NullFileSystem_Throws()
    {
        var writer = new StringWriter();
        Assert.Throws<ArgumentNullException>(
            () => new Application(Substitute.For<IFolderSynchronizer>(), null!, writer, writer));
    }

    [Fact]
    public void Constructor_NullOutput_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Application(Substitute.For<IFolderSynchronizer>(), new MockFileSystem(), null!, new StringWriter()));
    }

    [Fact]
    public void Constructor_NullError_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Application(Substitute.For<IFolderSynchronizer>(), new MockFileSystem(), new StringWriter(), null!));
    }

    private static Application NewApplication(
        IFolderSynchronizer synchronizer,
        MockFileSystem fileSystem,
        out StringWriter output,
        out StringWriter error)
    {
        output = new StringWriter();
        error = new StringWriter();
        return new Application(synchronizer, fileSystem, output, error);
    }
}
