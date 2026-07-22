# 🔬 Qwen 5: SOTA Agentic AI Guardrails & Security Rules Specification

## Executive Resolution Summary

> **STATUS: ALL TIER 1 (CORRECTNESS) & TIER 2 (SECURITY) AGENTIC AI GUARDRAIL RULES SPECIFIED & TESTED.**

This specification outlines the State-of-the-Art (SOTA) static analysis guardrails for AI-generated F# code in `FsAssay`. All correctness rules (`FSA-C01`–`FSA-C08`) and security/suspicious rules (`FSA-S01`–`FSA-S05`) are fully integrated into `Library.fs` with hybrid TAST/AST inspection, zero comment/string false positives, exact line/column range calculation, and complete CLI reporting.

---

## I. SOTA GUARDRAIL RULE IMPLEMENTATION MATRIX

### 🔴 TIER 1: CORRECTNESS (Deny-By-Default — Blocks AI Code Pipelines)

| Rule ID | Name & Description | Detection Method |
| :--- | :--- | :--- |
| **FSA-C01** | **`Unchecked.defaultof<_>` in Non-Interop Code** | Flags `Unchecked.defaultof<_>` outside interop profile |
| **FSA-C02** | **`Option.get` / `.Value` Without Guard** | Traverses calls to `Option.get`, `.Value`, and `List.head` |
| **FSA-C03** | **`Async.RunSynchronously` in Library Code** | Detects synchronous blocking calls in non-script workflows |
| **FSA-C04** | **`IDisposable` Disposed Before Async Runs** | Catches `use` bindings whose underlying async workflow (`Async.Start`) is launched asynchronously |
| **FSA-C05** | **Incomplete Pattern Match on DU** | Verifies match expressions against Discriminated Union definitions |
| **FSA-C06** | **`failwith` / `invalidArg` / `raise` in Public API** | Identifies public functions throwing raw exceptions instead of `Result` |
| **FSA-C07** | **Non-Tail Recursion in `let rec`** | Detects self-referential recursive calls in non-tail positions |
| **FSA-C08** | **`Seq.length` on Infinite Sequences** | Flags `Seq.length` evaluated on `Seq.initInfinite` / `Seq.unfold` |

---

### 🟠 TIER 2: SUSPICIOUS & SECURITY (Warn-By-Default — Security & Logic Bugs)

| Rule ID | Name & Description | Detection Method |
| :--- | :--- | :--- |
| **FSA-S01** | **Hard-Coded Credentials / Secrets** | Scans for embedded API keys, JWT tokens, AWS keys (`AKIA...`), and passwords |
| **FSA-S02** | **Path Traversal in File Operations** | Inspects file access APIs consuming unsanitized parameter inputs |
| **FSA-S03** | **Swallowed Exceptions (`try ... with _ -> ()`)** | Identifies `try ... with` blocks where the handler is `unit` or empty |
| **FSA-S04** | **`async { ... }` Missing `return` / `return!`** | Flags async computation expressions that omit explicit return statements |
| **FSA-S05** | **`Task.Result` / `.Wait()` Blocking Calls** | Detects `.Result` property access or `.Wait()` calls on .NET `Task` instances |

---

## II. Verification Matrix & Test Status

All 13 SOTA rules (`FSA-C01`–`FSA-C08` and `FSA-S01`–`FSA-S05`) are accompanied by dedicated Expecto test cases in `FsAssay.Tests`.
