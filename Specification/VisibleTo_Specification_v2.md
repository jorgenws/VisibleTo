# Specification: ScopeGuard (v2)

**ScopeGuard** is a specialized Roslyn-based architectural enforcement tool for C#. It allows developers to define "Virtual Access Modifiers" to maintain strict Clean Architecture boundaries, specifically solving the conflict where Domain entities must be `public` for infrastructure tools (like EF Core) but should remain "invisible" to other layers (like the UI).

---

## 1. Project Architecture
The solution is divided into two distinct components to ensure the Domain layer remains decoupled from the Infrastructure layer.

* **ScopeGuard.Attributes**
    * **Target:** .NET 10 / C# 14+
    * **Role:** Contains the attribute definitions. This is the only library referenced by the Domain project.
* **ScopeGuard.Analyzer**
    * **Target:** `.netstandard2.0` (Requirement for Roslyn compatibility).
    * **Role:** The engine that performs semantic analysis and enforces compile-time errors.

---

## 2. The Attribute: `[AvailableTo]`
The primary tool for developers. It defines which classes or namespaces are permitted to access a specific type or member.

* **Targets:** `Class`, `Property`, `Method`, `Struct`.
* **Parameters:** `params string[] allowedPatterns`.
* **Functionality:** Supports strict class names, `nameof()` (evaluated via the Symbolic model), and Wildcards.

---

## 3. The Enforcement Engine (Analyzer)
The analyzer converts "legal" C# code into a **Build Error** if it violates the `AvailableTo` contract.

### Detection Mechanism: Usage Check
Unlike standard analyzers that only look at definitions, ScopeGuard performs a **Usage Check** by patrolling the codebase for references to restricted types.

1.  **Operation Detection:** Instead of raw syntax, the analyzer registers for `OperationKind.Invocation`, `PropertyReference`, and `FieldReference`. This ensures that `var` usage, method calls, and property access are all caught regardless of coding style.
2.  **Semantic Mapping:** Uses the `SemanticModel` to resolve usage into a `Symbol`.
3.  **Symbolic Extraction:** Checks the Symbol (or its parent type) for the `AvailableToAttribute`.
4.  **Constant Evaluation:** Extracts string patterns from the Symbolic model’s `ConstructorArguments`, resolving `nameof()` expressions into final strings.
5.  **Context Comparison:** Identifies the "Caller" using `context.ContainingSymbol` and compares its Full Name against the allowed patterns.

### Optimization: Compilation-Level Caching
To maintain high performance in large solutions, the analyzer uses a thread-safe caching strategy:
* **`CompilationStartAnalysisContext`**: Used to initialize a `ConcurrentDictionary` that lasts for the duration of the build.
* **Symbol Caching**: Maps `ITypeSymbol` to its list of allowed wildcards. This ensures the "attribute-reading tax" is only paid once per type per build.

### Pattern Matching (Wildcards to Regex)
* **`*` (Single Star):** Matches characters within a namespace segment (e.g., `*Repository`).
* **`**` (Double Star):** Matches across namespace boundaries (e.g., `Infrastructure.**`).

---

## 4. Technical Requirements & Constraints
* **Zero Runtime Overhead:** Enforcement happens entirely at compile-time.
* **EF Core Compatibility:** Can be configured to ignore `Microsoft.EntityFrameworkCore` to allow automated mapping while blocking manual access.
* **Fail-Fast Refactoring:** Broken string references result in immediate **Compile Errors**.
* **Language:** C# / .NET 10.

---

## 5. Diagnostics & UX
* **Diagnostic ID:** `SG001` (Unauthorized Access).
* **Error Message:** *"Member '{0}' is available to '{1}', but is being accessed by '{2}'. Access denied by ScopeGuard."*
* **Severity:** Error.
