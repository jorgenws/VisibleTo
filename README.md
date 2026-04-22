# ScopeGuard

A Roslyn-based analyzer that enforces Clean Architecture boundaries at compile time.

## The Problem

Clean Architecture separates your code into layers — Domain, Application, Infrastructure, UI — where inner layers know nothing about outer ones. In practice this breaks down in one specific place: your ORM.

EF Core (and similar tools) require entity classes to be `public`. The moment they are, nothing stops a developer from doing this:

```csharp
// In a UI controller — this compiles fine, but violates your architecture
var user = new UserRepository().GetById(id);
Console.WriteLine(user.PasswordHash); // Domain internals, exposed to the UI
```

The type is `public` because it has to be. But it was never meant to be used here.

## The Solution

ScopeGuard introduces a `[AvailableTo]` attribute that lets you declare who is allowed to use a type or member. Any violation becomes a **compile-time error** — not a code review comment, not a runtime exception, a build failure.

```csharp
using ScopeGuard.Attributes;

namespace MyApp.Domain
{
    [AvailableTo("MyApp.Application.**")]
    public class User
    {
        public string PasswordHash { get; set; }
    }
}
```

Now this:

```csharp
// In MyApp.UI.Controllers
var hash = user.PasswordHash; // error SG001: Access denied by ScopeGuard
```

Fails to build with:

```
error SG001: Member 'PasswordHash' is available to 'MyApp.Application.**',
but is being accessed by 'MyApp.UI.Controllers.UserController.Index()'.
Access denied by ScopeGuard.
```

## How It Works

ScopeGuard is a Roslyn analyzer — it runs inside the compiler during every build. There is no runtime overhead and no separate tool to run.

When it sees a member access, property read/write, or method call, it checks whether the caller's namespace matches any of the patterns declared on the target. If not, it emits a compiler error.

## Pattern Syntax

| Pattern | Matches |
|---------|---------|
| `MyApp.Application` | Exactly that namespace |
| `MyApp.Application.*` | Any single segment under `Application` (e.g. `MyApp.Application.Handlers`, but not `MyApp.Application.Sub.Handlers`) |
| `MyApp.Application.**` | Any namespace rooted at `Application`, at any depth |

Multiple patterns are combined with OR logic — access is granted if the caller matches **any** of them.

```csharp
[AvailableTo("MyApp.Application.**", "MyApp.Tests.**")]
public class Order { ... }
```

## Examples

### Protect an entire class

```csharp
namespace MyApp.Domain
{
    [AvailableTo("MyApp.Application.**")]
    public class Invoice
    {
        public decimal Total { get; set; }
        public void Approve() { }
    }
}
```

All members of `Invoice` are restricted. Only code in `MyApp.Application` or its sub-namespaces may call `Approve()` or read `Total`.

### Protect a single member

```csharp
namespace MyApp.Domain
{
    public class Product
    {
        public string Name { get; set; }

        [AvailableTo("MyApp.Application.Pricing")]
        public decimal CostPrice { get; set; }
    }
}
```

`Name` is freely accessible. `CostPrice` is only accessible from `MyApp.Application.Pricing`.

### Allow multiple layers

```csharp
[AvailableTo("MyApp.Application.**", "MyApp.Tests.**")]
public class OrderLine { ... }
```

Both application code and tests can access `OrderLine`. Everything else cannot.

### Using `nameof` for refactor safety

String literals in attributes are a refactoring hazard — rename a namespace and the attribute silently refers to a name that no longer exists. Use `nameof` to keep patterns tied to real symbols:

```csharp
[AvailableTo(nameof(MyApp.Application))]
public class Entity { ... }
```

> **Note:** `nameof` evaluates to the last identifier only — `nameof(MyApp.Application)` produces `"Application"`, not `"MyApp.Application"`. Use it for single-segment patterns or combine with string concatenation for full paths.

## Installation

ScopeGuard consists of two packages:

- **ScopeGuard.Attributes** — reference this from your Domain project. Contains only the `[AvailableTo]` attribute; zero dependencies.
- **ScopeGuard.Analyzer** — reference this from any project that should be analyzed (typically all projects in the solution).

```xml
<!-- Domain project -->
<PackageReference Include="ScopeGuard.Attributes" Version="1.0.0" />

<!-- All other projects -->
<PackageReference Include="ScopeGuard.Analyzer" Version="1.0.0" />
```

## Diagnostic Reference

| ID | Severity | Message |
|----|----------|---------|
| SG001 | Error | Member `'{0}'` is available to `'{1}'`, but is being accessed by `'{2}'`. Access denied by ScopeGuard. |
