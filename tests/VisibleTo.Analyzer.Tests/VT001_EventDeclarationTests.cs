namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class VT001_EventDeclarationTests
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
    public async Task Event_Denied_RaisesVT001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public delegate void EntityChanged(MyApp.Domain.Entity entity);

                public class EventSource
                {
                    public event EntityChanged? OnChanged;
                }
            }
            """;
        // EntityChanged delegate fires vt001 for its parameter type,
        // EventSource.OnChanged fires vt001 for its event type.
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("Entity"));
    }

    [Fact]
    public async Task Event_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Application
            {
                public delegate void EntityChanged(MyApp.Domain.Entity entity);

                public class EventSource
                {
                    public event EntityChanged? OnChanged;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }
}
