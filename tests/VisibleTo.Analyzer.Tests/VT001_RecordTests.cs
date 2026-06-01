namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class VT001_RecordTests
{
    private const string RecordDef = """
        using VisibleTo.Attributes;

        namespace MyApp.Domain
        {
            [VisibleTo("MyApp.Application")]
            public record Entity(int Id, string Name);
        }

        """;

    private const string RecordStructDef = """
        using VisibleTo.Attributes;

        namespace MyApp.Domain
        {
            [VisibleTo("MyApp.Application")]
            public record struct ValueObject(int Id, string Name);
        }

        """;

    [Fact]
    public async Task Record_AccessFromDeniedNamespace_RaisesVT001()
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
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("Entity"));
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
    public async Task Record_InMethodSignature_Denied_RaisesVT001()
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
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("Entity"));
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
    public async Task RecordStruct_AccessFromDeniedNamespace_RaisesVT001()
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
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("ValueObject"));
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
