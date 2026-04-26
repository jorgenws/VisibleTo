namespace ScopeGuard.Analyzer;

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
    public static IEnumerable<GatedSite> ExtractFromOperation(IOperation operation)
    {
        var location = operation.Syntax.GetLocation();

        switch (operation)
        {
            case IInvocationOperation op:
                yield return new GatedSite(op.TargetMethod.ContainingType, op.TargetMethod.Name, location);
                foreach (var site in WalkTypeArgs(op.TargetMethod.TypeArguments, location))
                    yield return site;
                break;

            case IPropertyReferenceOperation op:
                yield return new GatedSite(op.Property.ContainingType, op.Property.Name, location);
                break;

            case IFieldReferenceOperation op:
                yield return new GatedSite(op.Field.ContainingType, op.Field.Name, location);
                break;

            case IObjectCreationOperation op when op.Type is INamedTypeSymbol created:
                yield return new GatedSite(created, created.Name, location);
                foreach (var site in WalkTypeArgs(created.TypeArguments, location))
                    yield return site;
                break;
        }
    }

    public static IEnumerable<GatedSite> ExtractFromNamedType(INamedTypeSymbol type)
    {
        var location = type.Locations.Length > 0 ? type.Locations[0] : Location.None;

        if (type.BaseType is { } baseType)
        {
            yield return new GatedSite(baseType, baseType.Name, location);
            foreach (var site in WalkTypeArgs(baseType.TypeArguments, location))
                yield return site;
        }

        foreach (var iface in type.Interfaces)
        {
            yield return new GatedSite(iface, iface.Name, location);
            foreach (var site in WalkTypeArgs(iface.TypeArguments, location))
                yield return site;
        }

        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared) continue;
            switch (member)
            {
                case IMethodSymbol { AssociatedSymbol: null } method:
                    foreach (var site in WalkTypeRef(method.ReturnType, location))
                        yield return site;
                    foreach (var p in method.Parameters)
                        foreach (var site in WalkTypeRef(p.Type, location))
                            yield return site;
                    break;
                case IPropertySymbol prop:
                    foreach (var site in WalkTypeRef(prop.Type, location))
                        yield return site;
                    break;
                case IFieldSymbol field:
                    foreach (var site in WalkTypeRef(field.Type, location))
                        yield return site;
                    break;
                case IEventSymbol evt:
                    foreach (var site in WalkTypeRef(evt.Type, location))
                        yield return site;
                    break;
            }
        }

        if (type.TypeKind == TypeKind.Delegate && type.DelegateInvokeMethod is { } invoke)
        {
            foreach (var site in WalkTypeRef(invoke.ReturnType, location))
                yield return site;
            foreach (var p in invoke.Parameters)
                foreach (var site in WalkTypeRef(p.Type, location))
                    yield return site;
        }
    }

    private static IEnumerable<GatedSite> WalkTypeRef(ITypeSymbol root, Location location)
    {
        while (root is IArrayTypeSymbol arr) root = arr.ElementType;
        if (root is not INamedTypeSymbol named) yield break;
        yield return new GatedSite(named, named.Name, location);
        foreach (var site in WalkTypeArgs(named.TypeArguments, location))
            yield return site;
    }

    private static IEnumerable<GatedSite> WalkTypeArgs(ImmutableArray<ITypeSymbol> roots, Location location)
    {
        if (roots.IsDefaultOrEmpty) yield break;

        var stack = new Stack<ITypeSymbol>(roots.Length);
        for (int i = roots.Length - 1; i >= 0; i--) stack.Push(roots[i]);

        while (stack.Count > 0)
        {
            if (stack.Pop() is not INamedTypeSymbol named) continue;
            yield return new GatedSite(named, named.Name, location);

            var args = named.TypeArguments;
            for (int i = args.Length - 1; i >= 0; i--) stack.Push(args[i]);
        }
    }
}
