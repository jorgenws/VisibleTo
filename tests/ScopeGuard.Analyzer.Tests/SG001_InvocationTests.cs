namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class SG001_InvocationTests
{
    [Fact]
    public async Task AllowedCaller_NoDiagnostic()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application.**")]
                public class DomainService
                {
                    public void Execute() { }
                }
            }

            namespace MyApp.Application.UseCases
            {
                public class UseCase
                {
                    public void Run()
                    {
                        var svc = new MyApp.Domain.DomainService();
                        svc.Execute();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DeniedCaller_RaisesSG001()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application.**")]
                public class DomainService
                {
                    public void Execute() { }
                }
            }

            namespace MyApp.UI
            {
                public class Controller
                {
                    public void Action()
                    {
                        var svc = new MyApp.Domain.DomainService();
                        svc.Execute();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Execute", sg001.GetMessage());
        Assert.Contains("MyApp.Application.**", sg001.GetMessage());
        Assert.Contains("MyApp.UI.Controller.Action()", sg001.GetMessage());
    }

    [Fact]
    public async Task AttributeOnContainingType_ProtectsAllMembers()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application")]
                public class Entity
                {
                    public void Save() { }
                }
            }

            namespace MyApp.UI
            {
                public class View
                {
                    public void Render()
                    {
                        var e = new MyApp.Domain.Entity();
                        e.Save();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Single(diagnostics, d => d.Id == "SG001");
    }

    [Fact]
    public async Task MultiplePatterns_OneMatches_NoDiagnostic()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application", "MyApp.Tests.**")]
                public class DomainService
                {
                    public void Execute() { }
                }
            }

            namespace MyApp.Tests.Integration
            {
                public class IntegrationTest
                {
                    public void Test()
                    {
                        var svc = new MyApp.Domain.DomainService();
                        svc.Execute();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoAttribute_NoDiagnostic()
    {
        var source = """
            namespace MyApp.Domain
            {
                public class DomainService
                {
                    public void Execute() { }
                }
            }

            namespace MyApp.UI
            {
                public class Controller
                {
                    public void Action()
                    {
                        var svc = new MyApp.Domain.DomainService();
                        svc.Execute();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MethodLevelAttribute_OnlyThatMethodRestricted()
    {
        var source = """
            using ScopeGuard.Attributes;

            namespace MyApp.Domain
            {
                public class DomainService
                {
                    [AvailableTo("MyApp.Application")]
                    public void RestrictedMethod() { }

                    public void OpenMethod() { }
                }
            }

            namespace MyApp.UI
            {
                public class Controller
                {
                    public void Action()
                    {
                        var svc = new MyApp.Domain.DomainService();
                        svc.RestrictedMethod();
                        svc.OpenMethod();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("RestrictedMethod", sg001.GetMessage());
    }
}
