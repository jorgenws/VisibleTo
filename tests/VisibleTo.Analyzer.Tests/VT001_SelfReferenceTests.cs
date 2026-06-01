namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class VT001_SelfReferenceTests
{
    [Fact]
    public async Task AccessingOwnMembers_NoDiagnostic()
    {
        var source = """
            using VisibleTo.Attributes;

            namespace MyApp.Domain
            {
                [VisibleTo("MyApp.Application")]
                public class User
                {
                    public string Name { get; set; } = "";

                    public string GetDisplayName()
                    {
                        return this.Name;
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReturningOwnType_NoDiagnostic()
    {
        var source = """
            using VisibleTo.Attributes;

            namespace MyApp.Domain
            {
                [VisibleTo("MyApp.Application")]
                public class User
                {
                    public User Clone() => new User();
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AcceptingOwnTypeAsParameter_NoDiagnostic()
    {
        var source = """
            using VisibleTo.Attributes;

            namespace MyApp.Domain
            {
                [VisibleTo("MyApp.Application")]
                public class User
                {
                    public bool Equals(User other) => false;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }
}
