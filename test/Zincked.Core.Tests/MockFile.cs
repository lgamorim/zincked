using System.IO.Abstractions.TestingHelpers;

namespace Zincked.Core.Tests;

/// <summary>
/// Helpers for building <see cref="MockFileData"/> with an explicit UTC last-write time,
/// keeping the test bodies focused on the scenario under test.
/// </summary>
internal static class MockFile
{
    /// <summary>Creates text file data stamped with the given UTC last-write time.</summary>
    public static MockFileData Text(string content, DateTime lastWriteTimeUtc) =>
        Stamp(new MockFileData(content), lastWriteTimeUtc);

    /// <summary>Creates binary file data stamped with the given UTC last-write time.</summary>
    public static MockFileData Binary(byte[] content, DateTime lastWriteTimeUtc) =>
        Stamp(new MockFileData(content), lastWriteTimeUtc);

    private static MockFileData Stamp(MockFileData data, DateTime lastWriteTimeUtc)
    {
        var utc = DateTime.SpecifyKind(lastWriteTimeUtc, DateTimeKind.Utc);
        data.LastWriteTime = new DateTimeOffset(utc);
        return data;
    }
}
