using Xunit;
using Zincked.App;

namespace Zincked.App.Tests;

public sealed class CommandLineParserTests
{
    [Fact]
    public void Parse_TwoPositionals_AssignsGameThenCloud()
    {
        CommandLineParseResult result = CommandLineParser.Parse([@"C:\game", @"C:\cloud"]);

        Assert.Equal(@"C:\game", result.GameFolder);
        Assert.Equal(@"C:\cloud", result.CloudFolder);
        Assert.False(result.HasError);
    }

    [Theory]
    [InlineData("--game", @"C:\game", "--cloud", @"C:\cloud")]
    [InlineData("--cloud", @"C:\cloud", "--game", @"C:\game")]
    [InlineData("-g", @"C:\game", "-c", @"C:\cloud")]
    [InlineData(@"--game=C:\game", @"--cloud=C:\cloud")]
    public void Parse_NamedOptions_ResolveRegardlessOfOrderOrSyntax(params string[] args)
    {
        CommandLineParseResult result = CommandLineParser.Parse(args);

        Assert.Equal(@"C:\game", result.GameFolder);
        Assert.Equal(@"C:\cloud", result.CloudFolder);
        Assert.False(result.HasError);
    }

    [Theory]
    [InlineData("--game", @"C:\game", @"C:\cloud")]   // cloud positional
    [InlineData(@"C:\game", "--cloud", @"C:\cloud")]  // game positional
    public void Parse_MixedPositionalAndNamed_FillsRemainingSlot(params string[] args)
    {
        CommandLineParseResult result = CommandLineParser.Parse(args);

        Assert.Equal(@"C:\game", result.GameFolder);
        Assert.Equal(@"C:\cloud", result.CloudFolder);
        Assert.False(result.HasError);
    }

    [Fact]
    public void Parse_CaseInsensitiveOptionNames_AreRecognized()
    {
        CommandLineParseResult result = CommandLineParser.Parse(["--GAME", @"C:\game", "--Cloud", @"C:\cloud"]);

        Assert.Equal(@"C:\game", result.GameFolder);
        Assert.Equal(@"C:\cloud", result.CloudFolder);
        Assert.False(result.HasError);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void Parse_HelpFlag_RequestsHelp(string flag)
    {
        CommandLineParseResult result = CommandLineParser.Parse([flag, @"C:\game", @"C:\cloud"]);

        Assert.True(result.HelpRequested);
    }

    [Fact]
    public void Parse_HelpFlagAfterErrors_StillRequestsHelp()
    {
        CommandLineParseResult result = CommandLineParser.Parse(["--bogus", "--help"]);

        Assert.True(result.HelpRequested);
        Assert.False(result.HasError);
    }

    [Fact]
    public void Parse_DuplicateGame_Fails()
    {
        CommandLineParseResult result = CommandLineParser.Parse(
            ["--game", @"C:\a", "--game", @"C:\b", "--cloud", @"C:\c"]);

        Assert.True(result.HasError);
    }

    [Fact]
    public void Parse_DuplicateCloud_Fails()
    {
        CommandLineParseResult result = CommandLineParser.Parse(
            ["--cloud", @"C:\a", "--cloud", @"C:\b", "--game", @"C:\c"]);

        Assert.True(result.HasError);
    }

    [Fact]
    public void Parse_UnknownOption_Fails()
    {
        CommandLineParseResult result = CommandLineParser.Parse(["--bogus", @"C:\game", @"C:\cloud"]);

        Assert.True(result.HasError);
        Assert.Contains("Unknown option", result.ErrorMessage);
    }

    [Theory]
    [InlineData("--game")]                     // no following value
    [InlineData("--game", "--cloud", @"C:\c")] // value looks like another option
    [InlineData("--game=")]                    // empty inline value
    public void Parse_OptionMissingValue_Fails(params string[] args)
    {
        CommandLineParseResult result = CommandLineParser.Parse(args);

        Assert.True(result.HasError);
        Assert.Contains("Missing value", result.ErrorMessage);
    }

    [Theory]
    [InlineData]                          // no arguments at all
    [InlineData("only-one")]
    [InlineData("--game", @"C:\game")]
    public void Parse_MissingRequiredFolder_Fails(params string[] args)
    {
        CommandLineParseResult result = CommandLineParser.Parse(args);

        Assert.True(result.HasError);
        Assert.Contains("required", result.ErrorMessage);
    }

    [Fact]
    public void Parse_TooManyPositionals_Fails()
    {
        CommandLineParseResult result = CommandLineParser.Parse([@"C:\a", @"C:\b", @"C:\c"]);

        Assert.True(result.HasError);
        Assert.Contains("Too many", result.ErrorMessage);
    }

    [Fact]
    public void Parse_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CommandLineParser.Parse(null!));
    }
}
