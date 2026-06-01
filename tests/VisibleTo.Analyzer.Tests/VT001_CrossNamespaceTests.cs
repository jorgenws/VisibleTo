namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_CrossNamespaceTests
{
    private const string Source = """
        using ScopeGuard.Attributes;

        namespace ScopeGuardTest
        {
            [VisibleTo("ScopeGuardBackend.**")]
            public class TestClass { }
        }

        namespace ScopeGuardBackend
        {
            public class Class1
            {
                public Class1() { var x = new ScopeGuardTest.TestClass(); }
            }
        }

        namespace ScopeGuardFrontend
        {
            public class Class2
            {
                public Class2() { var y = new ScopeGuardTest.TestClass(); }
            }
        }
        """;

    [Fact]
    public async Task AllowedNamespace_NoDiagnostic()
    {
        // ScopeGuardBackend matches ScopeGuardBackend.** — no error
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(Source);
        Assert.DoesNotContain(diagnostics, d =>
            d.Id == "SG001" && d.GetMessage().Contains("ScopeGuardBackend.Class1"));
    }

    [Fact]
    public async Task DeniedNamespace_RaisesSG001()
    {
        // ScopeGuardFrontend does not match ScopeGuardBackend.** — error
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(Source);
        Assert.Contains(diagnostics, d => d.Id == "SG001" && d.GetMessage().Contains("ScopeGuardFrontend"));
    }
}
