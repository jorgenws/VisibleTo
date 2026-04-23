# ScopeGuard

A Roslyn-based analyzer that enforces architectural boundaries at compile time.

## The Problem

Many architectural patterns divide code into layers where access between them should be controlled. In practice this breaks down at one specific place: types that must be `public` for technical reasons, but were never meant to be used everywhere.

A common example is EF Core — entity classes must be `public` for the ORM to work, but that makes them accessible from any layer in the solution:

```csharp
// In a UI controller — this compiles fine, but violates your architecture
var user = new UserRepository().GetById(id);
Console.WriteLine(user.PasswordHash); // Domain internals, exposed to the UI
```

The type is `public` because it has to be. But it was never meant to be used here.

## The Solution

ScopeGuard introduces a `[VisibleTo]` attribute that lets you declare which namespaces are allowed to use a type. Any violation becomes a **compile-time error** — not a code review comment, not a runtime exception, a build failure.

```csharp
using ScopeGuard.Attributes;

namespace MyApp.Domain
{
    [VisibleTo("MyApp.Application.**")]
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

When it sees a member access, property read/write, or method call, it checks whether the caller's namespace matches any of the patterns declared on the target type. If not, it emits a compiler error.

## Pattern Syntax

| Pattern | Matches |
|---------|---------|
| `MyApp.Application` | Exactly that namespace |
| `MyApp.Application.*` | Any single segment under `Application` (e.g. `MyApp.Application.Handlers`, but not `MyApp.Application.Sub.Handlers`) |
| `MyApp.Application.**` | Any namespace rooted at `Application`, at any depth |

Multiple patterns are combined with OR logic — access is granted if the caller matches **any** of them.

```csharp
[VisibleTo("MyApp.Application.**", "MyApp.Tests.**")]
public class Order { ... }
```

## Examples

### Protect an entire class

```csharp
namespace MyApp.Domain
{
    [VisibleTo("MyApp.Application.**")]
    public class Invoice
    {
        public decimal Total { get; set; }
        public void Approve() { }
    }
}
```

All members of `Invoice` are restricted. Only code in `MyApp.Application` or its sub-namespaces may call `Approve()` or read `Total`.

### Allow multiple layers

```csharp
[VisibleTo("MyApp.Application.**", "MyApp.Tests.**")]
public class OrderLine { ... }
```

Both application code and tests can access `OrderLine`. Everything else cannot.

## Installation

Install the `ScopeGuard` package in every project in your solution. The package bundles both the `[VisibleTo]` attribute and the analyzer.

```xml
<PackageReference Include="ScopeGuard" Version="1.0.0" />
```

## Diagnostic Reference

| ID | Severity | Message |
|----|----------|---------|
| SG001 | Error | Member `'{0}'` is available to `'{1}'`, but is being accessed by `'{2}'`. Access denied by ScopeGuard. |
