namespace ScopeGuard.Analyzer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

internal readonly record struct GatedSite(
    INamedTypeSymbol GatedType,
    string TargetDisplayName,
    Location Location);

internal static class Extractors
{
    public static void ExtractFromOperation(IOperation operation, Action<GatedSite> onSite)
    {
        var location = operation.Syntax.GetLocation();

        switch (operation)
        {
            case IInvocationOperation op:
                onSite(new GatedSite(op.TargetMethod.ContainingType, op.TargetMethod.Name, location));
                WalkTypeArgs(op.TargetMethod.TypeArguments, location, onSite);
                break;

            case IPropertyReferenceOperation op:
                onSite(new GatedSite(op.Property.ContainingType, op.Property.Name, location));
                break;

            case IFieldReferenceOperation op:
                onSite(new GatedSite(op.Field.ContainingType, op.Field.Name, location));
                break;

            case IObjectCreationOperation op when op.Type is INamedTypeSymbol created:
                onSite(new GatedSite(created, created.Name, location));
                WalkTypeArgs(created.TypeArguments, location, onSite);
                break;
        }
    }

    public static void ExtractFromNamedType(INamedTypeSymbol type, Action<GatedSite> onSite)
    {
        var location = type.Locations.Length > 0 ? type.Locations[0] : Location.None;

        if (type.BaseType is { } baseType)
        {
            onSite(new GatedSite(baseType, baseType.Name, location));
            WalkTypeArgs(baseType.TypeArguments, location, onSite);
        }

        foreach (var iface in type.Interfaces)
        {
            onSite(new GatedSite(iface, iface.Name, location));
            WalkTypeArgs(iface.TypeArguments, location, onSite);
        }
    }

    private static void WalkTypeArgs(
        ImmutableArray<ITypeSymbol> roots,
        Location location,
        Action<GatedSite> onSite)
    {
        if (roots.IsDefaultOrEmpty) return;

        // Iterative DFS — no yield state machine, no per-level allocation.
        var stack = new Stack<ITypeSymbol>(roots.Length);
        for (int i = roots.Length - 1; i >= 0; i--) stack.Push(roots[i]);

        while (stack.Count > 0)
        {
            if (stack.Pop() is not INamedTypeSymbol named) continue;
            onSite(new GatedSite(named, named.Name, location));

            var args = named.TypeArguments;
            for (int i = args.Length - 1; i >= 0; i--) stack.Push(args[i]);
        }
    }
}
