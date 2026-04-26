namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_EventDeclarationTests
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
    public async Task Event_Denied_RaisesSG001()
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
        // EntityChanged delegate fires SG001 for its parameter type,
        // EventSource.OnChanged fires SG001 for its event type.
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "SG001" && d.GetMessage().Contains("Entity"));
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
