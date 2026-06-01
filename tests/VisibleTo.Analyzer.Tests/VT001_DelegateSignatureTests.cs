namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class VT001_DelegateSignatureTests
{
    private const string EntityDef = """
        using VisibleTo.Attributes;

        namespace MyApp.Domain
        {
            [VisibleTo("MyApp.Application")]
            public class Entity { }
        }

        """;

    [Fact]
    public async Task Delegate_Parameter_Denied_RaisesVT001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public delegate void EntityHandler(MyApp.Domain.Entity entity);
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var vt001 = Assert.Single(diagnostics, d => d.Id == "VT001");
        Assert.Contains("Entity", vt001.GetMessage());
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
    public async Task Delegate_ReturnType_Denied_RaisesVT001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public delegate MyApp.Domain.Entity EntityFactory();
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var vt001 = Assert.Single(diagnostics, d => d.Id == "VT001");
        Assert.Contains("Entity", vt001.GetMessage());
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
