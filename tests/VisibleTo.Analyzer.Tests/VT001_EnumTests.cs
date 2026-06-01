namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class VT001_EnumTests
{
    private const string EnumDef = """
        using VisibleTo.Attributes;

        namespace MyApp.Domain
        {
            [VisibleTo("MyApp.Application")]
            public enum OrderStatus { Pending, Active, Completed }
        }

        """;

    [Fact]
    public async Task EnumMemberAccess_FromDeniedNamespace_RaisesVT001()
    {
        var source = EnumDef + """
            namespace MyApp.UI
            {
                public class Controller
                {
                    public void Handle()
                    {
                        var s = MyApp.Domain.OrderStatus.Active;
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("OrderStatus"));
    }

    [Fact]
    public async Task EnumMemberAccess_FromAllowedNamespace_NoDiagnostic()
    {
        var source = EnumDef + """
            namespace MyApp.Application
            {
                public class Service
                {
                    public void Handle()
                    {
                        var s = MyApp.Domain.OrderStatus.Active;
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EnumAsFieldType_FromDeniedNamespace_RaisesVT001()
    {
        var source = EnumDef + """
            namespace MyApp.UI
            {
                public class ViewModel
                {
                    public MyApp.Domain.OrderStatus Status { get; set; }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("OrderStatus"));
    }

    [Fact]
    public async Task EnumAsFieldType_FromAllowedNamespace_NoDiagnostic()
    {
        var source = EnumDef + """
            namespace MyApp.Application
            {
                public class Command
                {
                    public MyApp.Domain.OrderStatus Status { get; set; }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EnumAsMethodParameter_FromDeniedNamespace_RaisesVT001()
    {
        var source = EnumDef + """
            namespace MyApp.UI
            {
                public class Controller
                {
                    public void Handle(MyApp.Domain.OrderStatus status) { }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("OrderStatus"));
    }

    [Fact]
    public async Task EnumAsMethodParameter_FromAllowedNamespace_NoDiagnostic()
    {
        var source = EnumDef + """
            namespace MyApp.Application
            {
                public class Service
                {
                    public void Handle(MyApp.Domain.OrderStatus status) { }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }
}
