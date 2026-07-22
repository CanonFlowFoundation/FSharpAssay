# FSharpAssay (FsAssay) 🧪

> **FsAssay is a compiler-backed F# design critic for humans and coding agents—strict about mechanical truth, explicit about architectural opinion, and honest about uncertainty.**

---

## 🎯 The Three Pillars of FsAssay

### 1. 🔍 Strict About Mechanical Truth
- **Compiler-Backed Precision**: Built directly on `FSharp.Compiler.Service` (FCS) and `FSharp.Analyzers.SDK`. Operates on typed AST (TAST) trees rather than crude text matching.
- **Exact Source Ranges**: Every finding points to exact line and column coordinates (`Range.StartLine`, `Range.StartColumn`) for seamless editor diagnostics.
- **Zero Comment & String False Positives**: Lexical comments and string literals containing reserved keywords (`mutable`, `null`, `while`) are automatically sanitized.

### 2. 🏛 Explicit About Architectural Opinion
- **Functional-First Profile Matrix**: Recognizes that real-world applications need pragmatism at boundaries while demanding total functional purity in core domains.
  - **`core`**: Zero-tolerance functional purity for core domain logic.
  - **`shell`**: Permits infrastructure and ORM persistence adapters (EF Core).
  - **`interop`**: Permits C# interop pragmatism (`mutable`, `null`, `ResizeArray`).
  - **`script`**: Permits `.fsx` scripting idioms (`while` loops, synchronous blocking).
  - **`performance`**: Permits measured local hot-path mutability.

### 3. ⚖️ Honest About Uncertainty
- **Four-State Verdict Kernel**: Classifies analysis outputs explicitly into `Pass`, `Fail`, `Inconclusive`, or `ToolFailure` with non-zero CLI exit codes.
- **SARIF v2.1.0 Evidence Bundles**: Exports standardized OASIS SARIF output alongside Markdown Rate Cards (Grades S–F) and Material Design 5 Dashboards.

---

## 📊 Complete Rule Catalogue Matrix (42/42 Verified Tests)

### 🔴 Tier 1: SOTA Correctness Rules (`FSA-C01` – `FSA-C08`)

| Rule ID | Rule Name | Description | Elite F# Alternative |
| :--- | :--- | :--- | :--- |
| **`FSA-C01`** | **`Unchecked.defaultof<_>`** | Using `Unchecked.defaultof<_>` outside interop boundary. | Return `Option<'T>` or proper discriminated union initialization. |
| **`FSA-C02`** | **Partial Access / `.Value`** | Calling `Option.get`, `.Value`, or `List.head` without guards. | Use pattern matching (`match opt with Some v -> ...`). |
| **`FSA-C03`** | **Async Blocking in Library** | Using `Async.RunSynchronously` inside reusable library code. | Flow `async { ... }` or `task { ... }` computation expressions. |
| **`FSA-C04`** | **Async Disposed Leak** | `use` resource bound before `Async.Start` finishes. | Pass `CancellationToken` or use `Async.StartImmediate`. |
| **`FSA-C05`** | **Incomplete DU Pattern Match** | Non-exhaustive pattern matching missing cases or wildcard `\| _ ->`. | Match all Discriminated Union cases explicitly. |
| **`FSA-C06`** | **Exceptions in Public API** | Throwing `failwith` / `raise` in public functions. | Return `Result<'T, 'Error>` error channels. |
| **`FSA-C07`** | **Non-Tail Recursion** | Self-referential recursive call in non-tail position. | Convert to tail-recursive accumulator or `Seq.fold`. |
| **`FSA-C08`** | **`Seq.length` on Infinite Seq** | Evaluating `Seq.length` on `Seq.initInfinite` or `Seq.unfold`. | Use `Seq.truncate` or bounded collection streams. |

---

### 🟠 Tier 2: SOTA Security & Guardrail Rules (`FSA-S01` – `FSA-S05`)

| Rule ID | Rule Name | Description | Elite F# Alternative |
| :--- | :--- | :--- | :--- |
| **`FSA-S01`** | **Hard-Coded Secrets** | Embedded API keys, AWS tokens (`AKIA...`), or passwords. | Inject credentials from environment variables or secret vaults. |
| **`FSA-S02`** | **Path Traversal** | Unsanitized file path input containing relative parent `..`. | Normalize and validate paths with `Path.GetFullPath`. |
| **`FSA-S03`** | **Swallowed Exceptions** | Empty `try ... with _ -> ()` or `with _ -> ignore()`. | Handle specific exceptions or propagate as `Error` results. |
| **`FSA-S04`** | **`async` Missing `return`** | `async { ... }` computation expression omitting explicit return. | Always end async blocks with explicit `return` or `return!`. |
| **`FSA-S05`** | **Task Blocking Calls** | Calling `.Result` or `.Wait()` on .NET `Task` instances. | Use `let! res = task |> Async.AwaitTask`. |

---

### 🟢 Core Functional Anti-Patterns (`FSA1001` – `FSA1401`)

| Rule Code | Name | Description | Elite F# Alternative |
| :--- | :--- | :--- | :--- |
| **`FSA1001`** | **Mutation Overuse** | Unnecessary `mutable` variables or assignment (`<-`). | Use immutable record copy syntax (`{ r with Field = v }`). |
| **`FSA1002`** | **Partial Access** | Calling `.Value`, `Option.get`, or `.Head`. | Use pattern matching or `Option.map`. |
| **`FSA1003`** | **Null Reference** | Returning or assigning raw `null`. | Return `Option<'T>`. |
| **`FSA1004`** | **Primitive Obsession** | Using primitive type aliases (`type Email = string`). | Use Single-Case Discriminated Unions (`type Email = Email of string`). |
| **`FSA1005`** | **Boolean Validation** | Returning `bool` from validation functions. | Return `Result<ParsedType, Error>` (Parse, Don't Validate). |
| **`FSA1006`** | **Generic Catch** | Catching `System.Exception` generically. | Use `Result` returning functions. |
| **`FSA1007`** | **Imperative Loops** | Using `while` loops for iteration/aggregation. | Use `Seq.fold`, `Seq.map`, or recursion. |
| **`FSA1008`** | **OOP Inheritance** | Using `inherit`, `abstract`, or `interface ... with`. | Compose functions or use Discriminated Unions. |
| **`FSA1009`** | **Mutable Collections** | Using `ResizeArray` or `List<'T>`. | Use F# immutable `list`, `array`, `Set`, or `Map`. |
| **`FSA1101`** | **Async Blocking** | Synchronous blocking via `Async.RunSynchronously`. | Use `let!` inside async computation expressions. |
| **`FSA1201`** | **Unbounded Materialization** | Materializing infinite sequences (`Seq.toList`). | Truncate sequences before materialization. |
| **`FSA1301`** | **EF Core Domain Leak** | EF Core / ORM dependencies in domain models. | Isolate persistence logic to the infrastructure shell. |
| **`FSA1401`** | **Unbounded Async Start** | Launching `Async.Start` without `CancellationToken`. | Pass `CancellationToken` or use `Async.StartImmediate`. |

---

## 🛠 CLI Options & Quick-Fix Engine

```bash
# Run analysis on a solution or project
dotnet run --project FsAssay.Runner -- /path/to/solution.sln

# Automatically display inline --fix recommendations
dotnet run --project FsAssay.Runner -- /path/to/solution.sln --fix

# Export Markdown Rate Card (Grades S through F)
dotnet run --project FsAssay.Runner -- /path/to/solution.sln -r report.md

# Export Material Design 5 HTML Dashboard
dotnet run --project FsAssay.Runner -- /path/to/solution.sln -m dashboard.html

# Export OASIS SARIF v2.1.0 Evidence Bundle
dotnet run --project FsAssay.Runner -- /path/to/solution.sln --s results.sarif
```

---

## 🧪 Building & Running Tests

FsAssay enforces a zero-regression policy backed by Expecto:

```bash
# Execute full 42-test suite
dotnet run --project FsAssay.Tests
```

---

## 📚 Philosophy & Further Reading

- [Functional-First Architecture Specification](docs/Functional-First.md)
- [Qwen 5 SOTA Specification](docs/Qwen5.md)
- [Domain Modeling Made Functional](https://fsharpforfunandprofit.com/ddd/) by Scott Wlaschin