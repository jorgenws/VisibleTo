namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_ObjectCreationTests
{
    private const string Preamble = """
        using ScopeGuard.Attributes;
        using System.Collections.Generic;

        namespace MyApp.Domain
        {
            [VisibleTo("MyApp.Application")]
            public class Entity { }
        }

        """;

    [Fact]
    public async Task ObjectCreation_GenericTypeArgument_Denied_RaisesSG001()
    {
        var source = Preamble + """
            namespace MyApp.UI
            {
                public class Caller
                {
                    public void Call()
                    {
                        var list = new List<MyApp.Domain.Entity>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task ObjectCreation_GenericTypeArgument_Allowed_NoDiagnostic()
    {
        var source = Preamble + """
            namespace MyApp.Application
            {
                public class Caller
                {
                    public void Call()
                    {
                        var list = new List<MyApp.Domain.Entity>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ObjectCreation_GenericTypeArgument_NoAttribute_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            namespace MyApp.Domain
            {
                public class Entity { }
            }

            namespace MyApp.UI
            {
                public class Caller
                {
                    public void Call()
                    {
                        var list = new List<MyApp.Domain.Entity>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ObjectCreation_MultipleTypeArguments_OnlyRestrictedRaisesSG001()
    {
        var source = Preamble + """
            namespace MyApp.UI
            {
                public class Caller
                {
                    public void Call()
                    {
                        var dict = new Dictionary<MyApp.Domain.Entity, string>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task ObjectCreation_MultipleTypeArguments_Allowed_NoDiagnostic()
    {
        var source = Preamble + """
            namespace MyApp.Application
            {
                public class Caller
                {
                    public void Call()
                    {
                        var dict = new Dictionary<MyApp.Domain.Entity, string>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ObjectCreation_NestedGenericTypeArgument_Denied_RaisesSG001()
    {
        var source = Preamble + """
            namespace MyApp.UI
            {
                public class Caller
                {
                    public void Call()
                    {
                        var nested = new List<List<MyApp.Domain.Entity>>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task ObjectCreation_NestedGenericTypeArgument_Allowed_NoDiagnostic()
    {
        var source = Preamble + """
            namespace MyApp.Application
            {
                public class Caller
                {
                    public void Call()
                    {
                        var nested = new List<List<MyApp.Domain.Entity>>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }
}
