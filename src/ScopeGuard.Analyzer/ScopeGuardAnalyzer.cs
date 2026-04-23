namespace ScopeGuard.Analyzer;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ScopeGuardAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeFullName = "ScopeGuard.Attributes.VisibleToAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.SG001);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationCtx =>
        {
            var attributeType = compilationCtx.Compilation
                .GetTypeByMetadataName(AttributeFullName);

            // Bail silently if the attribute assembly is not referenced
            if (attributeType is null) return;

            var cache = new ConcurrentDictionary<ISymbol, ImmutableArray<string>?>(
                SymbolEqualityComparer.Default);

            compilationCtx.RegisterOperationAction(
                opCtx => AnalyzeOperation(opCtx, attributeType, cache),
                OperationKind.Invocation,
                OperationKind.PropertyReference,
                OperationKind.FieldReference,
                OperationKind.ObjectCreation);

            compilationCtx.RegisterSymbolAction(
                symCtx => AnalyzeNamedType(symCtx, attributeType, cache),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeOperation(
        OperationAnalysisContext context,
        INamedTypeSymbol attributeType,
        ConcurrentDictionary<ISymbol, ImmutableArray<string>?> cache)
    {
        ISymbol? targetSymbol = context.Operation switch
        {
            IInvocationOperation op => op.TargetMethod,
            IPropertyReferenceOperation op => op.Property,
            IFieldReferenceOperation op => op.Field,
            _ => null
        };

        if (context.Operation is IInvocationOperation invOp)
            ReportRestrictedTypeArguments(context, invOp.TargetMethod.TypeArguments, attributeType, cache);

        if (context.Operation is IObjectCreationOperation objOp && objOp.Type is INamedTypeSymbol createdType)
            ReportRestrictedTypeArguments(context, createdType.TypeArguments, attributeType, cache);

        if (targetSymbol is null) return;

        if (targetSymbol.ContainingType is not { } ct) return;
        var allowedPatterns = GetCachedPatterns(ct, attributeType, cache);

        if (allowedPatterns is null) return;

        var callerNamespace = GetFullNamespace(context.ContainingSymbol);

        if (!allowedPatterns.Value.Any(p => PatternMatcher.IsMatch(callerNamespace, p)))
        {
            var allowedList = string.Join(", ", allowedPatterns.Value);
            var callerDisplay = context.ContainingSymbol.ToDisplayString();
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.SG001,
                context.Operation.Syntax.GetLocation(),
                targetSymbol.Name,
                allowedList,
                callerDisplay));
        }
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        INamedTypeSymbol attributeType,
        ConcurrentDictionary<ISymbol, ImmutableArray<string>?> cache)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        var callerNamespace = GetFullNamespace(symbol);
        var callerDisplay = symbol.ToDisplayString();
        var location = symbol.Locations.FirstOrDefault();

        IEnumerable<INamedTypeSymbol> baseTypes = symbol.Interfaces;
        if (symbol.BaseType is not null)
            baseTypes = baseTypes.Prepend(symbol.BaseType);

        foreach (var baseType in baseTypes)
            foreach (var restricted in FindRestrictedTypeArguments(baseType.TypeArguments, attributeType, cache))
            {
                var patterns = GetCachedPatterns(restricted, attributeType, cache)!.Value;
                if (!patterns.Any(p => PatternMatcher.IsMatch(callerNamespace, p)))
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.SG001,
                        location,
                        restricted.Name,
                        string.Join(", ", patterns),
                        callerDisplay));
            }
    }

    private static void ReportRestrictedTypeArguments(
        OperationAnalysisContext context,
        ImmutableArray<ITypeSymbol> typeArgs,
        INamedTypeSymbol attributeType,
        ConcurrentDictionary<ISymbol, ImmutableArray<string>?> cache)
    {
        var callerNamespace = GetFullNamespace(context.ContainingSymbol);
        var callerDisplay = context.ContainingSymbol.ToDisplayString();
        var location = context.Operation.Syntax.GetLocation();

        foreach (var restricted in FindRestrictedTypeArguments(typeArgs, attributeType, cache))
        {
            var patterns = GetCachedPatterns(restricted, attributeType, cache)!.Value;
            if (!patterns.Any(p => PatternMatcher.IsMatch(callerNamespace, p)))
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.SG001,
                    location,
                    restricted.Name,
                    string.Join(", ", patterns),
                    callerDisplay));
        }
    }

    private static IEnumerable<INamedTypeSymbol> FindRestrictedTypeArguments(
        ImmutableArray<ITypeSymbol> typeArgs,
        INamedTypeSymbol attributeType,
        ConcurrentDictionary<ISymbol, ImmutableArray<string>?> cache)
    {
        foreach (var arg in typeArgs)
        {
            if (arg is not INamedTypeSymbol named) continue;
            if (GetCachedPatterns(named, attributeType, cache) is not null)
                yield return named;
            foreach (var nested in FindRestrictedTypeArguments(named.TypeArguments, attributeType, cache))
                yield return nested;
        }
    }

    private static ImmutableArray<string>? GetCachedPatterns(
        ISymbol symbol,
        INamedTypeSymbol attributeType,
        ConcurrentDictionary<ISymbol, ImmutableArray<string>?> cache)
    {
        return cache.GetOrAdd(symbol, s => ExtractAllowedPatterns(s, attributeType));
    }

    private static ImmutableArray<string>? ExtractAllowedPatterns(
        ISymbol symbol, INamedTypeSymbol attributeType)
    {
        var patterns = ImmutableArray.CreateBuilder<string>();

        foreach (var attr in symbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType))
                continue;

            if (attr.ConstructorArguments.Length == 0) continue;

            var arg = attr.ConstructorArguments[0];
            if (arg.Kind != TypedConstantKind.Array) continue;

            foreach (var element in arg.Values)
            {
                if (element.Value is string s && !string.IsNullOrWhiteSpace(s))
                    patterns.Add(s);
            }
        }

        return patterns.Count > 0 ? patterns.ToImmutable() : (ImmutableArray<string>?)null;
    }

    private static string GetFullNamespace(ISymbol symbol)
    {
        var parts = new List<string>();
        var ns = symbol.ContainingNamespace;

        while (ns is { IsGlobalNamespace: false })
        {
            parts.Add(ns.Name);
            ns = ns.ContainingNamespace;
        }

        parts.Reverse();
        return string.Join(".", parts);
    }
}
