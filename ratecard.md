# 🧪 FsAssay F# Code Quality Rate Card

**Timestamped Report:** `2026-07-22 03:40:27 UTC`
**Target Repos / Scanned Files:** 9 file(s)

## 🏆 Overall Code Base Rating

> ### Grade: **[F] Hostile / Worst Anti-Patterns** — Score: `0 / 100`
> *Purity Level: Worst. C# code translated to F# syntax with nulls, exceptions, and procedural mutation.*

---

## 📊 Anti-Pattern Spectrum Breakdown

| Classification Tier | Metric / Description | Status / Count |
| :--- | :--- | :--- |
| 🔴 **Worst (Hostile Anti-Patterns)** | Null References (`FSA1003`), Unsafe Casts (`FSA2016`) | 6 violation(s) |
| 🟧 **Bad (Imperative Intrusion)** | Mutable State (`FSA1001`), Mutable Collections (`FSA1009`) | 76 violation(s) |
| 🟩 **Goodness (Elite Functional)** | Immutability, Total Functions, Discriminated Unions | Active Target |

## 📁 File-by-File Quality Rate Card

| File Path | Violations | Rating Tier |
| :--- | :--- | :--- |
| `/root/FSharpAssay/FsAssay.Runner/AutoFix.fs` | 2 | **[B] Hybrid / Acceptable F#** |
| `/root/FSharpAssay/FsAssay.Runner/Orchestrator.fs` | 6 | **[C] C#-in-F# Smell (Bad)** |
| `/root/FSharpAssay/FsAssay.Runner/Output.fs` | 12 | **[F] Hostile / Worst Anti-Patterns** |
| `/root/FSharpAssay/FsAssay.Runner/Server.fs` | 5 | **[C] C#-in-F# Smell (Bad)** |
| `/root/FSharpAssay/FsAssay.Runner/Program.fs` | 15 | **[F] Hostile / Worst Anti-Patterns** |
| `/root/FSharpAssay/FsAssay.Analyzers/Library.fs` | 79 | **[F] Hostile / Worst Anti-Patterns** |

## 🛠️ Actionable Remediation Guidance

1. **Eliminate `null` References**: Replace `null` returns and parameters with `Option<'T>`. Use `Option.defaultValue` or `Option.map` to safely handle missing data.
2. **Eliminate `mutable` Variables**: Use `with` record updates or `Seq.fold` for accumulator loops.
3. **Replace Primitive Aliases**: Model domain types using Single-Case Discriminated Unions (e.g., `type CustomerId = CustomerId of Guid`) to make illegal states unrepresentable.
