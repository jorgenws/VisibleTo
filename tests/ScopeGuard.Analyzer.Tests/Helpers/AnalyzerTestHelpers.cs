namespace ScopeGuard.Analyzer.Tests.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;

internal static class AnalyzerVerifier
{
    // Included in every test compilation so that [AvailableTo] is resolvable.
    internal const string AttributeSource = """
        namespace ScopeGuard.Attributes;

        [System.AttributeUsage(
            System.AttributeTargets.Class | System.AttributeTargets.Struct |
            System.AttributeTargets.Method | System.AttributeTargets.Property,
            AllowMultiple = true, Inherited = false)]
        public sealed class AvailableToAttribute : System.Attribute
        {
            public string[] AllowedPatterns { get; }
            public AvailableToAttribute(params string[] allowedPatterns)
                => AllowedPatterns = allowedPatterns;
        }
        """;

    public static DiagnosticResult Diagnostic()
        => CSharpAnalyzerVerifier<ScopeGuardAnalyzer, DefaultVerifier>.Diagnostic(Descriptors.SG001);

    public static async Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ScopeGuardAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source, AttributeSource }
            }
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
