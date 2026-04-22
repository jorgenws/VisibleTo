namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_EfCoreExemptionTests
{
    [Fact]
    public async Task EfCoreCaller_AlwaysAllowed()
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

            namespace Microsoft.EntityFrameworkCore.Infrastructure
            {
                public class EfCoreMapping
                {
                    public void Configure()
                    {
                        var e = new MyApp.Domain.Entity();
                        e.Name = "mapped";
                    }
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync(source);
    }

    [Fact]
    public async Task EfCoreCaller_ExactNamespace_AllowedWithoutDot()
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

            namespace Microsoft.EntityFrameworkCore
            {
                public class DirectEfCaller
                {
                    public void Use()
                    {
                        var e = new MyApp.Domain.Entity();
                        e.Save();
                    }
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync(source);
    }

    [Fact]
    public async Task NearMatchNamespace_NotExempt_RaisesSG001()
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

            namespace Microsoft.EntityFrameworkCoreExtensions
            {
                public class ExtensionCaller
                {
                    public void Use()
                    {
                        var e = new MyApp.Domain.Entity();
                        {|#0:e.Save()|};
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier.Diagnostic()
            .WithLocation(0)
            .WithArguments("Save", "MyApp.Application",
                "Microsoft.EntityFrameworkCoreExtensions.ExtensionCaller.Use()");

        await AnalyzerVerifier.VerifyAsync(source, expected);
    }
}
