namespace ScopeGuard.Analyzer;

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ScopeGuardAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeFullName = "ScopeGuard.Attributes.VisibleToAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Descriptors.SG001];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationCtx =>
        {
            var attributeType = compilationCtx.Compilation
                .GetTypeByMetadataName(AttributeFullName);

            if (attributeType is null) return;

            var visibleToCtx = new VisibleToContext(attributeType);

            compilationCtx.RegisterOperationAction(
                opCtx => AnalyzeOperation(opCtx, visibleToCtx),
                OperationKind.Invocation,
                OperationKind.PropertyReference,
                OperationKind.FieldReference,
                OperationKind.ObjectCreation);

            compilationCtx.RegisterSymbolAction(
                symCtx => AnalyzeNamedType(symCtx, visibleToCtx),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeOperation(
        OperationAnalysisContext context,
        VisibleToContext visibleToCtx)
    {
        foreach (var site in Extractors.ExtractFromOperation(context.Operation))
            Enforce(site, context.ContainingSymbol, visibleToCtx, context.ReportDiagnostic);
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        VisibleToContext visibleToCtx)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        foreach (var site in Extractors.ExtractFromNamedType(type))
            Enforce(site, type, visibleToCtx, context.ReportDiagnostic);
    }

    private static void Enforce(
        GatedSite site,
        ISymbol caller,
        VisibleToContext visibleToCtx,
        Action<Diagnostic> report)
    {
        var patterns = visibleToCtx.GetPatterns(site.GatedType);
        if (patterns.IsDefault) return;

        var callerNs = visibleToCtx.GetNamespace(caller);

        for (int i = 0; i < patterns.Length; i++)
        {
            if (PatternMatcher.IsMatch(callerNs, patterns[i])) return;
        }

        report(Diagnostic.Create(
            Descriptors.SG001,
            site.Location,
            site.TargetDisplayName,
            string.Join(", ", patterns),
            caller.ToDisplayString()));
    }
}
