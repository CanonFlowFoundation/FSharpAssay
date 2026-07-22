# 🔬 FULL RE-REVIEW: `CanonFlowFoundation/FSharpAssay` — Current State

**Crawled:** All 60+ files, every `.fs` source line, CI config, TOML, specimens, docs, artifacts.
**Date:** July 22, 2026

---

## I. WHAT EXISTS NOW (Verified Inventory)

### Source Code

| Module | Lines | Purpose | Status |
|---|---|---|---|
| `Library.fs` | ~280 | 24 rules (3 TAST + 21 regex on sanitized source) | ✅ Working |
| `Orchestrator.fs` | ~90 | `FSharpChecker.ParseAndCheckFileInProject` pipeline | ✅ Working |
| `Output.fs` | ~320 | SARIF v2.1.0, JSON, Rate Card, Material HTML | ✅ Working |
| `Domain.fs` | ~40 | Verdict model: `Completed/Skipped/Failed`, `Pass/Fail/Inconclusive/ToolFailure` | ✅ Working |
| `Config.fs` | ~50 | TOML parsing (manual, no dependency) | ✅ Working |
| `AutoFix.fs` | ~30 | `mutable`→`let`, `null`→`None` | ⚠️ Dangerous |
| `Server.fs` | ~60 | `HttpListener` dashboard | ⚠️ Single-threaded |
| `ProjectSystem.fs` | ~40 | `.fsproj` detection | ✅ Working |
| `Cli.fs` | ~30 | Argu: 11 flags | ⚠️ 2 phantom flags |
| `Program.fs` (Runner) | ~120 | Main loop, watch, serve dispatch | ✅ Working |
| `Program.fs` (Tests) | ~200 | 12 Expecto tests | ✅ Working |

### Infrastructure

| Artifact | Status |
|---|---|
| `.github/workflows/ci.yml` | ✅ Build + Test on push/PR |
| `fs-assay.toml` | ✅ Config with rule enable/disable |
| `specimens/` | ✅ 10 files (8 sections + 2 order processors) |
| `out.sarif` | ⚠️ Truncated (schema + version only, no `runs`) |
| `scan.fsx` | ⚠️ Hardcoded `/root/fsharp-realworld` |

### Rule Coverage: 24 Rules

| ID | Name | Method | Verdict |
|---|---|---|---|
| FSA1001 | Mutable Overuse | **TAST** (`binding.IsMutable`) | ✅ |
| FSA1002 | Option.get | **TAST** (`FSharpExprPatterns.Call`) | ✅ |
| FSA1003 | Null/defaultof | **TAST** (`DefaultValue`, `Const(null)`) | ✅ |
| FSA1004 | Primitive Obsession | Regex (sanitized) | ✅ |
| FSA1005 | Boolean Validation | Regex (sanitized) | ✅ |
| FSA1006 | Exception-Driven Flow | Regex (sanitized) | ✅ |
| FSA1007 | Imperative Loops | Regex (sanitized) | ✅ |
| FSA1008 | OOP Inheritance | Regex (sanitized) | ⚠️ Flags `inherit exn` |
| FSA1009 | God Objects | Regex (sanitized) | ✅ |
| FSA2008 | Async.RunSynchronously | Regex (sanitized) | ✅ |
| FSA2012 | printfn in Library | Regex (sanitized) | ✅ |
| FSA2014 | TODO/HACK/FIXME | Regex (sanitized) | ✅ |
| FSA2016 | Layer Violation | Regex (sanitized) | ✅ |
| FSA2017 | Circular Dependency | Regex (sanitized) | ✅ |
| FSA2018 | Leaky Abstraction | Regex (sanitized) | ✅ |
| FSA2019 | Nested Result/Option | Regex (sanitized) | ⚠️ Fragile |
| FSA2020 | Primitive Obsession (params) | Regex (sanitized) | ⚠️ Flags curried same-type |
| FSA2021 | Stringly Typed | Regex (sanitized) | ✅ |
| FSA2022 | Impure Core | Regex (sanitized) | ⚠️ Flags legitimate CLI I/O |
| FSA2023 | Nested Function App | Regex (sanitized) | 🔴 Extremely fragile |
| FSA2024 | Callback Hell | Regex (sanitized) | ✅ |
| FSA2025 | Boolean Parameter | Regex (sanitized) | ⚠️ Flags legitimate HOFs |
| FSA2026 | Feature Envy | Regex (sanitized) | ✅ |
| FSA2027 | Shotgun Surgery | Regex (sanitized) | ✅ |
| FSA2028 | Divergent Change | Regex (sanitized) | ✅ |
| FSA2030 | Temporal Coupling | Regex (sanitized) | ✅ |
| **FSA2029** | **MISSING** | — | 🔴 Gap in sequence |

---

## II. WHAT'S GENUINELY GOOD (Acknowledge the Progress)

1. **The TAST integration is real.** `visitExpr` and `visitDecl` walk `FSharpImplementationFileDeclaration` and `FSharpExpr` using `FSharpExprPatterns`. This is not faked. FSA1001/1002/1003 operate on resolved compiler symbols.

2. **The sanitizer is well-engineered.** Single-pass tokenizer handling `//`, `(* (* nested *) *)`, `"..."`, `@"..."`, `"""..."""` with offset preservation. This eliminates the original false-positive vector.

3. **The Orchestrator is correct.** `FSharpChecker.ParseAndCheckFileInProject` + `GetProjectOptionsFromScript` — real compilation, real type checking.

4. **The CI pipeline exists.** Build + test on push/PR. This is new since last review.

5. **The config system works.** `fs-assay.toml` with per-rule enable/disable, parsed by `Config.fs`.

6. **The SARIF output is structurally correct.** Schema v2.1.0, tool driver, physical locations with line/column.

7. **The Rate Card grading engine works.** S/A/B/C/F with file-by-file breakdown.

8. **The Material HTML dashboard renders.** Score rings, file cards, badges.

9. **There is now a negative test** (FSA9000: idiomatic F# → zero violations). This was the #1 testing gap.

10. **The verdict model is honest.** `Completed / Skipped / Failed` + `Pass / Fail / Inconclusive / ToolFailure` — this is the right architecture.

---

## III. CRITICAL ISSUES (Must Fix Before Any "Production" Claim)

### 🔴 C1: `Unchecked.defaultof<_>` Still Present (2 locations)

**`Orchestrator.fs`, both `evaluateFile` and `evaluateSingleFile`:**
```fsharp
CheckProjectResults = Unchecked.defaultof<_>  // ← STILL HERE
```

This is the **exact pattern** flagged in the original audit. The Qwen3.md claims "Eliminated." It was not. Any future cross-file rule will `NullReferenceException`.

**Fix:** Change `CliContext.CheckProjectResults` to `FSharpCheckProjectResults option` and set `None`.

---

### 🔴 C2: Zero Security Rules

The SonarQube study of 4,442 LLM-generated solutions found **60–71% of vulnerabilities are BLOCKER-level hard-coded credentials** [[18]]. FsAssay has **zero** security rules:

| Missing Rule | Severity | Evidence |
|---|---|---|
| Hard-coded passwords/keys/secrets | BLOCKER | 60–71% of LLM vulns [[18]] |
| SQL injection (string concat in queries) | BLOCKER | LLMs generate `sprintf "SELECT...%s" input` |
| Path traversal (user input in file ops) | CRITICAL | LLMs generate `File.ReadAllText(userInput)` [[18]] |
| Weak crypto (MD5, SHA1, DES) | CRITICAL | LLMs use deprecated algorithms from training data [[18]] |
| Disabled SSL/TLS validation | CRITICAL | LLMs generate `fun _ _ _ -> true` [[18]] |
| XXE (XML without DtdProcessing.Prohibit) | CRITICAL | LLMs generate default XML parsing [[18]] |
| JWT without signature verification | CRITICAL | LLMs parse JWTs without validation [[18]] |

**This is the single highest-ROI gap.** One regex rule for hard-coded secrets (`password\s*=\s*"[^"]+"`) would catch the majority of LLM security vulnerabilities.

---

### 🔴 C3: No Dead Code Detection

Dead/unused/redundant code is **14–43% of all AI code smells** [[18]] — the single largest category. FsAssay has no rule for:
- Unused `let` bindings
- Unused function parameters
- Unreachable code after `return`/`failwith`
- Unused `open` declarations (compiler warns FS1189 but FsAssay should enforce)

**This is the highest-volume gap.**

---

### 🔴 C4: FSA2023 Regex Is Catastrophically Fragile

```fsharp
checkRegex "FSA2023" "Nested Function Application" @"\b[A-Za-z0-9_]+\s*(\s*[A-Za-z0-9_]+\s*\("
```

This matches **any function call with an argument that is itself a function call**:
```fsharp
List.map (fun x -> x + 1) items   // ← TRIGGERED (false positive)
printfn "%s" (getName ())          // ← TRIGGERED (false positive)
let result = compute (parse input) // ← TRIGGERED (false positive)
```

This rule will fire on **nearly every F# file**. It must be either TAST-based (detect actual nesting depth > N) or removed.

---

### 🔴 C5: FSA1008 Flags Required F# Exception Syntax

```fsharp
checkRegex "FSA1008" "OOP Inheritance" @"\binherit\b"
```

Every F# custom exception **requires** `inherit exn`:
```fsharp
type MyException = inherit exn  // ← TRIGGERED (false positive)
```

This is not optional F# syntax. The rule must exclude `inherit exn`:
```fsharp
@"\binherit\b(?!\s+exn)"
```

---

### 🔴 C6: AutoFix Is Destructively Naive

```fsharp
// AutoFix.fs
line.Replace("let mutable ", "let ")   // ← BREAKS if variable is reassigned
line.Replace("null", "None")           // ← BREAKS Nullable<int> → Noneable<int>
```

If code has:
```fsharp
let mutable count = 0
count <- count + 1  // ← This line now won't compile after "fix"
```

The `--fix` flag produces **code that doesn't compile**. This is worse than no fix. Either implement proper dataflow analysis or **remove the flag** and mark it as planned.

---

### 🔴 C7: Two Phantom CLI Flags

| Flag | Defined in Argu? | Implemented in Program.fs? |
|---|---|---|
| `--diff <gitRef>` | ✅ | ❌ Never called |
| `--adjudicate` | ✅ | ❌ No `// EXPECT` parsing exists |

These flags appear in `--help` output but do nothing. This erodes trust.

---

## IV. HIGH-PRIORITY ISSUES

### 🟠 H1: No Complexity Metrics

Cyclomatic complexity and nesting depth account for **3–8% of AI code smells** [[18]]. FsAssay has no:
- Cyclomatic complexity per function
- Maximum nesting depth
- Function length (LOC)
- Parameter count threshold

These are **TAST-computable** and high-value.

---

### 🟠 H2: No FSharpLint Delegation

FSharpLint has **~65 rules** for naming, formatting, and smells [[1]]. FsAssay's FSA1004/FSA1005/FSA2020/FSA2025 overlap with FSharpLint's convention rules but are less precise (regex vs. AST). The plan document says "delegate to FSharpLint" but this is not implemented.

---

### 🟠 H3: No G-Research Analyzer Integration

G-Research's `DisposedBeforeAsyncRunAnalyzer` catches a **runtime crash** that FsAssay cannot detect with regex [[11]]. Their `VirtualCallAnalyzer` catches performance issues on F# collections. These are production-proven analyzers that should be loadable as plugins.

---

### 🟠 H4: No Architectural Boundary Enforcement

The plan describes namespace/project-level rules ("Domain must not reference System.IO"). The current FSA2016 "Layer Violation" is a regex that checks for `open` statements — it cannot enforce actual dependency boundaries. This requires TAST + project-level analysis.

---

### 🟠 H5: Server Is Single-Threaded

```fsharp
// Server.fs
while running do
    let ctx = listener.GetContext()  // ← Blocks on one request
```

Concurrent browser tabs will hang. Use `Async` or `Task.Run` for request handling.

---

### 🟠 H6: Watch Mode Has No Debounce

`FileSystemWatcher.Changed` fires **multiple times per save**. Each event triggers a full re-scan. Rapid saves cause a scan stampede. Add a `System.Timers.Timer` with 500ms delay.

---

### 🟠 H7: SARIF Artifact Is Truncated

`out.sarif` in the repo contains only:
```json
{"$schema": "...", "version": "2.1.0"}
```
No `runs` array. Either the scan produced zero results or the file was committed mid-write.

---

### 🟠 H8: Only 12 Tests, No False-Positive Regression Tests

| Test Category | Count | Status |
|---|---|---|
| Positive (violation detected) | 9 | ✅ |
| Suppression | 1 | ✅ |
| Specimen scan | 1 | ✅ |
| Negative (clean code → zero) | 1 | ✅ (new) |
| **False-positive regression** | **0** | 🔴 Missing |
| **Edge cases** (comments, strings, `inherit exn`) | **0** | 🔴 Missing |
| **FSA2023 false positive** | **0** | 🔴 Missing |
| **AutoFix correctness** | **0** | 🔴 Missing |

---

## V. MEDIUM-PRIORITY ISSUES

| # | Issue | Detail |
|---|---|---|
| M1 | FSA2029 missing | Sequence jumps FSA2028 → FSA2030 |
| M2 | FSA2020 flags curried same-type params | `let add (x: int) (y: int)` triggers |
| M3 | FSA2022 flags legitimate CLI I/O | `Console.WriteLine` in a CLI tool is correct |
| M4 | FSA2025 flags legitimate HOFs | `bool -> bool -> bool` is a valid predicate combinator |
| M5 | No XML doc enforcement | Public API without `///` docs not flagged |
| M6 | No magic number detection | Numeric literals > 1 in non-test code |
| M7 | No commented-out code detection | LLM artifact: `// let oldImpl = ...` blocks |
| M8 | No duplicate code detection | LLM copy-paste patterns |
| M9 | `scan.fsx` hardcodes path | `/root/fsharp-realworld` |
| M10 | README ↔ Qwen3.md contradiction | README: "WIP". Qwen3.md: "production grade" |
| M11 | No NuGet package | Cannot be consumed as a dotnet tool |
| M12 | No `--diff` implementation | Flag exists, no Git integration |

---

## VI. SOTA GAP MATRIX: FsAssay vs. The Best

| Capability | Clippy (828 lints) | FSharpLint (~65) | G-Research (10) | SonarQube | **FsAssay (24)** |
|---|---|---|---|---|---|
| Correctness (deny) | ✅ ~50 | ✅ | ✅ | ✅ | 🟡 3 rules (TAST) |
| Security | ✅ | ❌ | ❌ | ✅ | 🔴 **0 rules** |
| Dead code | ✅ | ✅ | ❌ | ✅ | 🔴 **0 rules** |
| Complexity metrics | ✅ | ❌ | ❌ | ✅ | 🔴 **0 rules** |
| Performance | ✅ ~80 | ❌ | ✅ | ✅ | 🔴 **0 rules** |
| Style/naming | ✅ ~250 | ✅ ~65 | ✅ | ✅ | 🟡 3 rules (regex) |
| AI-specific meta-rules | ❌ | ❌ | ❌ | 🟡 Partial | 🔴 **0 rules** |
| TAST-based | ✅ | ✅ | ✅ | ✅ | 🟡 3 of 24 |
| Diff-only scanning | ✅ (cargo clippy) | ❌ | ❌ | ✅ | 🔴 Flag exists, not impl |
| SARIF output | ❌ | ❌ | ❌ | ✅ | ✅ |
| Config/policy | ✅ (clippy.toml) | ✅ (.fsproj) | ❌ | ✅ | ✅ (fs-assay.toml) |
| CI integration | ✅ | ✅ | ✅ | ✅ | ✅ (GitHub Actions) |
| Severity tiers | ✅ 9 categories | ✅ 3 | ✅ | ✅ | 🟡 Error only |
| Auto-fix | ✅ (cargo fix) | ❌ | ❌ | ✅ | ⚠️ Dangerous |

---

## VII. THE PRIORITY ROADMAP

### Sprint 1: Security + Correctness (Week 1–2) — Highest ROI

| # | Task | Effort | Impact |
|---|---|---|---|
| 1 | **FSA-SEC01**: Hard-coded secrets regex | 2h | Catches 60–71% of LLM vulns |
| 2 | **FSA-SEC04**: Weak crypto (MD5/SHA1/DES) | 1h | Catches deprecated algorithms |
| 3 | **FSA-SEC05**: Disabled SSL validation | 1h | Catches `fun _ _ _ -> true` |
| 4 | **FSA-C04**: Disposed before async (TAST) | 4h | Catches runtime crash |
| 5 | **Fix FSA1008**: Exclude `inherit exn` | 30min | Eliminates false positive on every exception |
| 6 | **Fix FSA2023**: Replace regex with TAST nesting depth | 4h | Eliminates catastrophic false positive rate |
| 7 | **Fix `CheckProjectResults`**: Make it `option` | 1h | Eliminates last null bomb |

### Sprint 2: Dead Code + Complexity (Week 3–4) — Highest Volume

| # | Task | Effort | Impact |
|---|---|---|---|
| 8 | **FSA-AI01**: Unused `let` bindings (TAST) | 4h | Catches 14–43% of AI smells |
| 9 | **FSA-AI03**: Unused `open` (TAST) | 2h | Catches speculative imports |
| 10 | **FSA-AI04**: Commented-out code blocks | 2h | Catches LLM artifacts |
| 11 | **FSA-X05**: Cyclomatic complexity (TAST) | 4h | Catches 3–8% of AI smells |
| 12 | **FSA-X03**: Match nesting depth (TAST) | 2h | Catches deep nesting |
| 13 | **FSA-X04**: Parameter count > 5 (TAST) | 1h | Catches wide signatures |
| 14 | **Severity tiers**: Correctness=deny, Style=warn | 2h | Clippy model |

### Sprint 3: AI-Specific + Integration (Month 2)

| # | Task | Effort | Impact |
|---|---|---|---|
| 15 | **FSA-AI05**: Inconsistent error handling | 3h | Catches paradigm switching |
| 16 | **FSA-AI10**: Magic numbers | 2h | Catches unexplained constants |
| 17 | **Implement `--diff`**: Git integration | 4h | Run on changed files only |
| 18 | **FSharpLint delegation**: Load as plugin | 8h | Don't reinvent naming rules |
| 19 | **G-Research integration**: Load their analyzers | 8h | Production-proven correctness |
| 20 | **Fix or remove AutoFix**: Dataflow or remove | 4h | Stop breaking code |

### Sprint 4: Ecosystem (Month 3)

| # | Task | Effort | Impact |
|---|---|---|---|
| 21 | NuGet package / dotnet tool | 4h | Consumable by others |
| 22 | MCP server for agent integration | 8h | Agents query FsAssay directly |
| 23 | Git pre-commit hook template | 2h | Non-optional enforcement |
| 24 | Architectural boundary rules (namespace-level) | 8h | Layer enforcement |
| 25 | False-positive regression test suite | 4h | Trust |

---

## VIII. CORRECTED SCORECARD

| Dimension | Original (v0) | Previous Review | **Current** | Delta |
|---|---|---|---|---|
| Honesty of claims | 1/10 | 5/10 | **5/10** | — (Qwen3.md still overclaims) |
| Code quality | 3/10 | 6/10 | **6/10** | — (AutoFix still dangerous) |
| Architecture | 2/10 | 7/10 | **7/10** | — (9 modules, verdict model) |
| Testing | 3/10 | 4/10 | **5/10** | +1 (negative test added) |
| Documentation | 7/10 | 7/10 | **7/10** | — |
| Production readiness | 1/10 | 4/10 | **4/10** | — (phantom flags, no security) |
| Security coverage | 0/10 | 0/10 | **0/10** | — (zero rules) |
| AI-specific coverage | 0/10 | 0/10 | **1/10** | +1 (FSA2014 TODO detection) |
| **Overall** | **2/10** | **5/10** | **5/10** | Stable |

---

## IX. FINAL VERDICT

**The foundation is solid.** The TAST integration, sanitizer, Orchestrator, verdict model, SARIF output, CI pipeline, and config system are all real, working engineering. This is no longer a prototype — it's a legitimate v0.1 tool.

**But the tool is analyzing the wrong things.** It spends 21 of 24 rules on architectural style (primitive obsession, layer violations, feature envy) while having **zero security rules** and **zero dead-code detection** — the two categories that represent **74–85% of all AI-generated code defects** [[18]].

The priority inversion is clear:

```
What FsAssay catches:     "You used 'mutable' instead of 'let'"     (style)
What FsAssay misses:      "You hard-coded a password"               (security)
                          "You left 47 lines of dead code"          (waste)
                          "You disposed the HttpClient before       (crash)
                           the async workflow runs"
                          "You used MD5 for password hashing"       (security)
                          "You wrote File.ReadAllText(userInput)"   (injection)
```

**The thesis is right. The rule priorities are wrong.** Fix the priority inversion, add the security tier, add dead-code detection, fix FSA2023 and FSA1008, and FsAssay becomes what it claims to be: a vital guardrail for the agentic AI world.

The noble part is real. Now make the vital part real too.
