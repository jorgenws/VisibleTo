# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test by name
dotnet test --filter "FullyQualifiedName~GenericTypeArgument_Denied"

# Pack the NuGet package (outputs to ./nupkgs)
dotnet pack src/VisibleTo.Analyzer/VisibleTo.Analyzer.csproj --output ./nupkgs
```

## Architecture

Three projects:

- **`src/VisibleTo.Attributes`** (`netstandard2.0`) — defines `[VisibleTo]`. Not published independently; it is bundled into the NuGet package as a `lib` assembly so consumers get the attribute automatically.
- **`src/VisibleTo.Analyzer`** (`netstandard2.0`) — the Roslyn `DiagnosticAnalyzer`. This is the sole published NuGet package (`PackageId: VisibleTo.Analyzer`). Emits **VT001** errors.
- **`tests/VisibleTo.Analyzer.Tests`** (`net10.0`) — xUnit tests that compile inline C# source strings via `CSharpCompilation` and assert on the diagnostics produced.

### How the analyzer works

`VisibleToAnalyzer` runs two analysis passes per compilation:

1. **Operation-level** (`RegisterOperationAction`): triggered on `Invocation`, `PropertyReference`, `FieldReference`, and `ObjectCreation`. Checks whether the *containing type* of the accessed member carries `[VisibleTo]`, and also walks generic type arguments (including nested ones) to catch e.g. `repo.Get<Entity>()` or `new List<Entity>()`.

2. **Symbol-level** (`RegisterSymbolAction` on `NamedType`): triggered when a class or struct is declared. Walks:
   - Base type and interfaces (including their generic type arguments)
   - All declared members: method return types and parameters, property types, field types, event types
   - Delegate invoke signatures (via `DelegateInvokeMethod`, since delegate members are `IsImplicitlyDeclared`)

   This catches `class Repo : IRepository<Entity>`, `User GetUser()`, `void Handle(User u)`, `public User CurrentUser { get; }`, etc.

Both passes use a `ConcurrentDictionary` cache keyed on `ISymbol` to avoid re-reading attributes for the same type repeatedly across a compilation.

A self-reference guard in `Enforce` ensures a type never triggers VT001 for referencing itself — `SymbolEqualityComparer.Default.Equals(callerType, site.GatedType)` is checked before pattern matching.

Pattern matching (`PatternMatcher.cs`) converts each pattern string into a compiled `Regex`:
- `**` → `.*` (matches across any number of namespace segments, including zero — `Foo.**` matches `Foo` itself)
- `*` → `[^.]+` (matches within a single segment)
- Literal strings are `Regex.Escape`d

### Key constraints

- `[VisibleTo]` can only be applied to **classes and structs** (`AttributeTargets.Class | AttributeTargets.Struct`). Methods and properties are not supported.
- `AllowMultiple = true` — multiple `[VisibleTo]` attributes on one type are combined with OR logic.
- `Inherited = false` — the attribute is not inherited by subclasses.

### Test approach

`AnalyzerVerifier.GetDiagnosticsAsync(source)` compiles a raw C# string in memory (referencing the real `VisibleTo.Attributes` assembly) and returns all analyzer diagnostics. Tests assert on diagnostic ID (`VT001`) and message content. There are no mocks.

### NuGet package structure

The package is **not** marked `DevelopmentDependency`, so it flows transitively in the NuGet graph. Any project that (directly or indirectly) depends on a project referencing VisibleTo.Analyzer will have the analyzer applied automatically.

`build/VisibleTo.props` adds `VisibleTo.Attributes` as an explicit `<Reference>` via HintPath for direct consumers. Transitive consumers get `VisibleTo.Attributes.dll` through NuGet's normal `lib/netstandard2.0` compile asset flow.

### Publishing

The NuGet package is published automatically by the GitHub Action in `.github/workflows/publish.yml` when a `v*` tag is pushed. The pack step bundles both `VisibleTo.Analyzer.dll` (under `analyzers/dotnet/cs`) and `VisibleTo.Attributes.dll` (under `lib/netstandard2.0`) into a single package.
