# FSharpAssay (FsAssay) 🧪

> **🚧 STATUS: v0.0.1 LEXICAL PROTOTYPE**  
> *The current engine is a regex-based prototype used to orchestrate the SDK. The true AST/TAST verification engine has NOT been started. See [Milestones.md](docs/Milestones.md) for the active roadmap.*

FsAssay aims to be an advanced F# code analyzer built on top of the `FSharp.Analyzers.SDK`. It is specifically designed to detect "C#-ish" F# code and enforce profound **Elite F#** idioms inspired by Domain-Driven Design (DDD).

If you are treating F# like C# with different syntax, FsAssay will find you.

## 🎯 Elite F# Rule Catalogue (Ideation Phase)

The following rules have been prototyped lexically, but are awaiting proper implementation using the F# Typed Abstract Syntax Tree (TAST) and AST profiles.

| Rule Code | Name | Description | Elite F# Alternative |
| :--- | :--- | :--- | :--- |
| **FSA1001** | **Mutation Overuse** | Using the `mutable` keyword unnecessarily. | Return new copies of records using the `with` keyword. |
| **FSA1002** | **Partial Access** | Using `.Value`, `Option.get`, or `.Head` which throw exceptions on empty data. | Use pattern matching or total functions like `Option.map`. |
| **FSA1003** | **Null Reference** | Returning or assigning `null` directly. | Return `Option<'T>`. |
| **FSA1004** | **Primitive Obsession** | Using type aliases for primitives (e.g., `type Email = string`). | Use Single-Case Discriminated Unions (e.g., `type Email = Email of string`). |
| **FSA1005** | **Boolean Validation** | Naming functions `isValidX` that return `bool`. | Return `Result<ParsedType, Error>` (Parse, Don't Validate). |
| **FSA1006** | **Generic Catch** | Using `try/with` to catch `System.Exception` generically for flow control. | Use `Result` returning functions instead of exceptions. |
| **FSA1007** | **Imperative Loops** | Using `while` loops for aggregations. | Use `Seq.fold`, `Seq.map`, or recursion. |
| **FSA1008** | **OOP Inheritance** | Using `inherit`, `abstract`, or `interface`. | Compose functions or use Discriminated Unions. |
| **FSA1009** | **Mutable Collections** | Using `ResizeArray` or `System.Collections.Generic.List`. | Use F# immutable `list`, `array`, or `Map`. |

## 🛠 Building & Testing Locally

FsAssay uses a Test-Driven Development (TDD) approach using `Expecto` to safely compile hostile F# strings in-memory and prove the analyzer logic.

```bash
# Run the Expecto Test Suite
dotnet run --project FsAssay.Tests
```

## 📚 Philosophy & Inspiration

This project draws heavy inspiration from the following resources:
- [Domain Modeling Made Functional](https://fsharpforfunandprofit.com/ddd/) by Scott Wlaschin
- [Workflows in F#](https://github.com/mjul/workflows-in-fsharp)
- [F# Cheatsheet](https://fsprojects.github.io/fsharp-cheatsheet/)