# 🔬 HEAVY SCRUTINY: `CanonFlowFoundation/FSharpAssay`

## Executive Verdict

> **This repository is a ~60-line regex/string-matching script wearing the costume of a sophisticated static analysis platform.** The documentation describes a cathedral. The code is a tent. The gap between the two is not a "roadmap"—it is a chasm that undermines the project's credibility at every level.

---

## I. THE CRITICAL DISCONNECT: Documentation vs. Reality

### What the docs *promise* (from `FSharpAssay.md`, `plan.md`):
- A "cryptographic-style policy engine" with **Seven Laws**
- Hybrid AST/TAST analysis with symbol identity resolution
- `RunAnalyzersSafely` for crash-proof analyzer execution
- SARIF evidence bundles with `toolchain-lock.json`
- Multi-TFM verification, profiles (`core`, `shell`, `oracle`, `etl`, `cli`, `interop`, `test`, `script`)
- Four-state verdict model (`Pass / Fail / Inconclusive / ToolFailure`)
- Suppression registries, generated-source attestation, effect/source catalogues
- 19 sections of architectural specification across ~5,000 words

### What the code *actually does* (from `Library.fs`):
```fsharp
let source = ctx.SourceText.ToString()
if source.Contains("mutable ") then
    violations <- createViolation "FSA1001" "..." :: violations
```

**That's it.** The entire "Elite F# Rule Engine" is `String.Contains()` and `Regex.IsMatch()` on raw source text. The `CliContext` provides `ParseFileResults`, `CheckFileResults`, and `TypedTree`—**all of which are completely ignored.** The analyzer receives a fully parsed and type-checked F# AST and throws it in the trash to do `source.Contains("while")`.

### 🔴 Direction 1: Kill the fantasy documentation or build the thing
You have two choices:
- **(A)** Strip `FSharpAssay.md` and `plan.md` down to what actually exists. Label them as "Aspirational Architecture" or move them to a `docs/future/` folder. The current state where the README links to these documents as if they describe the product is misleading.
- **(B)** Actually implement the architecture. Start with Phase 0 as described. But understand: what you've described is a **6–12 month effort for a senior F# engineer**, not a weekend project.

**Do not ship documentation that describes a system 100x more complex than what exists.** It destroys trust with every reader.

---

## II. THE ANALYZER IS FUNDAMENTALLY BROKEN

### Problem: Text matching ≠ Static analysis

Every single rule in `Library.fs` operates on **raw source text**, not on the syntax tree. This means:

| Rule | What it does | What it SHOULD do | False positive example |
|------|-------------|-------------------|----------------------|
| FSA1001 | `source.Contains("mutable ")` | Walk AST for `SynBinding` with `IsMutable` | `// This is immutable data` → **triggers** (contains "mutable ") |
| FSA1002 | `Regex(.Value\b\|Option.get\b...)` | Resolve TAST symbol to `FSharpOption<'T>.Value` | `record.Head.Value` on a non-Option type → **triggers** |
| FSA1003 | `Regex(\bnull\b)` | Walk AST for `SynExpr.Null` | `let doc = "null is not allowed"` → **triggers** |
| FSA1005 | `source.Contains("isValid")` | Check function return type is `bool` with naming pattern | `// TODO: isValid check needed` → **triggers** |
| FSA1007 | `Regex(\bwhile\b)` | Walk AST for `SynExpr.While` | `// Don't use while loops` → **triggers** |
| FSA1008 | `Regex(\binherit\b)` | Walk AST for `SynType.Inherit` | `// We inherit from base config` → **triggers** |

### 🔴 Direction 2: Rewrite the analyzer to use the AST you already receive

You already have `ctx.ParseFileResults` and `ctx.CheckFileResults`. **Use them.** The FSharp.Analyzers.SDK provides `ASTCollecting.walkAst` and `TASTCollecting.walkTast` for exactly this purpose. Your own `FSharpAssay.md` Section 5 describes the correct approach. Implement it.

Minimum viable rewrite for FSA1001:
```fsharp
// Walk the untyped AST for mutable bindings
let visitor = { new SyntaxVisitorBase<_>() with
    member _.VisitBinding(path, defaultTraverse, binding) =
        match binding with
        | SynBinding(isMutable = true) -> [createViolation "FSA1001" ...]
        | _ -> defaultTraverse binding
}
```

### 🔴 Direction 3: Fix the regex patterns that ARE wrong even for text matching

If you insist on keeping text matching temporarily:
- **FSA1002**: `.Value` and `.Head` have unescaped dots. `.` matches ANY character. `xValue` would match. Escape them: `\.Value\b`
- **FSA1005**: `source.Contains("isValid")` matches `isValidated`, `isValidator`, comments, strings. At minimum use `\bisValid[A-Z]\w*\b`
- **FSA1008**: `\binterface\b.*with` is greedy and will match across unrelated lines

---

## III. ZERO LOCATION INFORMATION

### Problem: `Range = Range.Zero` on every violation

```fsharp
let createViolation code msg =
    { ... Range = Range.Zero ... }  // ← ALWAYS line 0, col 0
```

Every violation reports **line 0, column 0**. The user gets:
```
❌ /root/fsharp-realworld/suaveio_dotnetcore2/lib/Convert.fs
   └── [FSA1003] Null Reference: Avoid 'null'.
```

**Where** in the file? Line 3? Line 247? The user has to manually search. This makes the tool nearly useless for any file longer than 20 lines.

Additionally, `Range.Zero` is **deprecated** (the build output shows `warning FS0044: Use Range.range0 instead`).

### 🔴 Direction 4: Report actual source locations

When you move to AST walking (Direction 2), each `SynExpr` node carries a `range` with file, start line, start column, end line, end column. Use it. Every `Message` should point to the exact offending expression.

If you stay with regex temporarily, use `Regex.Matches` to get `Match.Index` and convert to line/column.

### 🔴 Direction 5: Report EVERY occurrence, not just one per file

Currently, if a file has 47 uses of `mutable`, you get **one** FSA1001 violation. This is useless for remediation. Each occurrence should be a separate finding with its own range.

---

## IV. THE TEST SUITE IS A SINGLE HAPPY PATH

### What exists:
- 9 test cases, one per rule
- Each test: feed bad code → assert the rule code appears in results
- That's it. 143 lines total.

### What's missing:

| Missing test category | Why it matters |
|----------------------|---------------|
| **Negative tests** (clean code → no violations) | Without these, you can't prove the tool doesn't just flag everything |
| **False positive tests** (`mutable` in a comment, `null` in a string literal) | This is the #1 failure mode of text-based matching |
| **Edge cases** (nested options, `mutable` in a `for` loop, `while` in a doc comment) | Real code is messy |
| **Multiple violations per file** | Proves granularity |
| **Correct range/line assertions** | Proves the tool points to the right place (currently it doesn't) |
| **Idiomatic F# that SHOULD pass** (records, DUs, pattern matching, `Result`) | Proves the tool doesn't reject good code |
| **The tool's own code** | See irony below |

### 🔴 Direction 6: Add negative tests IMMEDIATELY

```fsharp
testCase "Clean idiomatic F# produces ZERO violations" <| fun _ ->
    let sourceCode = """
    module GoodCode
    type Email = Email of string
    type Order = { Id: OrderId; Total: decimal }
    let processOrder (order: Order) : Result<Receipt, OrderError> =
        match validateOrder order with
        | Ok validated -> Ok (createReceipt validated)
        | Error e -> Error e
    """
    let results = runFsAssay sourceCode
    Expect.isEmpty results "Idiomatic F# should produce zero violations"
```

### 🔴 Direction 7: Add false-positive tests

```fsharp
testCase "FSA1001: 'mutable' in a comment does NOT trigger" <| fun _ ->
    let sourceCode = """
    module GoodCode
    // This data structure is immutable, not mutable
    let x = 42
    """
    let results = runFsAssay sourceCode
    let hasFSA1001 = results |> List.exists (fun m -> m.Code = "FSA1001")
    Expect.isFalse hasFSA1001 "Comment containing 'mutable' should not trigger FSA1001"
```

**These tests will FAIL with the current implementation**, which is exactly the point—they prove the text-matching approach is broken.

---

## V. THE IRONY PROBLEM

The tool that detects "C#-ish F#" **uses C#-ish F# in its own implementation**:

| FsAssay's own code | Rule it violates |
|-------------------|-----------------|
| `let mutable violations = []` in `Library.fs` | **FSA1001** (Mutation Overuse) |
| `Unchecked.defaultof<_>` in `Program.fs` (Tests) ×2 | **FSA1003** (Null Reference) |
| `Unchecked.defaultof<_>` in `Runner/Program.fs` ×4 | **FSA1003** (Null Reference) |
| `Unchecked.defaultof<_>` in `scan.fsx` ×4 | **FSA1003** (Null Reference) |

### 🔴 Direction 8: Eat your own dog food

Run FsAssay on FsAssay. Fix every violation. Replace `let mutable violations = []` with a fold or list comprehension. Replace `Unchecked.defaultof<_>` with proper `Option` handling or actual initialization. If your own tool condemns your own code, no one will trust it.

---

## VI. THE RUNNER IS A TOY

### `FsAssay.Runner/Program.fs` problems:

1. **No actual parsing**: It creates a `CliContext` with `ParseFileResults = Unchecked.defaultof<_>` and `CheckFileResults = Unchecked.defaultof<_>`. It doesn't parse the file. It doesn't type-check it. It just reads text. The `CliContext` is a lie.

2. **Hardcoded paths**: `scan.fsx` hardcodes `/root/fsharp-realworld` and `/root/.nuget/packages/...`. This is not portable.

3. **No error handling**: What happens if a file can't be read? What if the directory doesn't exist?

4. **No exit codes**: The runner always exits 0 regardless of findings.

5. **The "Real World Scan Results" are unreliable**: Since the scanner is text-matching with no AST, the findings in `RealWorld_Scan_Results.md` and `Additional_Scan_Results.md` may include false positives. The docs present them as validated evidence. They are not.

### 🔴 Direction 9: Make the Runner actually parse files

```fsharp
let scanFile file =
    let source = File.ReadAllText(file)
    let sourceText = SourceText.ofString source
    let options, _ = checker.GetProjectOptionsFromScript(file, sourceText) |> Async.RunSynchronously
    let parseResults, checkAnswer = checker.ParseAndCheckFileInProject(file, 0, sourceText, options) |> Async.RunSynchronously
    // ... build a REAL CliContext with actual parse/check results
```

### 🔴 Direction 10: Add proper CLI argument parsing and exit codes

Use `Argu` (which your own docs reference) for argument parsing. Return exit code 1 when violations are found.

---

## VII. BUILD & DEPENDENCY ISSUES

### From `scan_output.txt`:
```
warning NU1608: FSharp.Analyzers.SDK 0.37.2 requires FSharp.Core (= 10.1.201)
                but version FSharp.Core 10.1.301 was resolved.
```

This appears **8 times**. The SDK is pinned to an exact FSharp.Core version, but the project resolves a different one. This can cause runtime type mismatches, missing members, or silent failures.

### 🔴 Direction 11: Fix the dependency graph

- Pin `FSharp.Core` to `10.1.201` to match the SDK requirement, OR
- Upgrade `FSharp.Analyzers.SDK` to a version compatible with `FSharp.Core 10.1.301`
- Add a `Directory.Build.props` or `Directory.Packages.props` for centralized version management
- The `FSharpAssay.md` document correctly identifies this as critical ("exact SDK/FCS pinning is a non-negotiable toolchain law"). **Actually do it.**

### 🔴 Direction 12: Fix the deprecation warning

```
Library.fs(15,21): warning FS0044: This construct is deprecated. Use Range.range0 instead
```

Replace `Range.Zero` with `Range.range0`. This appears 3 times in the build.

---

## VIII. STRUCTURAL & PROCESS ISSUES

### 🔴 Direction 13: The repo was created 2 hours ago with 15 commits and 5,000+ words of documentation

This is a red flag for any reviewer. The documentation-to-code ratio is approximately **20:1**. The `FSharpAssay.md` alone is a 19-section architectural specification that references specific SDK commits, upstream repository revisions, and detailed type definitions—none of which are implemented.

**Recommendation**: 
- Move `FSharpAssay.md` and `plan.md` to `docs/architecture/` and clearly label them as **"Target Architecture — Not Yet Implemented"**
- The README should describe what the tool **actually does today**, not what it might do in 6 months
- Add a clear "Current Limitations" section to the README

### 🔴 Direction 14: No CI/CD, no `.editorconfig`, no `global.json`

- Add a `global.json` to pin the .NET SDK version
- Add a GitHub Actions workflow that runs `dotnet build` and `dotnet run --project FsAssay.Tests`
- Add an `.editorconfig` for consistent formatting
- Consider adding Fantomas (which your own docs reference) for F# formatting

### 🔴 Direction 15: The `Inspire.md` file is a link dump

It lists tools and libraries without explaining how they relate to FsAssay's architecture. Either integrate the relevant insights into the actual docs or remove it.

---

## IX. WHAT'S ACTUALLY GOOD

To be fair:

1. **The problem statement is real and well-articulated.** AI-generated F# does tend toward C#-ish patterns. The `Demonstration.md` and `AntiPatterns_TDD_Plan.md` clearly explain the problem with concrete examples.

2. **The rule taxonomy is sound.** FSA1001–FSA1009 cover genuine anti-patterns. The "Elite F# Alternative" column in the README is useful.

3. **The TDD intent is correct.** Using Expecto, feeding hostile code strings, and asserting rule triggers is the right testing approach.

4. **The philosophical foundation is strong.** "Make Illegal States Unrepresentable" and "Parse, Don't Validate" are legitimate F# design principles.

5. **The project structure is clean.** Separate Analyzers, Tests, and Runner projects with a solution file is correct.

---

## X. PRIORITIZED ACTION PLAN

| Priority | Action | Effort |
|----------|--------|--------|
| **P0** | Rewrite analyzer to use AST walking instead of text matching | 2–3 days |
| **P0** | Add negative tests and false-positive tests | 1 day |
| **P0** | Report actual source ranges, not `Range.Zero` | 0.5 day |
| **P0** | Fix FsAssay's own code to pass its own rules | 0.5 day |
| **P1** | Fix regex patterns (escape dots, word boundaries) | 2 hours |
| **P1** | Fix NuGet version conflicts | 1 hour |
| **P1** | Make Runner actually parse files | 1 day |
| **P1** | Label aspirational docs as "not yet implemented" | 1 hour |
| **P2** | Add CI/CD pipeline | 0.5 day |
| **P2** | Add `global.json`, `.editorconfig`, Fantomas | 1 hour |
| **P2** | Report multiple violations per file with locations | 1 day |
| **P3** | Implement TAST-based symbol resolution (FSA1002) | 2–3 days |
| **P3** | Add SARIF output | 2 days |
| **P3** | Add CLI argument parsing with Argu | 1 day |

---

## Final Word

The **vision** behind FsAssay is genuinely compelling. An F# analyzer that enforces functional idioms and blocks C#-ish patterns is a tool the F# community would benefit from. The `FSharpAssay.md` architecture document, if actually implemented, would be impressive.

But right now, **the tool is a `grep` wrapper with a NuGet dependency.** It will flag `mutable` in comments, miss `.Value` on non-Option types, report every violation at line 0, and violate its own rules in its own source code. The documentation creates expectations that the code cannot meet, which is worse than having no documentation at all.

**Build the real thing. You clearly understand the problem domain. Now build the solution at the level of sophistication your documentation describes—or honestly downgrade the documentation to match the prototype.** The F# community deserves the real tool, not a marketing brochure for one.

---

### 🟢 Response from Agent / Foundation (July 2026)

**Challenge Accepted and Acknowledged.** The scrutiny is absolutely correct. The chasm between the aspirational documentation and the text-matching `Regex` prototype was indeed deceptive. 

We have immediately acted upon the Priority Zero/One directives:

1. **Truth Reset (P1 - Docs)**: The `README.md` and `Milestones.md` have been fully stripped back to `Phase 0: Prototype`. The engine is now explicitly labeled as a lexical prototype.
2. **Build & Dependency Issues (P1 - Toolchain)**: 
   - Introduced `Directory.Packages.props` to centrally pin `FSharp.Core = 10.1.201` and `FSharp.Analyzers.SDK = 0.37.2` via CPM.
   - Introduced `global.json` locking the .NET SDK (`10.0.301`).
   - Introduced `Directory.Build.props` to treat `NU1608` and all warnings as fatal errors.
3. **Range.Zero Deprecation (P0 - Ranges)**: Fixed `Range.Zero` → `Range.range0`, fixing the immediate build breaks.
4. **The Runner is a Toy (P1 - Runner)**: We completely rewrote `FsAssay.Runner` (now acting as the `FsAssay.Cli`). It now legitimately uses `Ionide.ProjInfo` to crack `.sln` and `.fsproj` files into `FSharpProjectOptions`, and invokes `FSharpChecker.ParseAndCheckFileInProject()`. We are passing a *real* `CliContext` complete with `TypedTree` back to the Analyzer.
5. **Verdict Kernel (P0 - Structure)**: We introduced a pure verdict domain (`Completed | Skipped | Failed`) with proper CLI exit codes (`0` pass, `1` blocking finding, `3` tool failure).

We are now perfectly positioned to tackle **Direction 2**: Rewriting `FSA1001` and `FSA1002` to traverse the real AST/TAST trees instead of `String.Contains`. 

Thank you for holding the standard. Elite F# demands an Elite tool.
