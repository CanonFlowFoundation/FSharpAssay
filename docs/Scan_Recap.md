# FsAssay: Scan Recap & Engine Status

Since the inception of `FsAssay`, our goal has been to enforce **"Elite Functional F#"** by detecting common C#-isms, OOP paradigms, and anti-patterns that dilute domain-driven functional programming.

We have extensively battle-tested the rules engine against **10 major community repositories** to ensure it effectively catches anti-patterns in the wild. Here is where we stand.

## The Rules Engine

The scanner currently enforces 9 critical rules:
1. **FSA1001 (Mutation Overuse)**: Avoid `mutable`.
2. **FSA1002 (Partial Access)**: Avoid `Option.get`, `.Value`, `.Head`.
3. **FSA1003 (Null Reference)**: Avoid `null` checks and initialization.
4. **FSA1004 (Primitive Obsession)**: Enforce Single-Case DUs instead of type aliases.
5. **FSA1005 (Parse, Don't Validate)**: Functions should return `Result` rather than boolean validation flags.
6. **FSA1006 (Generic Catch)**: Avoid using `try/with` as general flow control.
7. **FSA1007 (Imperative Loops)**: Use `Seq.fold` or recursion over `while`.
8. **FSA1008 (OOP Inheritance)**: Avoid `inherit` and `interface ... with`.
9. **FSA1009 (Mutable Collections)**: Avoid C# `ResizeArray` and `List`.

## Scan Recaps

| Repository | Paradigm/Focus | Scan Results | Insights |
| :--- | :--- | :--- | :--- |
| **`gothinkster/fsharp-realworld-example-app`** | Web API Backend | **Violations Found** (`FSA1001`, `FSA1003`) | Highlighted standard mutation in web controllers. |
| **`jbtule/OOP-Patterns-in-FSharp`** | OOP Translations | **Violations Found** (`FSA1008`, `FSA1009`) | Lit up like a Christmas tree; successfully detected heavy reliance on Inheritance and Interfaces. |
| **`efcore/EFCore.FSharp`** | ORM Wrapper | **Violations Found** (`FSA1003`, `FSA1005`) | Revealed heavy bridging friction between C# ORMs and F#. |
| **`dotnet/docs/snippets`** | Official Docs | **Violations Found** (`FSA1005`) | Showed that even Microsoft docs frequently rely on boolean validation over "Parse, Don't Validate". |
| **`osstotalsoft/NBB`** | Message/Node Framework | **Violations Found** (`FSA1008`, `FSA1003`) | Highlighted OOP bleed in F# Mediator implementations. |
| **`SneakyPeet/EasyEventSourcing`** | Event Sourcing | **Zero Violations!** 🎉 | A triumph for pure FP. The author perfectly used DUs and Records without falling back to OOP or mutation. |
| **`fsprojects/FSharp.Desktop.UI`** | UI/MVVM Framework | **Violations Found** (`FSA1008`, `FSA1003`) | WPF and MVVM are inherently stateful/OOP. The engine correctly flagged heavy inheritance. |
| **`ronnieholm/FSharp-onion-architecture`** | Onion Architecture | **Violations Found** (`FSA1008`, `FSA1009`) | Onion architecture heavily leverages Dependency Injection and interfaces, which flagged our OOP rules. |
| **`fsprojects/FSharp.ViewModule`** | MVVM Library | **Violations Found** (`FSA1008`) | Similar to `Desktop.UI`, flagged heavy use of `ViewModelBase` inheritance. |
| **`MassTransit/MassTransit`** | Enterprise Bus | **Zero F# Violations** | Primarily a C# repository, so F# anti-patterns were not present. |

## Next Steps

We have successfully proven that `FsAssay` can parse raw F# AST logic and detect architectural paradigms at the surface text level. The rules engine is mature, documented, and actively pushes developers toward true functional-first, domain-driven design.
