namespace VisibleTo.Analyzer.Tests;

using VisibleTo.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class VT001_CrossNamespaceTests
{
    private const string Source = """
        using VisibleTo.Attributes;

        namespace VisibleToTest
        {
            [VisibleTo("VisibleToBackend.**")]
            public class TestClass { }
        }

        namespace VisibleToBackend
        {
            public class Class1
            {
                public Class1() { var x = new VisibleToTest.TestClass(); }
            }
        }

        namespace VisibleToFrontend
        {
            public class Class2
            {
                public Class2() { var y = new VisibleToTest.TestClass(); }
            }
        }
        """;

    [Fact]
    public async Task AllowedNamespace_NoDiagnostic()
    {
        // VisibleToBackend matches VisibleToBackend.** — no error
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(Source);
        Assert.DoesNotContain(diagnostics, d =>
            d.Id == "VT001" && d.GetMessage().Contains("VisibleToBackend.Class1"));
    }

    [Fact]
    public async Task DeniedNamespace_RaisesVT001()
    {
        // VisibleToFrontend does not match VisibleToBackend.** — error
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(Source);
        Assert.Contains(diagnostics, d => d.Id == "VT001" && d.GetMessage().Contains("VisibleToFrontend"));
    }
}
