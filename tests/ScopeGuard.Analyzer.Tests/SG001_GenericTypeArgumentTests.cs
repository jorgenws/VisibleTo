namespace ScopeGuard.Analyzer.Tests;

using ScopeGuard.Analyzer.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

public class SG001_GenericTypeArgumentTests
{
    private static string Source(string targetNs, string callerNs, string pattern, string callExpression) => $$"""
        using ScopeGuard.Attributes;
        using System.Collections.Generic;

        namespace {{targetNs}}
        {
            [AvailableTo("{{pattern}}")]
            public class Entity { }

            public class Repository
            {
                public T Get<T>() => default!;
            }
        }

        namespace {{callerNs}}
        {
            public class Caller
            {
                public void Call()
                {
                    var repo = new {{targetNs}}.Repository();
                    {{callExpression}}
                }
            }
        }
        """;

    [Fact]
    public async Task GenericTypeArgument_Denied_RaisesSG001()
    {
        var source = Source("MyApp.Domain", "MyApp.UI", "MyApp.Application", "repo.Get<MyApp.Domain.Entity>();");
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task GenericTypeArgument_Allowed_NoDiagnostic()
    {
        var source = Source("MyApp.Domain", "MyApp.Application", "MyApp.Application", "repo.Get<MyApp.Domain.Entity>();");
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GenericTypeArgument_NoAttribute_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            namespace MyApp.Domain
            {
                public class Entity { }

                public class Repository
                {
                    public T Get<T>() => default!;
                }
            }

            namespace MyApp.UI
            {
                public class Caller
                {
                    public void Call()
                    {
                        var repo = new MyApp.Domain.Repository();
                        repo.Get<MyApp.Domain.Entity>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NestedGenericTypeArgument_Denied_RaisesSG001()
    {
        var source = """
            using ScopeGuard.Attributes;
            using System.Collections.Generic;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application")]
                public class Entity { }

                public class Repository
                {
                    public T Get<T>() => default!;
                }
            }

            namespace MyApp.UI
            {
                public class Caller
                {
                    public void Call()
                    {
                        var repo = new MyApp.Domain.Repository();
                        repo.Get<List<MyApp.Domain.Entity>>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        var sg001 = Assert.Single(diagnostics, d => d.Id == "SG001");
        Assert.Contains("Entity", sg001.GetMessage());
    }

    [Fact]
    public async Task NestedGenericTypeArgument_Allowed_NoDiagnostic()
    {
        var source = """
            using ScopeGuard.Attributes;
            using System.Collections.Generic;

            namespace MyApp.Domain
            {
                [AvailableTo("MyApp.Application")]
                public class Entity { }

                public class Repository
                {
                    public T Get<T>() => default!;
                }
            }

            namespace MyApp.Application
            {
                public class Caller
                {
                    public void Call()
                    {
                        var repo = new MyApp.Domain.Repository();
                        repo.Get<List<MyApp.Domain.Entity>>();
                    }
                }
            }
            """;
        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }
}
