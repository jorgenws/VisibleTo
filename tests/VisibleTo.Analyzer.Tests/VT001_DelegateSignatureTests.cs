namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_DelegateSignatureTests
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
    public async Task Delegate_Parameter_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public delegate void EntityHandler(MyApp.Domain.Entity entity);
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task Delegate_Parameter_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Application
            {
                public delegate void EntityHandler(MyApp.Domain.Entity entity);
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Delegate_ReturnType_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public delegate MyApp.Domain.Entity EntityFactory();
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task Delegate_ReturnType_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Application
            {
                public delegate MyApp.Domain.Entity EntityFactory();
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }
}
