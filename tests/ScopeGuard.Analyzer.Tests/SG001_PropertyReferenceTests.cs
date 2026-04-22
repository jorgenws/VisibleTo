namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_PropertyReferenceTests
{
    [Fact]
    public async Task PropertyGet_DeniedCaller_RaisesSG001()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application")]
                public class Entity
                {
                    public string Name { get; set; } = "";
                }
            }

            namespace MyApp.UI
            {
                public class View
                {
                    public string GetName()
                    {
                        var e = new MyApp.Domain.Entity();
                        return e.Name;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Name", sg001.GetMessage());
        Assert.Contains("MyApp.UI.View.GetName()", sg001.GetMessage());
    }

    [Fact]
    public async Task PropertySet_DeniedCaller_RaisesSG001()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application")]
                public class Entity
                {
                    public string Name { get; set; } = "";
                }
            }

            namespace MyApp.UI
            {
                public class View
                {
                    public void SetName()
                    {
                        var e = new MyApp.Domain.Entity();
                        e.Name = "test";
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Name", sg001.GetMessage());
        Assert.Contains("MyApp.UI.View.SetName()", sg001.GetMessage());
    }

    [Fact]
    public async Task PropertyGet_AllowedCaller_NoDiagnostic()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application.**")]
                public class Entity
                {
                    public string Name { get; set; } = "";
                }
            }

            namespace MyApp.Application.Handlers
            {
                public class Handler
                {
                    public string GetName()
                    {
                        var e = new MyApp.Domain.Entity();
                        return e.Name;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
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
                public class View
                {
                    public void Use()
                    {
                        var e = new MyApp.Domain.Entity();
                        var _ = e.Restricted;
                        var __ = e.Open;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Restricted", sg001.GetMessage());
    }
}
