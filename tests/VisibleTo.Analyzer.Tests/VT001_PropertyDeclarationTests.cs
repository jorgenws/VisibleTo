namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_PropertyDeclarationTests
{
    private const string EntityDef = """
        using ScopeGuard.Attributes;

        namespace MyApp.Domain
        {
            [VisibleTo("MyApp.Application")]
            public class Entity { }
        }

        """;

    [Fact]
    public async Task Property_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public class ViewModel
                {
                    public MyApp.Domain.Entity Current { get; set; }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task Property_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Application
            {
                public class ViewModel
                {
                    public MyApp.Domain.Entity Current { get; set; }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Property_GenericTypeArg_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            using System.Collections.Generic;

            namespace MyApp.UI
            {
                public class ViewModel
                {
                    public IReadOnlyList<MyApp.Domain.Entity> Items { get; set; }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }
}
