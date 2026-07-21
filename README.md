# FSharpAssay (FsAssay) 🧪

> **STATUS: v0.1.0 HYBRID TAST & ACCURATE AST ENGINE**  
> *FsAssay orchestrates the `FSharp.Analyzers.SDK` with a hybrid TAST (Typed AST) visitor and line-accurate pattern analysis engine. High-noise lexical rules are isolated to TAST inspection to guarantee 0 compiler-generated false positives.*

FsAssay is an advanced F# code analyzer specifically designed to detect "C#-ish" F# code and enforce profound **Elite F#** idioms inspired by Domain-Driven Design (DDD).

If you are treating F# like C# with different syntax, FsAssay will find you.

---

## 🏆 Code Quality Rate Card & Material Design 5 Dashboard

FsAssay includes an automated **Code Quality Rating Engine** that evaluates codebases across an **Anti-Pattern Spectrum** (Goodness vs. Imperative Intrusion vs. Hostile Anti-Patterns) to assign an overall grade and score:

* 🌟 **Grade [S] (Elite F# Mastery)** `95–100`: Pure functional, total functions, DUs, and immutability.
* 🟩 **Grade [A] (Idiomatic Functional F#)** `85–94`: Clean expression-oriented code with isolated side effects.
* 🟨 **Grade [B] (Hybrid / Acceptable F#)** `70–84`: Moderate functional code with occasional imperative loops.
* 🟧 **Grade [C] (C#-in-F# Smell / Bad)** `50–69`: Heavy mutability, primitive obsession, or class inheritance.
* 🔴 **Grade [F] (Hostile / Worst Anti-Patterns)** `0–49`: Procedural C# translated to F# syntax with `null`s, exceptions, and procedural mutation.

### 🚀 CLI Usage & Reporting Options

```bash
# Generate Markdown Rate Card (-r) and Material Design 5 HTML Dashboard (-m)
dotnet run --project FsAssay.Runner -- -r ratecard.md -m dashboard.html /path/to/target

# Canonical JSON output (-j) & SARIF format (-s)
dotnet run --project FsAssay.Runner -- -j output.json -s output.sarif /path/to/target
```

---

## 🎯 Elite F# Rule Catalogue

FsAssay enforces 30+ rules covering exhaustiveness, purity, type safety, and domain modeling:

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
| **FSA2016** | **Unsafe Cast** | Performing runtime downcasts (`:?>`) or unboxing. | Model state alternatives as a Discriminated Union. |
| **FSA2019** | **Missing CE** | Deeply nested `match` expressions on `Result`/`Option`. | Use Computation Expressions (`result { ... }`). |
| **FSA2029** | **Exception Throwing** | Explicitly throwing exceptions via `failwith`/`raise`. | Return `Result<'T, 'Error>` or `Option<'T>`. |

---

## 🛠 Building & Testing Locally

FsAssay uses a Test-Driven Development (TDD) approach with `Expecto` to safely compile hostile F# strings in-memory and prove the analyzer logic.

```bash
# Run the Expecto Test Suite
dotnet run --project FsAssay.Tests
```

---

## 📚 Philosophy & Inspiration

This project draws heavy inspiration from the following resources:
- [Domain Modeling Made Functional](https://fsharpforfunandprofit.com/ddd/) by Scott Wlaschin
- [Stylish F#](https://github.com/ArunNotFound/functional-skills/blob/main/stylish-fsharp/SKILL.md)
- [Workflows in F#](https://github.com/mjul/workflows-in-fsharp)
- [F# Cheatsheet](https://fsprojects.github.io/fsharp-cheatsheet/)