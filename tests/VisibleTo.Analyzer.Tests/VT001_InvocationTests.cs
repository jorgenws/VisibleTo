namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class VT001_InvocationTests
{
    private static string Source(string targetNs, string callerNs) => $$"""
        using VisibleTo.Attributes;

        namespace {{targetNs}}
        {
            public class Service
            {
                public void Execute() { }
            }
        }

        namespace {{callerNs}}
        {
            public class Caller
            {
                public void Call({{targetNs}}.Service svc)
                {
                    svc.Execute();
                }
            }
        }
        """;

    private static string Source(string targetNs, string callerNs, params string[] patterns)
    {
        var attribute = $"\n    [VisibleTo({string.Join(", ", patterns.Select(p => $"\"{p}\""))})]";
        return $$"""
            using VisibleTo.Attributes;

            namespace {{targetNs}}
            {{{attribute}}
                public class Service
                {
                    public void Execute() { }
                }
            }

            namespace {{callerNs}}
            {
                public class Caller
                {
                    public void Call({{targetNs}}.Service svc)
                    {
                        svc.Execute();
                    }
                }
            }
            """;
    }

    [Theory]
    [InlineData("MyApp.Domain", "MyApp.Application.**", "MyApp.Application.UseCases")]
    [InlineData("MyApp.Domain", "MyApp.Application", "MyApp.Application")]
    public async Task Allowed_NoDiagnostic(string targetNs, string pattern, string callerNs)
    {
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(Source(targetNs, callerNs, pattern));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Allowed_MultiplePatterns_NoDiagnostic()
    {
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(
            Source("MyApp.Domain", "MyApp.Tests.Integration", "MyApp.Application", "MyApp.Tests.**"));
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("MyApp.Domain", "MyApp.Application.**", "MyApp.UI")]
    [InlineData("MyApp.Domain", "MyApp.Application", "MyApp.UI")]
    public async Task Denied_RaisesSG001(string targetNs, string pattern, string callerNs)
    {
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(Source(targetNs, callerNs, pattern));
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("Service"));
    }

    [Fact]
    public async Task NoAttribute_NoDiagnostic()
    {
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(Source("MyApp.Domain", "MyApp.UI"));
        Assert.Empty(diagnostics);
    }

}
