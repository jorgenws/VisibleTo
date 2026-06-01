namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_RecordTests
{
    private const string RecordDef = """
        using ScopeGuard.Attributes;

        namespace MyApp.Domain
        {
            [VisibleTo("MyApp.Application")]
            public record Entity(int Id, string Name);
        }

        """;

    private const string RecordStructDef = """
        using ScopeGuard.Attributes;

        namespace MyApp.Domain
        {
            [VisibleTo("MyApp.Application")]
            public record struct ValueObject(int Id, string Name);
        }

        """;

    [Fact]
    public async Task Record_AccessFromDeniedNamespace_RaisesSG001()
    {
        var source = RecordDef + """
            namespace MyApp.UI
            {
                public class Controller
                {
                    public void Handle()
                    {
                        var e = new MyApp.Domain.Entity(1, "test");
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "SG001" && d.GetMessage().Contains("Entity"));
    }

    [Fact]
    public async Task Record_AccessFromAllowedNamespace_NoDiagnostic()
    {
        var source = RecordDef + """
            namespace MyApp.Application
            {
                public class Service
                {
                    public void Handle()
                    {
                        var e = new MyApp.Domain.Entity(1, "test");
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Record_InMethodSignature_Denied_RaisesSG001()
    {
        var source = RecordDef + """
            namespace MyApp.UI
            {
                public class Controller
                {
                    public void Handle(MyApp.Domain.Entity entity) { }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "SG001" && d.GetMessage().Contains("Entity"));
    }

    [Fact]
    public async Task Record_InMethodSignature_Allowed_NoDiagnostic()
    {
        var source = RecordDef + """
            namespace MyApp.Application
            {
                public class Service
                {
                    public void Handle(MyApp.Domain.Entity entity) { }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task RecordStruct_AccessFromDeniedNamespace_RaisesSG001()
    {
        var source = RecordStructDef + """
            namespace MyApp.UI
            {
                public class Controller
                {
                    public void Handle()
                    {
                        var v = new MyApp.Domain.ValueObject(1, "test");
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "SG001" && d.GetMessage().Contains("ValueObject"));
    }

    [Fact]
    public async Task RecordStruct_AccessFromAllowedNamespace_NoDiagnostic()
    {
        var source = RecordStructDef + """
            namespace MyApp.Application
            {
                public class Service
                {
                    public void Handle()
                    {
                        var v = new MyApp.Domain.ValueObject(1, "test");
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }
}
