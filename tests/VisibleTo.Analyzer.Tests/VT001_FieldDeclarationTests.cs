namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class VT001_FieldDeclarationTests
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
    public async Task Field_Denied_RaisesVT001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public class Store
                {
                    private MyApp.Domain.Entity _entity;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var vt001 = Assert.Single(diagnostics, d => d.Id == "VT001");
        Assert.Contains("Entity", vt001.GetMessage());
    }

    [Fact]
    public async Task Field_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Application
            {
                public class Store
                {
                    private MyApp.Domain.Entity _entity;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Field_GenericTypeArg_Denied_RaisesVT001()
    {
        var source = EntityDef + """
            using System.Collections.Generic;

            namespace MyApp.UI
            {
                public class Store
                {
                    private List<MyApp.Domain.Entity> _entities;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var vt001 = Assert.Single(diagnostics, d => d.Id == "VT001");
        Assert.Contains("Entity", vt001.GetMessage());
    }
}
