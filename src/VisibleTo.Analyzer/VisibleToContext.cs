namespace ScopeGuard.Analyzer;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

internal sealed class VisibleToContext
{
    private readonly INamedTypeSymbol _attributeType;
    private readonly ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<string>> _patternCache;
    private readonly ConcurrentDictionary<ISymbol, string> _namespaceCache;
    private readonly Func<INamedTypeSymbol, ImmutableArray<string>> _computePatterns;
    private readonly Func<ISymbol, string> _computeNamespace;

    public VisibleToContext(INamedTypeSymbol attributeType)
    {
        _attributeType = attributeType;
        _patternCache = new(SymbolEqualityComparer.Default);
        _namespaceCache = new(SymbolEqualityComparer.Default);
        _computePatterns = ExtractPatterns;
        _computeNamespace = ComputeNamespace;
    }

    /// <summary>
    /// Returns the allowed-namespace patterns declared on <paramref name="type"/>.
    /// Returns <c>default</c> when the type carries no [VisibleTo] — check
    /// <c>.IsDefault</c> on the result before using it.
    /// </summary>
    public ImmutableArray<string> GetPatterns(INamedTypeSymbol type)
        => _patternCache.GetOrAdd(type, _computePatterns);

    public string GetNamespace(ISymbol symbol)
        => _namespaceCache.GetOrAdd(symbol, _computeNamespace);

    private ImmutableArray<string> ExtractPatterns(INamedTypeSymbol type)
    {
        ImmutableArray<string>.Builder? builder = null;

        foreach (var attr in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _attributeType))
                continue;
            if (attr.ConstructorArguments.Length == 0) continue;

            var arg = attr.ConstructorArguments[0];
            if (arg.Kind != TypedConstantKind.Array) continue;

            foreach (var element in arg.Values)
            {
                if (element.Value is string s && !string.IsNullOrWhiteSpace(s))
                    (builder ??= ImmutableArray.CreateBuilder<string>()).Add(s);
            }
        }

        return builder is null ? default : builder.ToImmutable();
    }

    private static string ComputeNamespace(ISymbol symbol)
    {
        var ns = symbol.ContainingNamespace;
        if (ns is null || ns.IsGlobalNamespace) return string.Empty;

        var s0 = ns.Name; ns = ns.ContainingNamespace;
        if (ns is null || ns.IsGlobalNamespace) return s0;
        var s1 = ns.Name; ns = ns.ContainingNamespace;
        if (ns is null || ns.IsGlobalNamespace) return $"{s1}.{s0}";
        var s2 = ns.Name; ns = ns.ContainingNamespace;
        if (ns is null || ns.IsGlobalNamespace) return $"{s2}.{s1}.{s0}";

        var parts = new List<string> { s2, s1, s0 };
        while (ns is { IsGlobalNamespace: false }) { parts.Add(ns.Name); ns = ns.ContainingNamespace; }
        parts.Reverse();
        return string.Join(".", parts);
    }
}
