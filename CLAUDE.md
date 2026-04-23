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
dotnet pack src/ScopeGuard.Analyzer/ScopeGuard.Analyzer.csproj --output ./nupkgs
```

## Architecture

Three projects:

- **`src/ScopeGuard.Attributes`** (`netstandard2.0`) — defines `[VisibleTo]`. Not published independently; it is bundled into the NuGet package as a `lib` assembly so consumers get the attribute automatically.
- **`src/ScopeGuard.Analyzer`** (`netstandard2.0`) — the Roslyn `DiagnosticAnalyzer`. This is the sole published NuGet package (`PackageId: ScopeGuard`). Emits **SG001** errors.
- **`tests/ScopeGuard.Analyzer.Tests`** (`net10.0`) — xUnit tests that compile inline C# source strings via `CSharpCompilation` and assert on the diagnostics produced.

### How the analyzer works

`ScopeGuardAnalyzer` runs two analysis passes per compilation:

1. **Operation-level** (`RegisterOperationAction`): triggered on `Invocation`, `PropertyReference`, `FieldReference`, and `ObjectCreation`. Checks whether the *containing type* of the accessed member carries `[VisibleTo]`, and also walks generic type arguments (including nested ones) to catch e.g. `repo.Get<Entity>()` or `new List<Entity>()`.

2. **Symbol-level** (`RegisterSymbolAction` on `NamedType`): triggered when a class or struct is declared. Walks its base type and interfaces, then checks their generic type arguments for `[VisibleTo]`-protected types. This catches declarations like `class Repo : IRepository<Entity>`.

Both passes use a `ConcurrentDictionary` cache keyed on `ISymbol` to avoid re-reading attributes for the same type repeatedly across a compilation.

Pattern matching (`PatternMatcher.cs`) converts each pattern string into a compiled `Regex`:
- `**` → `.*` (matches across any number of namespace segments)
- `*` → `[^.]+` (matches within a single segment)
- Literal strings are `Regex.Escape`d

### Key constraints

- `[VisibleTo]` can only be applied to **classes and structs** (`AttributeTargets.Class | AttributeTargets.Struct`). Methods and properties are not supported.
- `AllowMultiple = true` — multiple `[VisibleTo]` attributes on one type are combined with OR logic.
- `Inherited = false` — the attribute is not inherited by subclasses.

### Test approach

`AnalyzerVerifier.GetDiagnosticsAsync(source)` compiles a raw C# string in memory (referencing the real `ScopeGuard.Attributes` assembly) and returns all analyzer diagnostics. Tests assert on diagnostic ID (`SG001`) and message content. There are no mocks.

### Publishing

The NuGet package is published automatically by the GitHub Action in `.github/workflows/publish.yml` when a `v*` tag is pushed. The pack step bundles both `ScopeGuard.Analyzer.dll` (under `analyzers/dotnet/cs`) and `ScopeGuard.Attributes.dll` (under `lib/netstandard2.0`) into a single package.
