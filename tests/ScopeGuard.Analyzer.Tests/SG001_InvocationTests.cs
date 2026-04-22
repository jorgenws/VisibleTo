namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
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

        await AnalyzerVerifier.VerifyAsync(source);
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
                        {|#0:svc.Execute()|};
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier.Diagnostic()
            .WithLocation(0)
            .WithArguments("Execute", "MyApp.Application.**", "MyApp.UI.Controller.Action()");

        await AnalyzerVerifier.VerifyAsync(source, expected);
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
                        {|#0:e.Save()|};
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier.Diagnostic()
            .WithLocation(0)
            .WithArguments("Save", "MyApp.Application", "MyApp.UI.View.Render()");

        await AnalyzerVerifier.VerifyAsync(source, expected);
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

        await AnalyzerVerifier.VerifyAsync(source);
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

        await AnalyzerVerifier.VerifyAsync(source);
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
                        {|#0:svc.RestrictedMethod()|};
                        svc.OpenMethod();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier.Diagnostic()
            .WithLocation(0)
            .WithArguments("RestrictedMethod", "MyApp.Application", "MyApp.UI.Controller.Action()");

        await AnalyzerVerifier.VerifyAsync(source, expected);
    }
}
