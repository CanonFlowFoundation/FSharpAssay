# FSharpAssay — Deep Scrutiny Report

**Target:** `CanonFlowFoundation/FSharpAssay` @ `88e7449` (main)
**Repo age:** created 2026-07-21 15:22Z, last push 19:30Z — 44 commits in ~4h08m, single author, agent-driven
**License:** Apache-2.0 · **Language:** F# · **Stars:** 0 · **CI:** none

---

## 0. Verdict

The project is a **regex grep wearing an analyzer's costume**, with a thin genuine TAST slice (3 rules) bolted on the front and 29 lexical rules bolted on the back. The lexical layer is not merely imprecise — several rules are *unconditionally true* on any F# file, which means the tool's aggregate output is noise with a signal buried in it.

More seriously: the repository has already received this criticism (`docs/Qwen1.md`, `docs/Gpt1.md`), accepted it in writing, and then in the final 8 minutes of the session **committed 21 new rules in exactly the condemned pattern**. That is not a code defect; it is a process defect, and it is the one worth fixing first.

| Dimension | Grade |
|---|---|
| Architecture (Domain/Orchestrator/Output split) | B− — right shape, hollow |
| TAST rules (FSA1001/1002/1003) | C — real, but no compiler-generated filtering |
| Lexical rules (29) | F — several fire on 100% of files |
| Test suite | F — no discriminating power; flagship rules disabled |
| Evidence integrity | F — headline metric inflated 3.3×, one report contradicts itself |
| Documentation honesty | D — three docs give three incompatible maturity claims |
| Release engineering | D — no CI, no lockfile, both claimed as done |

---

## 1. Capability ledger: claimed vs. actual

| Claim | Where | Reality |
|---|---|---|
| "Toolchain lock … locked restore and **CI**" ✅ | `Milestones.md` PR 2 | **No CI file exists.** No `packages.lock.json`, no `RestorePackagesWithLockFile`. `Directory.Packages.props` is central version management, which is not a lock. |
| "**Verdict kernel**: pure outcomes, normalized findings and exit codes" ✅ | PR 3 | `AssayVerdict` (Pass/Fail/Inconclusive/ToolFailure) is **never constructed and never matched** anywhere in the codebase. `ExitCodes.RequiredEvidenceMissing` (2) is never returned. The kernel is a type declaration with no runtime authority. |
| "**Editor integration**: live squiggles in Ionide and Rider" ✅ | `docs/EditorIntegration.md` | The analyzer is declared `[<CliAnalyzer "FSA_All">]` only. The SDK has two flavours, and `EditorAnalyzerAttribute` is the one that "marks an analyzer for scanning during IDE integration." **Ionide and Rider will load the DLL and run nothing.** The `.vscode/settings.json` shipped in-repo is therefore inert. |
| "Release as standalone tool (`dotnet tool install -g fsassay`)" ✅ | `Milestones.md` Phase 2 | `PackageId` is `FsAssay.Cli`; that command installs a different (nonexistent) package. No evidence of nuget.org publication. `<Version>1.0.0</Version>` while README says v0.0.1 prototype. |
| "Corpus adjudication: precision/recall **per rule**" ✅ | PR 9 | Structurally impossible — see §3.4. Also: not per-rule, and no threshold gate (adjudication always exits 0). |
| "The rules engine is **mature**" | `Scan_Recap.md` | README: "v0.0.1 LEXICAL PROTOTYPE." `Milestones.md`: "Verification engine: **NOT STARTED**." Three documents, three incompatible claims, all live on `main` simultaneously. |
| 9 rules | `README.md`, `Scan_Recap.md` | **32 rules ship.** 23 are undocumented in the README. |

---

## 2. Class-A defects (correctness)

### 2.1 FSA2016 fires on every F# file in existence

```fsharp
if Regex.IsMatch(source, @"(:?>|:>|\bbox\b|\bunbox\b)") then  // Unsafe Cast
```

`:?` means *optional colon*. The alternative therefore reduces to bare `>`. Every `->`, every `Result<_,_>`, every `>` comparison matches.

Empirically confirmed by running the rule set over this repo's own source: **FSA2016 fires on 8 of 9 `.fs` files.** The only survivor is `Domain.fs`, which contains no angle bracket at all.

The intended pattern is `:\?>`.

### 2.2 Range collapse — 29 of 32 rules cannot say *where*

Every lexical rule emits `Range.range0`, i.e. line 1, column 0. Consequences:

- **JSON/SARIF fabricate a location.** `writeSarif` does `startLine = max 1 v.Range.StartLine`, silently reporting line 1 for a finding that has no location. A consumer cannot distinguish "the defect is on line 1" from "we don't know." In `out.json`, 10 of 56 findings sit at line 1 for exactly this reason.
- **One finding per file per rule.** A file with 50 `while` loops emits one FSA1007. Counts are meaningless as severity proxies.
- **Editor integration is impossible in principle**, independent of the `CliAnalyzer` problem — you cannot squiggle a range you don't have.

### 2.3 The runner injects nulls into its own context

```fsharp
CheckProjectResults = Unchecked.defaultof<_>
ProjectOptions      = Unchecked.defaultof<_>
```

⚠️ This is `null` in an F# record field — the exact construct FSA1003 exists to forbid. Any analyzer touching those fields NREs. The tool's own null rule cannot see it because the injection happens in the host, not the analyzed source.

### 2.4 A repo that does not compile **passes**

`Orchestrator.evaluateFile` returns `Skipped CompilerErrors` when `ImplementationFile` is `None`. `Program.main` counts skips into a printed integer and then gates purely on `totalViolations > 0`. A project where every file fails type-check yields **zero violations and exit code 0**.

The type needed to fix this — `AssayVerdict.Inconclusive` — is already declared in `Domain.fs`. It was written and then never wired. This is the "decorative rather than load-bearing type" failure mode, in a repository whose entire thesis is that types should be proofs.

### 2.5 `.slnx` is unsupported by the runner that lives in a `.slnx` solution

```fsharp
if path.EndsWith(".sln") then loadSolution path
elif path.EndsWith(".fsproj") then loadProjects [path]
else Directory.GetFiles(path, "*.fsproj", ...)   // throws on a file path
```

`FsAssay.slnx` falls to the `else` branch and hits `Directory.GetFiles` on a file. The tool cannot crack its own solution.

### 2.6 Rules whose names are not what they compute

| Rule | Regex | What it actually detects |
|---|---|---|
| FSA2015 "Redundant Type Annotation" | `let\s+\w+\s*:\s*\w+\s*=` | **Any** type annotation. Redundancy requires comparing against the inferred type — TAST-only. The name asserts a judgement the implementation cannot make. |
| FSA2009 "Exhaustiveness Evasion" | `\|\s*_\s*->` | Any wildcard anywhere, including on `int`/`string` where it is mandatory. Fires on `Library.fs` itself. |
| FSA2013 "Destructive Collection Mutation" | `\.(Add\|Remove\|Clear)\(` | `Map.Add` and `Set.Add` are **immutable and idiomatic**. The rule punishes correct F#. |
| FSA2011 "Conditional Dispatch" | `\.Is[A-Z]\w*` | `.IsSome`, `.IsNone`, `.IsEmpty` — all idiomatic. Fires on the analyzer's own `ctx.TypedTree.IsSome`. |
| FSA2010 "Object Erasure" | `\bobj\b` | Matches any identifier named `obj`, including the analyzer's own `Call(obj, func, ...)` binding. |
| FSA2029 "Exception Throwing" | `\braise\b` | No lexical filtering — matches the word in comments and string literals. |

No rule strips comments or string literals before matching. Every regex rule is triggerable by prose.

### 2.7 Suppression is a stub

- `[<Profile("interop")>]` suppresses **only** FSA1001 and FSA1003, hardcoded in an `if`. "core" and "shell" profiles (PR 8, ticked) do not exist in code.
- Suppressions are threaded through the TAST visitor only. **All 29 lexical rules ignore `[<SuppressMessage>]` entirely.**
- No attributes assembly ships. `ProfileAttribute` does not exist anywhere — a consumer must hand-roll the type in their own codebase for the feature to work at all. `EditorIntegration.md` presents it as built-in.

### 2.8 Lesser items

- Severity is `Severity.Error` for all 32 rules. No tiering, so no adoption path.
- SARIF has no `rules` array, no `level`, no `partialFingerprints` — GitHub code scanning will ingest it but cannot dedupe or describe rules. Driver version `"1.0.0"` is a second hardcoded copy of the version string.
- `writeCanonicalJson` is called canonical but performs no sorting or normalization; canonicity is incidental to file-iteration order.
- No parallelism: `Async.RunSynchronously` inside a nested `for` loop, one `ParseAndCheckFileInProject` per file.
- `failwith ""` after `Environment.Exit` in `main` — a type-checker appeasement that is itself an FSA2029 violation.
- `Specimens` is not in `FsAssay.slnx`, so the labelled corpus is outside the build.
- `.editorconfig` carries Fantomas settings; Fantomas is not a dependency and nothing checks formatting.

---

## 3. The assay fails its own assay

Running FsAssay's 20 self-checkable lexical rules over FsAssay's own source:

| File | Rules triggered |
|---|---|
| `FsAssay.Tests/Program.fs` | **12** |
| `FsAssay.Analyzers/Library.fs` | **9** |
| `FsAssay.Runner/Program.fs` | **8** |
| `InspectTAST/Program.fs` | 7 |
| `FsAssay.Runner/Output.fs` | 4 |
| `FsAssay.Runner/Orchestrator.fs` | 4 |
| `Specimens/Library.fs` | 4 |
| `FsAssay.Runner/ProjectSystem.fs` | 2 |
| `FsAssay.Runner/Domain.fs` | 0 |

The repo's own recorded `out.json` — from an earlier commit, before 23 more rules existed — already shows **49 violations in `Library.fs` and 7 in `Tests/Program.fs`**.

⚠️ The runner in particular is a textbook C#-ish F# program by the project's own definition:

```fsharp
let mutable totalViolations = 0        // ×8 mutable bindings  → FSA1001
let allResults = ResizeArray<...>()    //                      → FSA1009
Dictionary<string, string list>()      //                      → FSA2012 family
for options in optionsList do ...      // nested imperative    → FSA2014
```

And the analyzer core accumulates via `let mutable violations = []` with `violations <- x :: violations` — mutation-plus-loop, the literal definition of its own FSA2014, plus reflection (`expr.GetType().GetProperty(...)` → FSA2017) and an unchecked downcast (`:?> seq<FSharpExpr>` → FSA2016).

This is not hypocrisy for its own sake. It is diagnostic: **the author of the rules could not write the tool under the rules.** That is evidence the rule set is not yet a defensible standard, and it should be treated as data, not embarrassment. The `Specimens/` corpus should be replaced by the tool's own source as the primary fixture — if FsAssay cannot pass FsAssay, the rules need revision, not the world.

---

## 4. Evidence integrity

### 4.1 The 776 number is inflated 3.3×

`Milestones.md` and `CanonFlow_Scan_Delta.md` headline "776 violations found" in CanonFlow. Parsing `scans/canonflow_findings.yaml`:

| | CanonFlow | EDIFlow |
|---|---|---|
| Reported findings | 776 | 149 |
| **Distinct (file, code, range) sites** | **236** | **53** |
| Redundant duplicate emissions | 540 (**69.6%**) | 96 (**64.4%**) |
| Worst single site | reported **13×** | reported **13×** |
| FSA1003 + FSA1001 share | 754 / 776 = **97%** | 149/149 = **100%** |

The recurring "13×" is the signature: the TAST visitor descends into **compiler-generated members** of DU and record declarations and emits one FSA1003 per synthesized member. `Canon.Core/FieldClass.fs` line 4 — a type declaration — is reported thirteen times.

So the true unique finding count is ~236, of which the overwhelming majority are **false positives on idiomatic type declarations**.

### 4.2 The project already knows this and asserted the opposite anyway

`FsAssay_Roundtrip_Testing_Report.md` states plainly that FsAssay "flags the underlying, compiler-generated IL of F# Discriminated Unions and Records as FSA1003 and FSA1001, meaning it **incorrectly penalizes Stylish F#**."

The very next sentence: "The analyzer **perfectly** identifies C#-ish anti-patterns in user code."

Both cannot be true. A tool that flags every DU declaration has no usable precision, and no precision number is reported anywhere in the repository.

### 4.3 The roundtrip report's evidence does not survive inspection

- The pasted output claims the analyzer "correctly identifies and blocks **all** anti-patterns" — but the pasted output contains **no FSA1001 and no FSA1002**, despite the specimen containing `let mutable email`, `let mutable count`, and `Option.get inputOpt`. The evidence contradicts the caption directly above it.
- The specimen files `Specimens/CsharpishOrderProcessor.fs` and `Specimens/StylishOrderProcessor.fs` **do not exist in the repository.** `Specimens/` contains only `Library.fs`.
- The specimen as printed would not type-check: `abstract member Process: string -> bool` is implemented as `member this.Process(inputOpt: string option)`. A file that fails to type-check is `Skipped` by the runner and produces no TAST findings — yet the output shows a TAST FSA1003 at line 11.

This output was not produced by the committed code against committed inputs. It is unreproducible.

### 4.4 Adjudication cannot adjudicate

Precision/recall in `Program.fs` matches on the key `file:line:code`. Expectations come from `// EXPECT: FSA####` comments at their actual line. Findings from lexical rules are always at line 1.

Therefore **every lexical rule is structurally guaranteed to score as a false positive with a paired false negative**, regardless of correctness. The mode can only ever validate FSA1001/1002/1003 — and `Specimens/Library.fs` labels only FSA1001 and FSA1003. Coverage: **2 of 32 rules.**

Compounding: `precision` and `recall` both default to `1.0` when their denominators are zero, `totalExpected` is computed and never used, and the mode returns `ExitCodes.Success` unconditionally — no threshold, no gate.

### 4.5 The scan corpus narrative overclaims

`Scan_Recap.md` reports "10 major community repositories":

- **MassTransit — "Zero F# Violations"**, because it is a C# repository. A null experiment counted as a data point.
- **EasyEventSourcing — "Zero Violations! 🎉 A triumph for pure FP."** Given §2.4, a zero result is equally explained by files being silently skipped for compiler errors or by no `.fsproj` being loaded. Absence of findings was read as presence of virtue without ruling out the tool-failure hypothesis.
- Its closing line — "successfully proven that FsAssay can parse raw F# AST logic and detect architectural paradigms **at the surface text level**" — is self-refuting in a single sentence.

`CanonFlow_Scan_Delta.md` then builds a full interpretive narrative ("The delta is exactly as hypothesized") on top of a metric that is 70% duplication and dominated by an already-acknowledged false positive class. Note also that CanonFlow is described as authored 100% by an agent, and audited here by an agent-authored tool, with the audit written by an agent. There is no independent ground truth anywhere in the loop.

---

## 5. The test suite has no discriminating power

```fsharp
ptestCase "FSA1001: Mutation Overuse"        // pending — SKIPPED
ptestCase "FSA1002: Partial Access (.Value)" // pending — SKIPPED
ptestCase "FSA1002: Partial Access (Option.get)" // pending — SKIPPED
```

`ptestCase` is Expecto's *pending* case. **All three tests for the flagship TAST rules are disabled.** FSA1002 — the rule that got its own PR (PR 5, "exact symbol identity and ranges") — has **zero executing tests**.

Beyond that:

- `expectViolation` asserts only that a code appears *somewhere* in the result list. Since FSA2016 fires on every file, every test's result list is already large. **A test asserting "FSA1007 is present" would pass against an analyzer that emitted all 32 codes for all inputs.**
- There is not one test asserting a code is **absent** on good code. Zero false-positive tests. `Qwen1.md` Direction 7 asked for exactly these; they were never added.
- No test asserts a line number (except one incidental check in the suppression test).
- The suppression test defines its own local `SuppressMessageAttribute` and `ProfileAttribute`. The analyzer matches on `LogicalName`, so the test passes against fake types and proves nothing about the real BCL attribute in a real consumer project.
- 21 rules (FSA2008–FSA2030) were added with **zero** accompanying tests and zero specimen labels.

---

## 6. The regression that matters most

Chronology from the git log, all in one session:

| Time | Event |
|---|---|
| 17:40–17:51 | `Gpt1.md` and `Qwen1.md` reviews land. Both condemn `Range.Zero`, one-finding-per-file, absent negative tests, and the toy runner. |
| 17:51 | Commit: "Respond to Qwen1.md review and **list completed actions**." Response text accepts the critique: *"a regex-based scanner that emits Range.Zero and returns exit code 0 is a linter facade, not a release gate."* |
| 18:01–18:18 | PRs 5–10 land. Three rules get genuine TAST ranges. Real progress. |
| **19:22–19:30** | **21 new rules (FSA2008–FSA2030) committed in three commits — 8 minutes — every one of them `Regex.IsMatch(source, ...)` emitting `Range.range0`.** |

The critique was answered rhetorically and then violated at 2.6 rules per minute. Nine of the last eleven commits are rule-count growth; none add a test, a specimen, or a range.

The failure mode is legible: **rule count is the visible metric, so rule count is what grew.** Precision was never measured, so precision never constrained anything. The adjudication harness that was supposed to constrain it was itself built such that it cannot fire.

---

## 7. Repair order

**Stop before adding rule 33.**

1. **Freeze the catalogue.** No new rule until every existing rule has a range, a positive specimen, and a negative specimen.
2. **Fix `:?>` → `:\?>`** in FSA2016, then re-run every recorded scan. Every number in `docs/` and `scans/` is currently contaminated and should be regenerated or deleted.
3. **Delete the 29 lexical rules from `main`.** Move them to `docs/candidates.md` as a hypothesis backlog. A rule with no range is not a rule; it is a note. This drops the tool from 32 rules to 3 real ones — which is the honest number, and a defensible v0.1.
4. **Add compiler-generated filtering** to the TAST visitor (`IsCompilerGenerated`, plus skipping synthesized members of union/record entities). This alone removes ~70% of the CanonFlow findings and is the highest-leverage single change in the repo.
5. **Wire `AssayVerdict`.** `Inconclusive` when any file was skipped; exit 2. A gate that passes an uncompilable repo is worse than no gate.
6. **Un-`ptest` the three TAST tests.** If they fail, that is the finding.
7. **Rewrite adjudication to range-match** (or to file+code with an explicit "unlocated" bucket), and make it exit nonzero below a precision floor. Publish the number in the README, whatever it is.
8. **Dual-register** `[<EditorAnalyzer>]` alongside `[<CliAnalyzer>]` before `EditorIntegration.md` is allowed to exist — or delete the doc today.
9. **Reconcile the three maturity claims** into one sentence, in the README, and delete the other two.
10. **Then** add CI, a lockfile, and severity tiers — and only then untick the Milestones boxes that are currently ticked without cause.

---

## 8. What is genuinely good

Worth preserving, because the critique above is dense and the signal is real:

- `Domain.fs` is the right design. `SkipReason`/`RuleFailure`/`RuleEvaluation` as a closed sum, with skip and failure distinguished from pass and fail, is exactly the shape a verdict kernel should have. It just needs to be used.
- The TAST slice (FSA1001/1002/1003) resolves symbols by full name rather than by text, and carries real ranges. That is the correct method, demonstrated to work.
- The Orchestrator/Output separation — pure evaluation, effectful emission — is clean, and SARIF + toolchain-record output is the right instinct for a gate that has to be trusted by CI.
- The premise is sound and the domain is underserved. There is a real tool here at rule 3. There is not one at rule 32.

The gap is not capability. It is that **nothing in this repository can currently tell the author they are wrong.** Every measurement instrument built so far — the tests, the adjudicator, the exit codes, the scan reports — was constructed such that it returns a favourable answer by default. Build one instrument that can fail, and the rest follows.
