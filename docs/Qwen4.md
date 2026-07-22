# 🔭 FsAssay in the Agentic AI World: SOTA Gap Analysis

You're right that FsAssay occupies a **genuine and vital niche**. The research is unambiguous: AI-generated code carries **~2 issues per passing task** on average, with **5–8% bugs and ~2% security vulnerabilities** even in code that passes all functional tests [[18]]. The SonarQube study of 4,442 LLM-generated Java solutions found that **functional benchmark performance is NOT a good indicator of code quality or security** [[18]]. And the agentic coding guardrails literature is emphatic: *"If there's a deterministic tool for the job, don't 'prompt' the model to do the tool's work"* [[126]].

FsAssay's thesis — **deterministic, compiler-backed, F#-specific static analysis as a guardrail for AI-generated code** — is exactly what the field needs. F#'s type system is already a powerful first line of defense [[126]], but it needs an analyzer layer on top.

Here is the **complete SOTA landscape** and what FsAssay is still missing.

---

## I. THE SOTA LANDSCAPE: What the Best Tools Catch

### A. Rust Clippy — The Gold Standard (828 lints, 9 categories)

Clippy is the most mature language-specific linter in existence, with **828 lints** organized into 9 severity-graded categories [[89]][[91]]:

| Category | Default Level | Purpose | Clippy Count |
|---|---|---|---|
| **correctness** | `deny` (blocks compilation) | Code is outright wrong | ~50 |
| **suspicious** | `warn` | Code is really sus, probably a bug | ~40 |
| **complexity** | `warn` | Can be simplified | ~200 |
| **perf** | `warn` | Performance improvements | ~80 |
| **style** | `warn` | Idiomatic code | ~250 |
| **pedantic** | `allow` (opt-in) | Power-user strictness | ~150 |
| **restriction** | `allow` (opt-in) | Restricts language features | ~100 |
| **cargo** | `allow` | Cargo.toml improvements | ~20 |
| **nursery** | `allow` | Experimental/buggy | ~30 |

**Key design principle:** Correctness lints are **deny-by-default and abort compilation**. They are *"carefully picked and should be free of false positives"* [[89]]. This is the model FsAssay should follow: **correctness rules block the build; style rules warn**.

### B. FSharpLint — The F# Convention/Smell Layer (~65+ rules)

FSharpLint analyzes code using **both typed and untyped syntax trees** [[1]][[97]]. Its rules fall into three categories:

- **Conventions**: Naming (PascalCase, camelCase, interface prefixes), formatting, indentation
- **Smells**: Logic that poses maintainability problems (e.g., FL0034: unnecessary lambda, FL0065: `List.fold (+) 0` → `List.sum`)
- **Formatting**: Cosmetic standards

Latest release **v0.27.0** (Dec 2025) added `SynchronousFunctionNames` — flagging synchronous functions marked with async prefixes [[104]].

### C. G-Research FSharp Analyzers — Production-Grade TAST Analyzers (10 analyzers)

These are **real-world production analyzers** from G-Research's trading systems [[11]][[64]]:

| Analyzer | What It Catches |
|---|---|
| **VirtualCallAnalyzer** | Virtual method calls on F# collections (List, Set, Map) — suggests F#-idiomatic alternatives |
| **UnionCaseAnalyzer** | Union case naming violations |
| **StringAnalyzer** | Misuse of `String.IndexOf` / `String.LastIndexOf` (should use `String.Contains`, `String.StartsWith`) |
| **TypeAnnotateStringFunctionAnalyzer** | Missing type annotations on string functions |
| **TypedInterpolatedStringsAnalyzer** | Untyped interpolated strings that should be typed |
| **ImmutableCollectionEqualityAnalyzer** | Equality checks on immutable collections that may not behave as expected |
| **LoggingArgFuncNotFullyAppliedAnalyzer** | Partially applied logging argument functions |
| **LoggingTemplateMissingValuesAnalyzer** | Logging templates with missing placeholder values |
| **DisposedBeforeAsyncRunAnalyzer** | 🔴 **CRITICAL**: `IDisposable` disposed before the async workflow that uses it actually runs |
| **JsonSerializerOptionsAnalyzer** | Incorrect `JsonSerializerOptions` usage |

### D. F# Compiler Warnings (FS0001–FS0064+)

The F# compiler itself emits warnings that FsAssay should **never duplicate** but should **complement**:

| Warning | Description |
|---|---|
| FS0025 | Incomplete pattern match |
| FS0044 | Deprecated/obsolete API usage |
| FS0049 | Uppercase variable name |
| FS0052 | Defensive copy (value type mutated) |
| FS0064 | Nullness warnings (F# 9+) |
| FS0104 | `let rec` evaluated out of order |
| FS1189 | Unused `open` |

### E. AI-Generated Code Defect Taxonomy (Empirical Research)

The SonarQube study of 4,442 LLM-generated solutions found these **code smell categories** [[18]]:

| Defect Category | % of Smells | FsAssay Covers? |
|---|---|---|
| **Dead/unused/redundant code** | 14–43% | ❌ NO |
| **Design/framework best-practices** | 12–22% | 🟡 Partial (FSA2016–2030) |
| **Assignment/field/scope visibility** | 11–15% | 🟡 Partial (FSA1001) |
| **Collection/generics/param/type** | 8–14% | ❌ NO |
| **Regex/pattern/string/format** | 5–14% | ❌ NO |
| **Cognitive/computational complexity** | 3–8% | ❌ NO |
| **Control/conditional-logic smell** | 2–5% | ❌ NO |
| **Deprecated/obsolete APIs** | 2–4% | ❌ NO |
| **Naming/style/documentation** | 2–3% | ❌ NO |

And these **vulnerability categories** [[18]]:

| Vulnerability | Severity | FsAssay Covers? |
|---|---|---|
| **Hard-coded credentials** | BLOCKER (60–71%) | ❌ NO |
| **Path traversal & injection** | BLOCKER/CRITICAL | ❌ NO |
| **Cryptography misconfiguration** | CRITICAL | ❌ NO |
| **Certificate validation omissions** | CRITICAL | ❌ NO |
| **XXE (XML External Entity)** | CRITICAL | ❌ NO |
| **JWT signature not verified** | CRITICAL | ❌ NO |

And these **LLM-specific mistake categories** [[84]]:

| Mistake Type | Description | FsAssay Covers? |
|---|---|---|
| **Edge Case blindness** | LLMs overlook corner cases (null, empty, boundary) | ❌ NO |
| **Incorrect Trained Knowledge** | Wrong API usage (e.g., Python `split` ≠ Java `split`) | 🟡 Partial (FSA1002) |
| **Misleading Function Signature** | Function name implies different behavior than spec | ❌ NO |
| **Positional Sensitivity** | LLM misses requirements buried in prompt | ❌ NO (not detectable statically) |

---

## II. THE COMPLETE GAP ANALYSIS: What FsAssay Should Catch

### 🔴 TIER 1: CORRECTNESS (Deny-by-default — blocks the build)

These are **outright wrong** patterns. Clippy's correctness category is the model [[89]].

| ID | Rule | Detection Method | Why AI Gets This Wrong |
|---|---|---|---|
| **FSA-C01** | **`Unchecked.defaultof<_>` in non-interop code** | TAST: `FSharpExprPatterns.DefaultValue` where type is not a P/Invoke struct | LLMs generate this as a "null" substitute. FsAssay has FSA1003 but it's regex-based and flags legitimate interop. |
| **FSA-C02** | **`Option.get` / `.Value` without guard** | TAST: `Call` to `FSharpOption.get` not inside `match`/`if isSome` | LLMs treat `Option` like nullable — call `.Value` directly. Already partially in FSA1002 but needs TAST precision. |
| **FSA-C03** | **`Async.RunSynchronously` in library code** | TAST + project context | Already FSA2008 but regex-based. Must be TAST to avoid false positives in comments. |
| **FSA-C04** | **`IDisposable` disposed before async runs** | TAST: `use`/`using` scope vs `Async.Start`/`Async.RunSynchronously` call site | **G-Research's DisposedBeforeAsyncRunAnalyzer** catches this [[11]]. LLMs generate `use x = new HttpClient()` then `Async.Start` — the client is disposed before the async work runs. This is a **runtime crash**. |
| **FSA-C05** | **Incomplete pattern match on DU with `[<RequireQualifiedAccess>]`** | TAST: `SynMatchExpr` where not all union cases covered | LLMs add new DU cases but forget to update all match expressions. The compiler warns (FS0025) but FsAssay should **deny** this in AI-generated code. |
| **FSA-C06** | **`failwith` / `invalidArg` / `raise` in library public API** | TAST: exception-raising expressions in `let` bindings with public visibility | LLMs use exceptions for control flow. FSA1006 exists but is regex-based. |
| **FSA-C07** | **Infinite recursion without tail call** | TAST: recursive `let rec` where recursive call is not in tail position | LLMs generate recursive functions that stack-overflow on large inputs. |
| **FSA-C08** | **`Seq.length` / `List.length` on potentially infinite sequence** | TAST: `Seq.length` call on `Seq.initInfinite` or `Seq.unfold` | LLMs don't track laziness. |

### 🟠 TIER 2: SUSPICIOUS (Warn-by-default — probably a bug)

| ID | Rule | Detection Method | Why AI Gets This Wrong |
|---|---|---|---|
| **FSA-S01** | **Hard-coded credentials / secrets** | Regex on sanitized source: `password\s*=\s*"`, `apiKey\s*=\s*"`, `secret\s*=\s*"`, connection strings | **60–71% of LLM vulnerabilities are BLOCKER-level hard-coded credentials** [[18]]. This is the #1 security issue. |
| **FSA-S02** | **Path traversal: unsanitized user input in file paths** | TAST: `System.IO.Path.Combine` or `File.ReadAllText` with parameter from function argument | LLMs generate `File.ReadAllText(userInput)` without validation [[18]]. |
| **FSA-S03** | **Swallowed exceptions: `try ... with _ -> ()`** | TAST: `SynExpr.TryWith` where handler body is `SynExpr.Const(())` | LLMs generate empty catch blocks to "handle" errors. |
| **FSA-S04** | **`async { ... }` without `return` or `return!`** | TAST: `SynExpr.ComputationExpr` with `async` CE that has no return | LLMs generate async blocks that silently discard results. |
| **FSA-S05** | **`Task.Result` / `.Wait()` in async context** | TAST: `Call` to `Task.get_Result` or `Task.Wait` inside `async { }` | LLMs mix Task and Async paradigms, causing deadlocks. |
| **FSA-S06** | **`lock` / `Monitor` in F# code** | TAST: `Call` to `System.Threading.Monitor` or `lock` | LLMs generate C#-style locking instead of `MailboxProcessor` or `Agent`. |
| **FSA-S07** | **Mutable static / global state** | TAST: `[<Literal>]` or `static let mutable` | LLMs generate global mutable state for "caching". |
| **FSA-S08** | **`printfn` / `Console.WriteLine` in library code** | TAST: `Call` to `printfn` in non-`[<EntryPoint>]` assembly | Already FSA2012 but regex-based. Must be TAST. |

### 🟡 TIER 3: COMPLEXITY (Warn — can be simplified)

| ID | Rule | Detection Method | Why AI Gets This Wrong |
|---|---|---|---|
| **FSA-X01** | **Unnecessary lambda: `(fun x -> f x)` → `f`** | TAST: `SynExpr.Lambda` where body is `SynExpr.App` of single arg | LLMs generate redundant lambdas. FSharpLint FL0034 catches this [[1]]. |
| **FSA-X02** | **`List.fold (+) 0` → `List.sum`** | TAST: `Call` to `List.fold` with `(+)` and `0` | FSharpLint FL0065 catches this [[1]]. LLMs generate verbose folds. |
| **FSA-X03** | **Nested `match` > 3 levels deep** | TAST: `SynMatchExpr` nesting depth | LLMs generate deeply nested pattern matches instead of using `Result` CEs or `Option` combinators. |
| **FSA-X04** | **Function with > 5 parameters** | TAST: `SynBinding` with > 5 args | LLMs generate wide parameter lists instead of records. |
| **FSA-X05** | **Cyclomatic complexity > threshold** | TAST: count branches in `SynExpr` | LLMs generate complex functions. SonarQube flags this in 3–8% of AI code smells [[18]]. |
| **FSA-X06** | **`if/then/else` chain > 4 branches → `match`** | TAST: nested `SynExpr.IfThenElse` | LLMs generate C#-style if/else chains. |
| **FSA-X07** | **`List.map (fun x -> ...) |> List.filter (fun x -> ...)` → `List.choose`** | TAST: pipeline of map+filter | LLMs generate verbose pipelines. |
| **FSA-X08** | **String concatenation in loop → `StringBuilder` or `String.concat`** | TAST: `+` on strings inside `for`/`while`/recursive loop | LLMs generate O(n²) string building. |

### 🔵 TIER 4: PERFORMANCE (Warn — measurable perf impact)

| ID | Rule | Detection Method | Why AI Gets This Wrong |
|---|---|---|---|
| **FSA-P01** | **`List.append` in loop → `List.collect` or tail-recursive accumulator** | TAST | LLMs generate O(n²) list building. |
| **FSA-P02** | **`Seq.toList` then `List.length` → `Seq.length`** | TAST | LLMs materialize sequences unnecessarily. |
| **FSA-P03** | **`List.rev (List.map f xs)` → `List.map f xs |> List.rev` or `List.mapBack`** | TAST | LLMs generate unnecessary intermediate lists. |
| **FSA-P04** | **Boxing: `int` passed as `obj` parameter** | TAST: implicit boxing conversions | LLMs generate C#-style `object` parameters. |
| **FSA-P05** | **`String.Format` with concatenation → interpolated string** | TAST | LLMs generate verbose string formatting. |

### 🟣 TIER 5: STYLE / IDIOMATIC F# (Warn — readability)

| ID | Rule | Detection Method | Why AI Gets This Wrong |
|---|---|---|---|
| **FSA-T01** | **Naming conventions: PascalCase for types, camelCase for let bindings** | TAST: `SynIdent` | LLMs mix C# and F# naming. FSharpLint covers this [[1]]. |
| **FSA-T02** | **`open` unused** | TAST: `SynModuleDecl.Open` with no references | LLMs generate unnecessary opens. Compiler warns (FS1189) but FsAssay should enforce. |
| **FSA-T03** | **`type` abbreviation for single-constructor DU → record** | TAST | LLMs generate DUs where records suffice. |
| **FSA-T04** | **`member this.X` instead of `member _.X`** | TAST: unused `this` in member | LLMs generate C#-style `this` references. |
| **FSA-T05** | **`new` keyword on F# types** | TAST: `SynExpr.New` on F#-defined types | LLMs generate `new MyType()` instead of `MyType()`. |
| **FSA-T06** | **Semicolons in list/array literals** | AST: `SynExpr.ArrayOrList` with semicolons | LLMs generate `[1; 2; 3]` with inconsistent semicolons. |
| **FSA-T07** | **`fun () ->` instead of named function for top-level** | TAST | LLMs generate anonymous functions where named ones are clearer. |

### ⚫ TIER 6: RESTRICTION (Opt-in — project-specific policy)

| ID | Rule | Detection Method | Why It Matters for AI Code |
|---|---|---|---|
| **FSA-R01** | **No `System.Console` in domain layer** | TAST + project/namespace context | Architectural boundary enforcement [[126]]. |
| **FSA-R02** | **No `System.IO` in domain layer** | TAST + project context | LLMs generate I/O in pure domain code. |
| **FSA-R03** | **No `HttpClient` instantiation (use `IHttpClientFactory`)** | TAST: `SynExpr.New` on `HttpClient` | LLMs generate `new HttpClient()` per request — socket exhaustion. |
| **FSA-R04** | **No `DateTime.Now` (use injected clock)** | TAST: `Call` to `DateTime.Now` | LLMs generate non-testable time dependencies. |
| **FSA-R05** | **No `Guid.NewGuid()` in domain (use injected generator)** | TAST | LLMs generate non-deterministic domain logic. |
| **FSA-R06** | **No `Thread.Sleep` (use `Async.Sleep`)** | TAST | LLMs generate blocking sleeps. |
| **FSA-R07** | **No `Environment.GetEnvironmentVariable` outside config module** | TAST | LLMs scatter config reads. |
| **FSA-R08** | **No `#nowarn` without justification comment** | AST: `SynModuleDecl.HashDirective` | LLMs suppress warnings silently. |

### 🔒 TIER 7: SECURITY (Deny — non-negotiable)

| ID | Rule | Detection Method | Evidence |
|---|---|---|---|
| **FSA-SEC01** | **Hard-coded passwords/keys/secrets** | Regex on sanitized source | 60–71% of LLM vulns are BLOCKER [[18]] |
| **FSA-SEC02** | **SQL injection: string concatenation in SQL queries** | TAST: string concat passed to `SqlCommand` | LLMs generate `sprintf "SELECT * FROM users WHERE id = %s" input` |
| **FSA-SEC03** | **Path traversal: user input in file operations** | TAST | LLMs generate `File.ReadAllText(userInput)` [[18]] |
| **FSA-SEC04** | **Weak cryptography: MD5, SHA1, DES** | TAST: `Call` to `MD5.Create`, `SHA1.Create`, `DES.Create` | LLMs generate deprecated crypto [[18]] |
| **FSA-SEC05** | **Disabled SSL/TLS validation** | TAST: `ServicePointManager.ServerCertificateValidationCallback` set to `true` | LLMs generate `fun _ _ _ -> true` for cert validation [[18]] |
| **FSA-SEC06** | **XXE: XML parser without external entity protection** | TAST: `XmlDocument` or `XDocument.Load` without `DtdProcessing.Prohibit` | LLMs generate default XML parsing [[18]] |
| **FSA-SEC07** | **JWT without signature verification** | TAST: `JwtSecurityToken` without `TokenValidationParameters.ValidateIssuerSigningKey` | LLMs generate JWT parsing without validation [[18]] |

### 🧠 TIER 8: AI-SPECIFIC META-RULES (Novel — no existing tool does this)

This is where FsAssay can be **genuinely novel** — rules that specifically target **LLM generation artifacts**:

| ID | Rule | Detection Method | Why |
|---|---|---|---|
| **FSA-AI01** | **Dead code: unused `let` bindings** | TAST: `SynBinding` with no references | 14–43% of AI code smells [[18]]. LLMs generate helper functions they never call. |
| **FSA-AI02** | **Duplicate code blocks (> 6 lines identical)** | AST: hash-based dedup | LLMs copy-paste patterns instead of abstracting. |
| **FSA-AI03** | **Phantom imports: `open` for modules never used** | TAST | LLMs generate speculative imports. |
| **FSA-AI04** | **Commented-out code blocks (> 3 lines)** | Sanitizer: detect `//` blocks that parse as valid F# | LLMs leave "alternative implementations" commented out. |
| **FSA-AI05** | **Inconsistent error handling: mix of `Result` and `try/with` in same module** | TAST | LLMs switch paradigms mid-file. |
| **FSA-AI06** | **`TODO` / `HACK` / `FIXME` / `PLACEHOLDER` comments** | Regex on sanitized source | Already FSA2014 but should be elevated for AI code. LLMs leave placeholders. |
| **FSA-AI07** | **Overly generic type parameters: `'a` used > 3 times in one function** | TAST | LLMs over-generalize. |
| **FSA-AI08** | **Missing XML doc on public API** | TAST: `SynBinding` with `public` visibility and no `///` doc | LLMs generate undocumented public APIs. |
| **FSA-AI09** | **Test assertion without message: `Assert.True(x)` → `Assert.True(x, "description")`** | TAST | LLMs generate assertions that fail silently. |
| **FSA-AI10** | **Magic numbers: numeric literals > 1 in non-test code** | TAST: `SynExpr.Const(Int)` not in `[<Literal>]` or test file | LLMs generate unexplained constants. |

---

## III. THE AGENTIC LOOP INTEGRATION: How FsAssay Should Fit

The guardrails literature is clear: **static analysis must run INSIDE the agentic loop, not after PR submission** [[126]][[125]].

```
┌─────────────────────────────────────────────────────────┐
│                    AGENTIC LOOP                         │
│                                                         │
│  1. Agent generates/modifies F# code                    │
│  2. ┌──────────────────────────────────────────┐        │
│     │  FsAssay runs on DIFF only (not full repo)│        │
│     │  • Correctness rules → BLOCK (exit 1)     │        │
│     │  • Security rules → BLOCK (exit 1)        │        │
│     │  • Suspicious rules → WARN (exit 0)       │        │
│     │  • Complexity/Style → INFO (exit 0)       │        │
│     └──────────────────────────────────────────┘        │
│  3. If BLOCKED: agent receives precise violation list   │
│     with file, line, column, rule ID, fix suggestion    │
│  4. Agent fixes violations                              │
│  5. FsAssay re-runs on diff                             │
│  6. Repeat until green                                  │
│  7. ONLY THEN: human reviews                            │
│                                                         │
│  Output format: SARIF (machine-readable for agents)     │
│  Signal-to-noise: suppress green output, emit only      │
│  violations (reduce token burn) [[126]]                 │
└─────────────────────────────────────────────────────────┘
```

### Critical integration requirements:

1. **Run on diff, not full repo** [[126]]: `fsassay --diff HEAD~1` — only analyze changed files. The `--diff` flag exists in Argu but is **not implemented**.

2. **Machine-readable output for agents** [[126]]: SARIF is correct. But also emit a **minimal JSON** mode: `[{"file":"X.fs","line":42,"rule":"FSA-C04","message":"..."}]` — one line per violation, no decoration.

3. **Deterministic, non-optional hooks** [[125]]: FsAssay should be invokable as a **git pre-commit hook** and a **CI gate**. Not "prompt the agent to run the linter" — make it impossible to commit without passing.

4. **Signal over noise** [[126]]: When everything passes, output **nothing** (exit 0, empty stdout). When violations exist, output **only violations**. No "Scan complete!" banners. No rate cards. Agents don't need encouragement.

5. **Architectural boundary enforcement** [[126]]: FsAssay should support **namespace/project-level rules**: "This namespace must not reference System.IO." This is the F# equivalent of ArchUnit.

---

## IV. PRIORITY ROADMAP: What To Build First

Based on the **frequency and severity** of AI-generated defects [[18]][[84]]:

### Sprint 1: Security + Correctness (Highest ROI)
| Priority | Rule | Rationale |
|---|---|---|
| 1 | **FSA-SEC01**: Hard-coded secrets | 60–71% of LLM vulns [[18]] |
| 2 | **FSA-C04**: Disposed before async | Runtime crash. G-Research proves this is real [[11]] |
| 3 | **FSA-C02**: `Option.get` without guard (TAST upgrade) | Most common F# runtime error in AI code |
| 4 | **FSA-S03**: Swallowed exceptions | LLMs generate empty catch blocks |
| 5 | **FSA-SEC04**: Weak crypto | LLMs use MD5/SHA1 from training data |

### Sprint 2: Dead Code + Complexity (Highest Volume)
| Priority | Rule | Rationale |
|---|---|---|
| 6 | **FSA-AI01**: Dead/unused code | 14–43% of AI code smells [[18]] |
| 7 | **FSA-X01**: Unnecessary lambda | Most common F# idiom violation |
| 8 | **FSA-X05**: Cyclomatic complexity | 3–8% of AI code smells [[18]] |
| 9 | **FSA-AI04**: Commented-out code | LLM artifact |
| 10 | **FSA-T01**: Naming conventions | FSharpLint delegation [[1]] |

### Sprint 3: Architectural + AI-Specific
| Priority | Rule | Rationale |
|---|---|---|
| 11 | **FSA-R01/R02**: Layer boundary enforcement | Architectural drift [[126]] |
| 12 | **FSA-AI05**: Inconsistent error handling | LLM paradigm switching |
| 13 | **FSA-S05**: `Task.Result` in async | Deadlock risk |
| 14 | **FSA-AI10**: Magic numbers | LLM artifact |
| 15 | **FSA-X03**: Nested match depth | LLM generates deep nesting |

### Sprint 4: Integration + Ecosystem
| Priority | Task | Rationale |
|---|---|---|
| 16 | **Implement `--diff`** | Run on changed files only [[126]] |
| 17 | **FSharpLint delegation** | Don't reinvent naming/formatting rules [[1]] |
| 18 | **G-Research analyzer integration** | Load their analyzers as plugins [[11]] |
| 19 | **MCP server** | Let agents query FsAssay directly [[126]] |
| 20 | **Git hook + CI template** | Make it non-optional [[125]] |

---

## V. WHAT MAKES FsAssay GENUINELY NOBLE

The user's instinct is correct. Here's why this matters more than a generic linter:

1. **F# is the ideal AI guardrail language.** Its type system already eliminates entire bug categories at compile time [[126]]. FsAssay adds the layer the compiler can't reach: semantic anti-patterns, architectural boundaries, security patterns.

2. **The "deterministic tool over hopeful prompting" principle** [[126]] is FsAssay's entire thesis. Don't prompt the LLM to "avoid mutable state." Make it **impossible to commit** mutable state.

3. **The SonarQube study proves functional correctness ≠ quality** [[18]]. Code that passes all tests still has 2.11 issues per task. FsAssay catches what tests miss.

4. **The agentic loop needs fast, precise, machine-readable feedback** [[126]]. FsAssay's SARIF output, exit codes, and per-violation locations are the right interface. But it needs to be **faster** (diff-only) and **quieter** (no output when green).

5. **No existing F# tool combines security + correctness + complexity + AI-specific rules in one pass.** FSharpLint does conventions/smells [[1]]. G-Research does production correctness [[11]]. The compiler does type safety. **Nobody does all four plus AI-specific meta-rules.** That's FsAssay's gap.

---

## VI. THE HONEST CAVEAT

The vision is right. The execution is at **~15% of SOTA**. The 24 current rules cover perhaps **10% of the categories** that Clippy (828 lints) [[89]], FSharpLint (~65 rules) [[1]], and G-Research (10 production analyzers) [[11]] collectively address. The security tier — which represents **60–71% of LLM vulnerability severity** [[18]] — is **completely absent**.

The path from here to SOTA is not "add more regex rules." It's:
- **TAST-first** for every rule that touches symbols, types, or call graphs
- **Delegate** naming/formatting to FSharpLint [[1]]
- **Integrate** G-Research analyzers as plugins [[11]]
- **Build the security tier** (hard-coded secrets, path traversal, weak crypto, XXE, JWT)
- **Build the AI-specific tier** (dead code, commented-out blocks, paradigm inconsistency)
- **Make it fast** (diff-only, incremental)
- **Make it quiet** (no output when green, SARIF-only for agents)

The thesis is sound. The need is proven. The gap is enormous. **Build it.**
