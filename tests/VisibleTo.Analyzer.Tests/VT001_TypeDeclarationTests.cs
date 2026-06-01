namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class SG001_TypeDeclarationTests
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
    public async Task InterfaceGenericTypeArgument_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            using System.Collections.Generic;

            namespace MyApp.UI
            {
                public class Comparer : IComparer<MyApp.Domain.Entity>
                {
                    public int Compare(MyApp.Domain.Entity? x, MyApp.Domain.Entity? y) => 0;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "SG001" && d.GetMessage().Contains("Entity"));
    }

    [Fact]
    public async Task InterfaceGenericTypeArgument_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            using System.Collections.Generic;

            namespace MyApp.Application
            {
                public class Comparer : IComparer<MyApp.Domain.Entity>
                {
                    public int Compare(MyApp.Domain.Entity? x, MyApp.Domain.Entity? y) => 0;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task InterfaceGenericTypeArgument_NoAttribute_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            namespace MyApp.Domain
            {
                public class Entity { }
            }

            namespace MyApp.UI
            {
                public class Comparer : IComparer<MyApp.Domain.Entity>
                {
                    public int Compare(MyApp.Domain.Entity? x, MyApp.Domain.Entity? y) => 0;
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BaseClassGenericTypeArgument_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.Domain
            {
                public class EntityBase<T> { }
            }

            namespace MyApp.UI
            {
                public class Derived : MyApp.Domain.EntityBase<MyApp.Domain.Entity> { }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task BaseClassGenericTypeArgument_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Domain
            {
                public class EntityBase<T> { }
            }

            namespace MyApp.Application
            {
                public class Derived : MyApp.Domain.EntityBase<MyApp.Domain.Entity> { }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NestedGenericTypeArgument_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.Domain
            {
                public class Command<T> { }
            }

            namespace MyApp.UI
            {
                public interface IHandler<T> { }

                public class Handler : IHandler<MyApp.Domain.Command<MyApp.Domain.Entity>> { }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task NestedGenericTypeArgument_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Domain
            {
                public class Command<T> { }
            }

            namespace MyApp.Application
            {
                public interface IHandler<T> { }

                public class Handler : IHandler<MyApp.Domain.Command<MyApp.Domain.Entity>> { }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DirectBaseClass_Denied_RaisesSG001()
    {
        var source = EntityDef + """
            namespace MyApp.UI
            {
                public class Sub : MyApp.Domain.Entity { }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task DirectBaseClass_Allowed_NoDiagnostic()
    {
        var source = EntityDef + """
            namespace MyApp.Application
            {
                public class Sub : MyApp.Domain.Entity { }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }
}
