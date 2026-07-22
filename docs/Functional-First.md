# FsAssay Functional-First Architecture Specification

## 1. Executive Summary

F# is a **functional-first** programming language, not a **functional-only** language. FsAssay enforces a pure functional core in domain logic while recognizing that production systems require measured imperative constructs at application boundaries (I/O, ORMs, performance hot paths, and .NET interop).

This specification details the profile-aware enforcement matrix, local mutability scope rules, and boundary rules for FsAssay.

---

## 2. Profile Matrix

| Profile | Target Layer | Allowed Constructs | Suppressed Rules |
| :--- | :--- | :--- | :--- |
| **`core`** | Domain Models, Pure Business Logic | Immutable Records, DUs, Total Functions, `Result`/`Option` | None (All `FSA1001`-`FSA1401` enforced) |
| **`shell`** | Infrastructure, Controllers, Handlers | Persistence adapters, DB contexts | `FSA1301` (EF Core Scope) |
| **`interop`** | C# Interop, Native Wrappers | `null`, `mutable`, C# collections (`ResizeArray`) | `FSA1001` (Mutation), `FSA1003` (Null), `FSA1009` (Mutable Collections) |
| **`script`** | `.fsx` Scripts, Benchmarks | `Async.RunSynchronously`, `while` loops | `FSA1101` (Async Blocking), `FSA1007` (Imperative Loops) |
| **`performance`** | Low-level streaming, hot paths | Private local `mutable` within function body | `FSA1001` (Local Mutation only) |

---

## 3. Local Mutability Scope Rule (FSA1001 Refinement)

* **Module-level / Public Mutation**: `let mutable publicState = 0` → **Always Blocked** (`FSA1001`).
* **Private Local Mutation**: `let compute () = let mutable acc = 0; ...` → **Permitted in `performance` / `shell` profile**, blocked in `core` profile.

---

## 4. Implementation Guidelines for AGY AI Coding Agents

1. Keep all business entities, workflows, and domain rules in the **`core`** profile (Grade [S] / [A] requirement).
2. Annotate boundary interop modules with `[<Profile("interop")>]` or `[<Profile("shell")>]`.
3. Use `[<SuppressMessage("FsAssay", "FSA1001")>]` only for explicit, reviewed performance exceptions.
