# FSharpAssay (FsAssay) 🧪

> **🚧 STATUS: WORK IN PROGRESS (WIP)**  
> *Currently in v0.1: Engine Foundation and Core Anti-Pattern Implementation. See [Milestones.md](docs/Milestones.md) for the active roadmap.*

FsAssay is an advanced F# code analyzer built on top of the `FSharp.Analyzers.SDK`. It is specifically designed to detect "C#-ish" F# code and enforce profound **Elite F#** idioms inspired by Domain-Driven Design (DDD), such as "Make Illegal States Unrepresentable" and "Parse, Don't Validate".

If you are treating F# like C# with different syntax, FsAssay will find you.

## 🎯 Elite F# Rule Set

The following rules have been implemented and fully tested in the v0.1 engine:

| Rule Code | Name | Description | Elite F# Alternative |
| :--- | :--- | :--- | :--- |
| **FSA1001** | **Mutation Overuse** | Using the `mutable` keyword unnecessarily. | Return new copies of records using the `with` keyword. |
| **FSA1002** | **Partial Access** | Using `.Value`, `Option.get`, or `.Head` which throw exceptions on empty data. | Use pattern matching or total functions like `Option.map`. |
| **FSA1003** | **Null Reference** | Returning or assigning `null` directly. | Return `Option<'T>`. |
| **FSA1004** | **Primitive Obsession** | Using type aliases for primitives (e.g., `type Email = string`). | Use Single-Case Discriminated Unions (e.g., `type Email = Email of string`). |
| **FSA1005** | **Boolean Validation** | Naming functions `isValidX` that return `bool`. | Return `Result<ParsedType, Error>` (Parse, Don't Validate). |
| **FSA1006** | **Generic Catch** | Using `try/with` to catch `System.Exception` generically for flow control. | Use `Result` returning functions instead of exceptions. |
| **FSA1007** | **Imperative Loops** | Using `while` loops for aggregations. | Use `Seq.fold`, `Seq.map`, or recursion. |

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