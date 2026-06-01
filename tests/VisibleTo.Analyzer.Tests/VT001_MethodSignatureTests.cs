namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class VT001_MethodSignatureTests
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
    public async Task MethodParameter_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public class Handler
                {
                    public void Handle(MyApp.Domain.Entity entity) { }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var vt001 = Assert.Single(diagnostics, d => d.Id == "VT001");
        Assert.Contains("Entity", vt001.GetMessage());
    }

    [Fact]
    public async Task MethodParameter_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Application
            {
                public class Handler
                {
                    public void Handle(MyApp.Domain.Entity entity) { }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MethodReturnType_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public class Factory
                {
                    public MyApp.Domain.Entity Create() => null!;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var vt001 = Assert.Single(diagnostics, d => d.Id == "VT001");
        Assert.Contains("Entity", vt001.GetMessage());
    }

    [Fact]
    public async Task MethodReturnType_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Application
            {
                public class Factory
                {
                    public MyApp.Domain.Entity Create() => null!;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Constructor_Parameter_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public class Wrapper
                {
                    public Wrapper(MyApp.Domain.Entity entity) { }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var vt001 = Assert.Single(diagnostics, d => d.Id == "VT001");
        Assert.Contains("Entity", vt001.GetMessage());
    }

    [Fact]
    public async Task Constructor_Parameter_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Application
            {
                public class Wrapper
                {
                    public Wrapper(MyApp.Domain.Entity entity) { }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GenericParameter_RestrictedTypeArg_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public class Processor
                {
                    public void Process(System.Action<MyApp.Domain.Entity> callback) { }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var vt001 = Assert.Single(diagnostics, d => d.Id == "VT001");
        Assert.Contains("Entity", vt001.GetMessage());
    }
}
