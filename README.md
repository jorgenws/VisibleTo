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

Any of the following from an unauthorized namespace now fails to build:

```csharp
// In MyApp.UI.Controllers
var hash = user.PasswordHash;   // error SG001 — member access
var user = new User();          // error SG001 — object creation
```

```csharp
// In MyApp.UI
class AdminUser : User { }                     // error SG001 — inheritance
class UserList : IEnumerable<User> { ... }     // error SG001 — generic type argument
```

## What Is Checked

ScopeGuard catches every place a restricted type appears:

- **Member access** — calling a method, reading or writing a property or field on a restricted type
- **Object creation** — `new RestrictedType()`
- **Type declarations** — inheriting from or implementing a restricted type
- **Generic type arguments** — `IRepository<User>`, `List<User>`, `IHandler<Command<User>>`

The attribute is placed on the **type**, not on individual members. All uses of the type are restricted.

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

## Notes

- `[VisibleTo]` can only be applied to **classes and structs**. It cannot be placed on methods, properties, or fields.
- The attribute is **not inherited** — subclasses of a restricted type are not themselves restricted unless they carry their own `[VisibleTo]`.

## Diagnostic Reference

| ID | Severity | Message |
|----|----------|---------|
| SG001 | Error | Member `'{0}'` is available to `'{1}'`, but is being accessed by `'{2}'`. Access denied by ScopeGuard. |
