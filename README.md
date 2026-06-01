# VisibleTo

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

VisibleTo introduces a `[VisibleTo]` attribute that lets you declare which namespaces are allowed to use a type. Any violation becomes a **compile-time error** — not a code review comment, not a runtime exception, a build failure.

```csharp
using VisibleTo.Attributes;

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
var user = new User();                         // error VT001 — object creation
var hash = user.PasswordHash;                  // error VT001 — member access
```

```csharp
// In MyApp.UI
class AdminUser : User { }                     // error VT001 — inheritance
class UserList : IEnumerable<User> { }         // error VT001 — generic type argument
void Handle(User user) { }                     // error VT001 — method parameter
User GetCurrent() => null!;                    // error VT001 — return type
private User _current;                         // error VT001 — field type
```

## What Is Checked

VisibleTo catches every place a restricted type appears:

- **Member access** — calling a method, reading or writing a property or field on a restricted type
- **Object creation** — `new RestrictedType()`
- **Inheritance and interface implementation** — `class Sub : RestrictedType`, `class Impl : IRestrictedInterface`
- **Method signatures** — a restricted type as a parameter type or return type, including constructors
- **Field and property types** — `private User _user`, `public User Current { get; set; }`
- **Delegate signatures** — a restricted type as a parameter or return type of a delegate declaration
- **Event types** — `public event UserChanged OnChanged`
- **Generic type arguments** — in all of the above: `List<User>`, `IRepository<User>`, `Action<User>`, `IHandler<Command<User>>`

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
- **Same-layer references**: because member signatures are checked, types in the same layer that reference each other must include their own namespace in the allowed list. For example, if `User` and `Order` are both in `MyApp.Domain` and `Order` appears in a `User` method signature, `Order`'s `[VisibleTo]` must include `"MyApp.Domain.**"` alongside any other allowed namespaces.

## Diagnostic Reference

| ID | Severity | Message |
|----|----------|---------|
| VT001 | Error | Member `'{0}'` is available to `'{1}'`, but is being accessed by `'{2}'`. Access denied by VisibleTo. |
