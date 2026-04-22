namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_PropertyReferenceTests
{
    [Fact]
    public async Task PropertyGet_DeniedCaller_RaisesSG001()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application")]
                public class Entity
                {
                    public string Name { get; set; } = "";
                }
            }

            namespace MyApp.UI
            {
                public class View
                {
                    public string GetName()
                    {
                        var e = new MyApp.Domain.Entity();
                        return {|#0:e.Name|};
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier.Diagnostic()
            .WithLocation(0)
            .WithArguments("Name", "MyApp.Application", "MyApp.UI.View.GetName()");

        await AnalyzerVerifier.VerifyAsync(source, expected);
    }

    [Fact]
    public async Task PropertySet_DeniedCaller_RaisesSG001()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application")]
                public class Entity
                {
                    public string Name { get; set; } = "";
                }
            }

            namespace MyApp.UI
            {
                public class View
                {
                    public void SetName()
                    {
                        var e = new MyApp.Domain.Entity();
                        {|#0:e.Name|} = "test";
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier.Diagnostic()
            .WithLocation(0)
            .WithArguments("Name", "MyApp.Application", "MyApp.UI.View.SetName()");

        await AnalyzerVerifier.VerifyAsync(source, expected);
    }

    [Fact]
    public async Task PropertyGet_AllowedCaller_NoDiagnostic()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application.**")]
                public class Entity
                {
                    public string Name { get; set; } = "";
                }
            }

            namespace MyApp.Application.Handlers
            {
                public class Handler
                {
                    public string GetName()
                    {
                        var e = new MyApp.Domain.Entity();
                        return e.Name;
                    }
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync(source);
    }

    [Fact]
    public async Task PropertyAttributeOnly_OtherPropertiesFree()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                public class Entity
                {
                    [AvailableTo("MyApp.Application")]
                    public string Restricted { get; set; } = "";

                    public string Open { get; set; } = "";
                }
            }

            namespace MyApp.UI
            {
                public class View
                {
                    public void Use()
                    {
                        var e = new MyApp.Domain.Entity();
                        var _ = {|#0:e.Restricted|};
                        var __ = e.Open;
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier.Diagnostic()
            .WithLocation(0)
            .WithArguments("Restricted", "MyApp.Application", "MyApp.UI.View.Use()");

        await AnalyzerVerifier.VerifyAsync(source, expected);
    }
}
