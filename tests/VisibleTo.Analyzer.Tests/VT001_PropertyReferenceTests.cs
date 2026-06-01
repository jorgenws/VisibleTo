namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class VT001_PropertyReferenceTests
{
    private static string ClassLevelSource(string targetNs, string callerNs, string pattern, string propertyAccess) =>
        $$"""
        using VisibleTo.Attributes;

        namespace {{targetNs}}
        {
            [VisibleTo("{{pattern}}")]
            public class Entity
            {
                public string Name { get; set; } = "";
            }
        }

        namespace {{callerNs}}
        {
            public class Caller
            {
                public void Call({{targetNs}}.Entity e)
                {
                    {{propertyAccess}}
                }
            }
        }
        """;

    [Theory]
    [InlineData("MyApp.Domain", "MyApp.Application", "MyApp.UI", "_ = e.Name;")]
    [InlineData("MyApp.Domain", "MyApp.Application", "MyApp.UI", "e.Name = \"x\";")]
    public async Task PropertyAccess_Denied_RaisesVT001(string targetNs, string pattern, string callerNs, string access)
    {
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(ClassLevelSource(targetNs, callerNs, pattern, access));
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("Entity"));
    }

    [Theory]
    [InlineData("MyApp.Domain", "MyApp.Application.**", "MyApp.Application.Handlers", "_ = e.Name;")]
    [InlineData("MyApp.Domain", "MyApp.Application.**", "MyApp.Application.Handlers", "e.Name = \"x\";")]
    public async Task PropertyAccess_Allowed_NoDiagnostic(string targetNs, string pattern, string callerNs, string access)
    {
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(ClassLevelSource(targetNs, callerNs, pattern, access));
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("MyApp.Domain", "MyApp.UI", "_ = e.Name;")]
    [InlineData("MyApp.Domain", "MyApp.UI", "e.Name = \"x\";")]
    public async Task PropertyAccess_NoAttribute_NoDiagnostic(string targetNs, string callerNs, string access)
    {
        var source = $$"""
            namespace {{targetNs}}
            {
                public class Entity
                {
                    public string Name { get; set; } = "";
                }
            }

            namespace {{callerNs}}
            {
                public class Caller
                {
                    public void Call()
                    {
                        var e = new {{targetNs}}.Entity();
                        {{access}}
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

}
