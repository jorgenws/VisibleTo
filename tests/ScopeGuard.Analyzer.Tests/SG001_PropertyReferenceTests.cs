namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class SG001_PropertyReferenceTests
{
    private static string ClassLevelSource(string targetNs, string callerNs, string pattern, string propertyAccess) =>
        $$"""
        using ScopeGuard.Attributes;

        namespace {{targetNs}}
        {
            [AvailableTo("{{pattern}}")]
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
                    {{propertyAccess}}
                }
            }
        }
        """;

    [Theory]
    [InlineData("MyApp.Domain", "MyApp.Application", "MyApp.UI", "_ = e.Name;")]
    [InlineData("MyApp.Domain", "MyApp.Application", "MyApp.UI", "e.Name = \"x\";")]
    public async Task PropertyAccess_Denied_RaisesSG001(string targetNs, string pattern, string callerNs, string access)
    {
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(ClassLevelSource(targetNs, callerNs, pattern, access));
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Name", sg001.GetMessage());
    }

    [Theory]
    [InlineData("MyApp.Domain", "MyApp.Application.**", "MyApp.Application.Handlers", "_ = e.Name;")]
    [InlineData("MyApp.Domain", "MyApp.Application.**", "MyApp.Application.Handlers", "e.Name = \"x\";")]
    public async Task PropertyAccess_Allowed_NoDiagnostic(string targetNs, string pattern, string callerNs, string access)
    {
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(ClassLevelSource(targetNs, callerNs, pattern, access));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task PropertyAttributeOnly_OtherPropertiesFree()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                public class Entity
                {
                    [AvailableTo("MyApp.Application")]
                    public string Restricted { get; set; } = "";

                    public string Open { get; set; } = "";
                }
            }

            namespace MyApp.UI
            {
                public class Caller
                {
                    public void Call()
                    {
                        var e = new MyApp.Domain.Entity();
                        _ = e.Restricted;
                        _ = e.Open;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Restricted", sg001.GetMessage());
    }
}
