# FsAssay: Scan Recap & Engine Status

Since the inception of `FsAssay`, our goal has been to enforce **"Elite Functional F#"** by detecting common C#-isms, OOP paradigms, and anti-patterns that dilute domain-driven functional programming.

We have extensively battle-tested the rules engine against **major community and domain repositories** to ensure it effectively catches anti-patterns in the wild. Here is where we stand.

## Engine Status & Features (v0.1.0)

The scanner features a **Hybrid TAST & Line-Accurate AST Engine** supporting:
- **Canonical JSON (`-j`)** & **SARIF v2.1.0 (`-s`)** emission.
- **Timestamped Rate Card Markdown (`-r`)**: Automated S/A/B/C/F grade calculation based on weighted severity.
- **Material Design 5 HTML Dashboard (`-m`)**: Standalone, interactive HTML5 report with HSL color themes and expandable code violation cards.
- **Precise Line Ranges**: String index offset calculation guarantees exact source line reporting.

## Key Rule Categories

1. **FSA1001 (Mutation Overuse)**: Avoid `mutable`.
2. **FSA1002 (Partial Access)**: Avoid `Option.get`, `.Value`, `.Head`.
3. **FSA1003 (Null Reference)**: Avoid `null` checks and initialization.
4. **FSA1004 (Primitive Obsession)**: Enforce Single-Case DUs instead of type aliases.
5. **FSA1005 (Parse, Don't Validate)**: Functions should return `Result` rather than boolean validation flags.
6. **FSA1006 (Generic Catch)**: Avoid using `try/with` as general flow control.
7. **FSA1007 (Imperative Loops)**: Use `Seq.fold` or recursion over `while`.
8. **FSA1008 (OOP Inheritance)**: Avoid `inherit` and `interface ... with`.
9. **FSA1009 (Mutable Collections)**: Avoid C# `ResizeArray` and `List`.
10. **FSA2016 (Unsafe Cast)**: Runtime downcasting (`:?>`) or unboxing.
11. **FSA2019 (Missing CE)**: Nested `match` expressions on `Result`/`Option`.
12. **FSA2029 (Exception Throwing)**: Explicit exception throwing via `failwith`/`raise`.

## Scan Recaps Across In-The-Wild Repositories

| Repository | Focus | Findings | Key Insights |
| :--- | :--- | :--- | :--- |
| **`CanonFlowFoundation/CanonFlow`** | Enterprise Core | **Scanned** | Identified mutability (`FSA1001`), OOP inheritance (`FSA1008`), and nested calls (`FSA2023`). |
| **`CanonFlowFoundation/GSTFlow`** | Rules & UI Engine | **Scanned** | Highlighted `null` references in WASM wrappers (`FSA1003`) and mutable accumulator lists (`FSA1001`). |
| **`gothinkster/fsharp-realworld-example-app`** | Web API Backend | **Violations Found** | Highlighted standard mutation in web controllers. |
| **`SneakyPeet/EasyEventSourcing`** | Event Sourcing | **Zero Violations!** 🎉 | Perfect usage of DUs and Records without falling back to OOP or mutation. |
