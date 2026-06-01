namespace VisibleTo.Analyzer.Tests;

using Xunit;

public class VT001_PatternMatchingTests
{
    [Theory]
    [InlineData("MyApp.Domain.Entities", true)]
    [InlineData("MyApp.Domain.Entities.Sub", true)]
    [InlineData("MyApp.Domain", true)]
    [InlineData("MyApp.Other.Entities", false)]
    [InlineData("Other.Domain.Entities", false)]
    public void DoubleStarMatchesAcrossSegments(string caller, bool expected)
    {
        Assert.Equal(expected, PatternMatcher.IsMatch(caller, "MyApp.Domain.**"));
    }

    [Theory]
    [InlineData("MyApp.OrderRepository", true)]
    [InlineData("MyApp.UserRepository", true)]
    [InlineData("MyApp.Sub.OrderRepository", false)]
    [InlineData("MyApp.Order", false)]
    [InlineData("Other.OrderRepository", false)]
    public void SingleStarMatchesWithinSegment(string caller, bool expected)
    {
        Assert.Equal(expected, PatternMatcher.IsMatch(caller, "MyApp.*Repository"));
    }

    [Theory]
    [InlineData("MyApp.Domain.Services", true)]
    [InlineData("MyApp.Domain.Service", false)]
    [InlineData("MyApp.Domain.Services.Extra", false)]
    [InlineData("Other.Domain.Services", false)]
    public void ExactStringMatchesLiterally(string caller, bool expected)
    {
        Assert.Equal(expected, PatternMatcher.IsMatch(caller, "MyApp.Domain.Services"));
    }

    [Theory]
    [InlineData("MyApp.Anything")]
    [InlineData("Completely.Different")]
    [InlineData("")]
    public void DoubleStarAloneMatchesEverything(string caller)
    {
        Assert.True(PatternMatcher.IsMatch(caller, "**"));
    }

    [Fact]
    public void EmptyPatternMatchesNothing()
    {
        Assert.False(PatternMatcher.IsMatch("MyApp.Domain", ""));
    }

    [Theory]
    [InlineData("MyApp.FooService", true)]
    [InlineData("MyApp.BarService", true)]
    [InlineData("MyApp.FooRepository", false)]
    public void SingleStarInMiddleMatchesSegment(string caller, bool expected)
    {
        Assert.Equal(expected, PatternMatcher.IsMatch(caller, "MyApp.*Service"));
    }
}
