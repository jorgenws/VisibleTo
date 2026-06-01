using System;

namespace VisibleTo.Attributes;

/// <summary>
/// Restricts access to the decorated type or member to the specified namespace patterns.
/// Any caller whose full namespace does not match at least one pattern will receive a
/// compile-time error (VT001) from the VisibleTo analyzer.
/// </summary>
/// <remarks>
/// Supported pattern syntax:
/// <list type="bullet">
///   <item><description><c>*</c> — matches any characters within a single namespace segment.</description></item>
///   <item><description><c>**</c> — matches any characters across namespace boundaries.</description></item>
///   <item><description>Exact strings — match the full namespace literally.</description></item>
/// </list>
/// Multiple applications of this attribute are combined with OR logic.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum,
    AllowMultiple = true,
    Inherited = false)]
public sealed class VisibleToAttribute : Attribute
{
    public string[] AllowedPatterns { get; }

    public VisibleToAttribute(params string[] allowedPatterns)
        => AllowedPatterns = allowedPatterns;
}
