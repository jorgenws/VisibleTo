namespace VisibleTo.Analyzer;

using Microsoft.CodeAnalysis;

internal static class Descriptors
{
    private const string Category = "Architecture";

    public static readonly DiagnosticDescriptor VT001 = new(
        id: "VT001",
        title: "Unauthorized Member Access",
        messageFormat: "Member '{0}' is available to '{1}', but is being accessed by '{2}'. Access denied by VisibleTo.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "This member is protected by VisibleTo. Only explicitly allowed namespaces may access it.");
}
