using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Zincked.Core.Tests;

public sealed class FolderSynchronizerTests
{
    private const string GameRoot = @"C:\game";
    private const string CloudRoot = @"C:\cloud";

    private static readonly DateTime Older = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Newer = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Synchronize_FileOnlyInFirst_CopiesToSecond()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("progress", Newer));
        fileSystem.AddDirectory(CloudRoot);
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.True(fileSystem.FileExists(@"C:\cloud\save.dat"));
        Assert.Equal("progress", fileSystem.File.ReadAllText(@"C:\cloud\save.dat"));
        Assert.Equal(1, result.CopiedToSecond);
        Assert.Equal(0, result.CopiedToFirst);
    }

    [Fact]
    public void Synchronize_FileOnlyInSecond_CopiesToFirst()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(GameRoot);
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("progress", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.True(fileSystem.FileExists(@"C:\game\save.dat"));
        Assert.Equal("progress", fileSystem.File.ReadAllText(@"C:\game\save.dat"));
        Assert.Equal(1, result.CopiedToFirst);
        Assert.Equal(0, result.CopiedToSecond);
    }

    [Fact]
    public void Synchronize_FirstIsNewer_OverwritesSecond()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("new", Newer));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("old", Older));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.Equal("new", fileSystem.File.ReadAllText(@"C:\cloud\save.dat"));
        Assert.Equal(SyncDirection.FirstToSecond, Assert.Single(result.Files).Direction);
    }

    [Fact]
    public void Synchronize_SecondIsNewer_OverwritesFirst()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("old", Older));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("new", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.Equal("new", fileSystem.File.ReadAllText(@"C:\game\save.dat"));
        Assert.Equal(SyncDirection.SecondToFirst, Assert.Single(result.Files).Direction);
    }

    [Fact]
    public void Synchronize_EqualTimestamps_DoesNothing()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("game", Newer));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("cloud", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        // No copy: both keep their original (differing) contents.
        Assert.Equal("game", fileSystem.File.ReadAllText(@"C:\game\save.dat"));
        Assert.Equal("cloud", fileSystem.File.ReadAllText(@"C:\cloud\save.dat"));
        Assert.Equal(1, result.UpToDate);
    }

    [Fact]
    public void Synchronize_DifferenceWithinTolerance_DoesNothing()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("game", Newer));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("cloud", Newer.AddSeconds(1)));
        var synchronizer = new FolderSynchronizer(fileSystem); // default 2s tolerance

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.Equal("game", fileSystem.File.ReadAllText(@"C:\game\save.dat"));
        Assert.Equal("cloud", fileSystem.File.ReadAllText(@"C:\cloud\save.dat"));
        Assert.Equal(1, result.UpToDate);
    }

    [Fact]
    public void Synchronize_DifferenceJustBeyondTolerance_Copies()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("game", Newer.AddSeconds(3)));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("cloud", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem); // default 2s tolerance

        synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.Equal("game", fileSystem.File.ReadAllText(@"C:\cloud\save.dat"));
    }

    [Fact]
    public void Synchronize_NestedSubfolders_AreSyncedRecursively()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\profiles\slot1\save.dat", MockFile.Text("one", Newer));
        fileSystem.AddFile(@"C:\cloud\profiles\slot2\save.dat", MockFile.Text("two", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.True(fileSystem.FileExists(@"C:\cloud\profiles\slot1\save.dat"));
        Assert.True(fileSystem.FileExists(@"C:\game\profiles\slot2\save.dat"));
    }

    [Fact]
    public void Synchronize_CopyPreservesTimestamp_SecondRunIsNoOp()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("progress", Newer));
        fileSystem.AddDirectory(CloudRoot);
        var synchronizer = new FolderSynchronizer(fileSystem);

        synchronizer.Synchronize(GameRoot, CloudRoot);
        SyncResult secondRun = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.Equal(Newer, fileSystem.File.GetLastWriteTimeUtc(@"C:\cloud\save.dat"));
        Assert.Equal(0, secondRun.CopiedToSecond);
        Assert.Equal(0, secondRun.CopiedToFirst);
        Assert.Equal(1, secondRun.UpToDate);
    }

    [Fact]
    public void Synchronize_IdenticalTrees_PerformNoCopies()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("same", Newer));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("same", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.Equal(1, result.UpToDate);
        Assert.Equal(0, result.CopiedToFirst);
        Assert.Equal(0, result.CopiedToSecond);
    }

    [Fact]
    public void Synchronize_EmptyFolders_ProduceEmptyResult()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(GameRoot);
        fileSystem.AddDirectory(CloudRoot);
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.Empty(result.Files);
    }

    [Fact]
    public void Synchronize_MissingFolders_AreCreated()
    {
        var fileSystem = new MockFileSystem();
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.True(fileSystem.Directory.Exists(GameRoot));
        Assert.True(fileSystem.Directory.Exists(CloudRoot));
        Assert.Empty(result.Files);
    }

    [Fact]
    public void Synchronize_SamePathForBothRoots_IsNoOp()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("progress", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, GameRoot);

        Assert.Equal("progress", fileSystem.File.ReadAllText(@"C:\game\save.dat"));
        Assert.Equal(SyncDirection.None, Assert.Single(result.Files).Direction);
    }

    [Fact]
    public void Synchronize_BinaryContent_IsCopiedVerbatim()
    {
        byte[] bytes = [0x00, 0xFF, 0x10, 0x7F, 0x80, 0x00, 0x42];
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.bin", MockFile.Binary(bytes, Newer));
        fileSystem.AddDirectory(CloudRoot);
        var synchronizer = new FolderSynchronizer(fileSystem);

        synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.Equal(bytes, fileSystem.File.ReadAllBytes(@"C:\cloud\save.bin"));
    }

    [Fact]
    public void Synchronize_SpecialCharactersInNames_AreHandled()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save (1) naïve.dat", MockFile.Text("ok", Newer));
        fileSystem.AddDirectory(CloudRoot);
        var synchronizer = new FolderSynchronizer(fileSystem);

        synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.True(fileSystem.FileExists(@"C:\cloud\save (1) naïve.dat"));
    }

    [Fact]
    public void Synchronize_EmptyDirectories_AreReplicatedByDefault()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\game\screenshots");
        fileSystem.AddDirectory(CloudRoot);
        var synchronizer = new FolderSynchronizer(fileSystem);

        synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.True(fileSystem.Directory.Exists(@"C:\cloud\screenshots"));
    }

    [Fact]
    public void Synchronize_EmptyDirectoryReplicationDisabled_DoesNotCreateThem()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\game\screenshots");
        fileSystem.AddDirectory(CloudRoot);
        var options = new SyncOptions { ReplicateEmptyDirectories = false };
        var synchronizer = new FolderSynchronizer(fileSystem, options);

        synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.False(fileSystem.Directory.Exists(@"C:\cloud\screenshots"));
    }

    [Fact]
    public void Synchronize_MixedTree_ReportsEachDirection()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\only-game.dat", MockFile.Text("g", Newer));
        fileSystem.AddFile(@"C:\cloud\only-cloud.dat", MockFile.Text("c", Newer));
        fileSystem.AddFile(@"C:\game\both.dat", MockFile.Text("new", Newer));
        fileSystem.AddFile(@"C:\cloud\both.dat", MockFile.Text("old", Older));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot);

        Assert.Equal(2, result.CopiedToSecond); // only-game.dat + both.dat
        Assert.Equal(1, result.CopiedToFirst);  // only-cloud.dat
        Assert.Equal(3, result.Files.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Synchronize_BlankFolderArgument_Throws(string? folder)
    {
        var synchronizer = new FolderSynchronizer(new MockFileSystem());

        Assert.ThrowsAny<ArgumentException>(() => synchronizer.Synchronize(folder!, CloudRoot));
    }

    [Fact]
    public void Synchronize_FirstToSecond_PushesNewFileButDoesNotPull()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\push.dat", MockFile.Text("g", Newer));
        fileSystem.AddFile(@"C:\cloud\keep.dat", MockFile.Text("c", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot, SyncMode.FirstToSecond);

        Assert.True(fileSystem.FileExists(@"C:\cloud\push.dat"));   // pushed
        Assert.False(fileSystem.FileExists(@"C:\game\keep.dat"));   // not pulled back
        Assert.Equal(1, result.CopiedToSecond);
        Assert.Equal(0, result.CopiedToFirst);
    }

    [Fact]
    public void Synchronize_FirstToSecond_DoesNotOverwriteFirstWhenSecondIsNewer()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("old", Older));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("new", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot, SyncMode.FirstToSecond);

        // The first (game) folder is never written to in this mode, even though cloud is newer.
        Assert.Equal("old", fileSystem.File.ReadAllText(@"C:\game\save.dat"));
        Assert.Equal(0, result.CopiedToFirst);
        Assert.Equal(1, result.UpToDate);
    }

    [Fact]
    public void Synchronize_FirstToSecond_OverwritesSecondWhenFirstIsNewer()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("new", Newer));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("old", Older));
        var synchronizer = new FolderSynchronizer(fileSystem);

        synchronizer.Synchronize(GameRoot, CloudRoot, SyncMode.FirstToSecond);

        Assert.Equal("new", fileSystem.File.ReadAllText(@"C:\cloud\save.dat"));
    }

    [Fact]
    public void Synchronize_SecondToFirst_PullsNewFileButDoesNotPush()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\cloud\pull.dat", MockFile.Text("c", Newer));
        fileSystem.AddFile(@"C:\game\keep.dat", MockFile.Text("g", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot, SyncMode.SecondToFirst);

        Assert.True(fileSystem.FileExists(@"C:\game\pull.dat"));    // pulled
        Assert.False(fileSystem.FileExists(@"C:\cloud\keep.dat"));  // not pushed
        Assert.Equal(1, result.CopiedToFirst);
        Assert.Equal(0, result.CopiedToSecond);
    }

    [Fact]
    public void Synchronize_SecondToFirst_DoesNotOverwriteSecondWhenFirstIsNewer()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("new", Newer));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("old", Older));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot, SyncMode.SecondToFirst);

        Assert.Equal("old", fileSystem.File.ReadAllText(@"C:\cloud\save.dat"));
        Assert.Equal(0, result.CopiedToSecond);
        Assert.Equal(1, result.UpToDate);
    }

    [Fact]
    public void Synchronize_SecondToFirst_OverwritesFirstWhenSecondIsNewer()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("old", Older));
        fileSystem.AddFile(@"C:\cloud\save.dat", MockFile.Text("new", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, CloudRoot, SyncMode.SecondToFirst);

        Assert.Equal("new", fileSystem.File.ReadAllText(@"C:\game\save.dat"));
        Assert.Equal(1, result.CopiedToFirst);
    }

    [Theory]
    [InlineData(SyncMode.Bidirectional)]
    [InlineData(SyncMode.FirstToSecond)]
    [InlineData(SyncMode.SecondToFirst)]
    public void Synchronize_SamePathForBothRoots_IsNoOpRegardlessOfMode(SyncMode mode)
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(@"C:\game\save.dat", MockFile.Text("progress", Newer));
        var synchronizer = new FolderSynchronizer(fileSystem);

        SyncResult result = synchronizer.Synchronize(GameRoot, GameRoot, mode);

        Assert.Equal("progress", fileSystem.File.ReadAllText(@"C:\game\save.dat"));
        Assert.Equal(SyncDirection.None, Assert.Single(result.Files).Direction);
    }

    [Fact]
    public void Synchronize_OneWay_OnlyReplicatesEmptyDirectoriesInThatDirection()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\game\game-only");
        fileSystem.AddDirectory(@"C:\cloud\cloud-only");
        var synchronizer = new FolderSynchronizer(fileSystem);

        synchronizer.Synchronize(GameRoot, CloudRoot, SyncMode.FirstToSecond);

        Assert.True(fileSystem.Directory.Exists(@"C:\cloud\game-only"));   // replicated forward
        Assert.False(fileSystem.Directory.Exists(@"C:\game\cloud-only"));  // not replicated back
    }
}
