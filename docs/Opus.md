# FSharpAssay — Deep Scrutiny from the CFF Standpoint

**Repo:** `CanonFlowFoundation/FSharpAssay` @ `61e31ac`
**Analyzer core:** `FsAssay.Analyzers/Library.fs` (244 LOC) — the whole tool lives here.
**Verdict:** **C‑ / "compiles, doesn't hold."** A genuine TAST skeleton wrapped in ~85% regex theater, with an evidence layer that fails its own integrity standard. Good demo, not yet an examiner.

---

## 0. What is actually sound (credit first)

- **The mission is right.** *Compiles ≠ admissible*; "a Skill is the textbook, FsAssay is the examiner"; compiler‑backed design critic for agents and humans. The concept is valuable and well‑articulated in the README.
- **The TAST scaffold is the correct architecture.** `visitExpr` / `visitDecl` over `FSharpImplementationFileDeclaration` + `FSharpExprPatterns.Call` with `func.FullName` matching is exactly how a real analyzer should work. For the five rules that use it (FSA1001 mutable, FSA1002 partial access, FSA1003 null, FSA1101 blocking, FSA1401 async‑start), you get **real ranges** and **semantic precision**.
- **Suppression model has the right shape** — `[<SuppressMessage>]` + `[<Profile>]` attributes with profile‑gated cores/boundaries is the correct policy design.

Everything below is about the gap between that skeleton and the mission it claims to serve.

---

## 1. The CFF‑law indictment (the spine)

Judged against your own constitution‑first, law‑verified discipline, FsAssay violates the four meta‑laws it should exemplify:

### Law 1 — Parse, don't validate → **inverted**
The tool that preaches this is built on `source.Contains("...")` and `Regex.IsMatch(source, ...)`. It never parses its subject into a typed representation; it runs string predicates over raw source. ~22 of 26 rules are stringly‑typed validation — the precise anti‑pattern the tool exists to punish. A CFF‑grade analyzer parses source → typed `Finding` carrying a mandatory proof‑of‑location (`Range`), never a `bool` over a text blob.

### Law 2 — Types as proofs / evidence integrity → **broken**
- **`Range.range0` is a null proof.** ~22 rules emit findings at line 1, col 0. A finding without a location is an assertion without a proof. The type system *allows* `range0`, so the tool ships proofs that prove nothing. `Range` should be non‑optional and non‑zero by construction (a refined `Located<Finding>`), making "finding without location" unrepresentable.
- **Committed evidence ≠ committed code.** `out.sarif` still emits an entire `FSA2xxx` rule family (2014, 2016, 2017, 2020, 2023, 2024, 2026) that **does not exist anywhere in `Library.fs`**. It was generated at `1ecb129` (03:41) against a since‑deleted ruleset; `Library.fs` moved on at `c7d38c2` (08:16). The committed VerdictEnvelope does not correspond to its artifact.
- **The specimen suite tests a vanished ruleset.** `Specimens/Section*.fs` carry `// EXPECT: FSA2019/2023/2024/2040` markers for rules the current analyzer can no longer emit. Dead expectations that can never be satisfied.

### Law 3 — Total functions / Result over exceptions → **violated**
The entire TAST walk is wrapped in `try ctx.TypedTree.Value.Declarations |> List.iter … with _ -> ()`. If the semantic layer throws, the tool **silently degrades to regex** and reports anyway, with zero signal that the precise layer failed. That is a non‑total, dishonest function: it returns "results" that are secretly the fallback. CFF demands `Result<Findings, AnalyzerError>` with the degradation made explicit in the envelope.

### Law 4 — Constitution‑first → **absent**
Rules are ad‑hoc substring checks accreted commit‑by‑commit ("Qwen4 SOTA", "Qwen5 SOTA"), not derived from a single stated law set. There is no source of truth mapping *law → detector → evidence class*. Compare CanonFlow, where schema‑as‑axiom derives every downstream theorem. Here, the "constitution" is scattered across 26 `if source.Contains` branches with no shared algebra.

---

## 2. Code‑level defects (concrete)

### 2a. Whole‑file substring rules — structural false positives
These fire if a substring appears **anywhere** in the file (comment, string literal, unrelated identifier), and report at `range0`:

| Rule | Trigger | Why it's broken |
|---|---|---|
| `FSA-C06` | `"let "` **AND** (`"failwith"` OR `"raise "`) | Every F# file has `"let "`. Reduces to "mentions failwith anywhere." No publicness check, no function scope. Near‑guaranteed FP. |
| `FSA-C07` | `"let rec "` **AND** (`"+ "` OR `"1 +"`) | Zero recursion analysis. Any `let rec` in a file that also contains `a + b` → "non‑tail recursion." Coin flip. |
| `FSA-C05` | `"match "` **AND** `"A -> 1"` **AND NOT** `"B ->"` | Hard‑coded to one specimen's literal text. A fixture detector, not a rule. Never fires on real code. |
| `FSA1005` | `let\s+is[A-Z]…` OR `"isValid"` | Matches `isEmpty`, `isSome`, `isNone` — idiomatic total F#. Fires on nearly every real file. |
| `FSA1002` (fallback) | `\.Value\b` OR `"List.head"`/`"Seq.head"` | `.Value` matches `KeyValuePair.Value`, `Lazy.Value`, `Nullable.Value`, active‑pattern results. Over‑broad. |
| `FSA1101/FSA-C03/FSA-S05` | `".Result"` / `".Wait()"` | `.Result` substring matches `validationResult`, `.ResultCode`, any field named `Result`. On a Result‑centric codebase this fires constantly — on exactly the philosophy the tool blesses. |
| `FSA-S01` | `(?i)(password\|secret\|apiKey…)` | Matches the word "password" in a doc comment or a UI label. |
| `FSA-S04` | `"async {"` **AND NOT** `"return"` | `"return"` substring matches `returnValue`/`returned`; a legit `async { … do! }` with no return is flagged. Both over‑ and under‑inclusive. |

### 2b. Triple‑counting one issue as three codes
A single `.Result` produces **FSA1101 + FSA‑C03 + FSA‑S05**; a single `Option.get` produces **FSA1002 + FSA‑C02**. Separate codes for the same underlying defect, all at `range0`, inflating the finding count 2–3× per site.

### 2c. Profile scoping via substring = file‑global escape hatch
`hasProfile p` = `source.Contains("[<Profile(\"interop\")>]")`. One profile attribute **anywhere** in the file suppresses the rule for the **entire file**. That is not scoping; it's a blanket bypass triggered by text presence. Real scoping must be symbol‑ranged (attribute on *this* declaration suppresses *its* subtree only).

### 2d. Reflection fallback + swallow = silent blind spots
The `| _ ->` catch‑all does `expr.GetType().GetProperty("ImmediateSubExpressions")` via reflection. When that property is absent/null, entire subtrees are skipped silently. Combined with the outer `with _ -> ()`, the semantic layer can fail partially or fully and nobody knows. This is also the mechanism behind the FSA1003 duplication (§3).

### 2e. Dogfooding failure ⚠️
The analyzer's own source is a catalogue of what it bans:
- `mutable` ×5 (its own FSA1001), `let mutable violations = []` + `violations <- … :: violations` — imperative accumulation instead of `fold`.
- `:?>` downcasts ×4, `Unchecked.defaultof` (FSA‑C01, in the test harness `CliContext`), `isNull`, reflection, `with _ -> ()` (its own FSA‑S03 swallow).
Run FsAssay on FsAssay and it fails itself — then swallows the exception.

---

## 3. Evidence‑integrity failure on your own CanonFlow scan

Computed directly from `scans/canonflow_findings.yaml`:

- **Headline "776 violations" → 236 unique** `(file, code, exact‑range)` tuples. **3.29× inflation.**
- **Per‑finding duplication up to 13×:** `FieldClass.fs:4`, `Lattice.fs:4 / 29 / 39 / 44 / 58` each emitted **thirteen times** — the reflection walk re‑visiting the same `Const(null)`/`DefaultValue` node through multiple paths, with no dedup for FSA1003.
- **554 / 776 (71%) are FSA1003 "null reference"** on a codebase that is Option/Result by construction. These are **phantom nulls**: the TAST `Const(obj, ty)` and `DefaultValue` patterns firing on the compiler's internal representation of DU tags and `option` cases — *not* source‑level `null`. (Hypothesis to confirm by eyeballing `FieldClass.fs:4` / `Lattice.fs:4`, but the 13× repeat at an early type‑declaration line is the signature.)
- **22 findings at line 1, col 0** — the whole‑file regex rules (FSA1005 ×8, FSA1008 ×10, FSA1007 ×2, FSA1009 ×2) with null location. FSA1008 "OOP inheritance" fires at 1:0 on every `ISchemaProvider.fs` / `IEmitter.fs` / `IConformanceFixture.fs` — flagging interfaces *by filename convention*, when provider‑boundary interfaces are the correct pattern (your own Type‑Provider triangle depends on them).

### The dangerous part: the narrative laundering
`CanonFlow_Scan_Delta.md` presents all of this as *"the delta is exactly as hypothesized,"* attributing 776 findings to "standard F# null usage at boundaries (KafkaConsumer, ScaleTests)." But the data shows FSA1003 firing 13× on `FieldClass.fs:4`, `Lattice.fs`, `Refined.fs:5` — **core domain type definitions, not I/O boundaries.** A false‑positive class produced by a walk bug is being written up as a confirmed prediction. This is precisely the motivated‑reasoning failure mode you named for yourself: the artifact rationalizes the bug instead of catching it. From a CFF standpoint this is the most serious finding in the repo — worse than any single rule — because it means the evidence pipeline manufactures confirmations.

---

## 4. Docs / mission / approach

- **README / mission:** strong. Keep the framing. But it *promises* "compiler‑aware (FCS/TAST)" and "machine‑verifiable evidence (OASIS SARIF v2.1.0)". Today the majority of rules are neither compiler‑aware nor verifiable (null ranges, stale SARIF). The prose over‑claims relative to the engine.
- **The `Qwen*.md` / `Gpt*.md` "audit resolution" docs** narrate rules as "implemented / resolved" that are substring stubs. Status is asserted, not demonstrated — same integrity gap as the SARIF.
- **`ratecard.md` / grades S–F:** grading precision on top of a 3.3×‑inflated, 71%‑false‑positive finding stream produces confident‑looking nonsense. Grades inherit the garbage.

---

## 5. The path to "there" (CFF‑grade rewrite)

1. **Delete every regex rule that emits `range0`.** A rule that can't locate its finding is not a rule. Ship 5 precise TAST rules over 26 imprecise ones. Precision > coverage for an examiner.
2. **Make location a proof.** `type Located<'F> = { Finding: 'F; Range: Range }` with a smart constructor that rejects `range0`. "Finding without location" becomes unrepresentable.
3. **Model rules as a DU, not `if`‑chains.** `type Rule = FSA1001 | FSA1003 | …`, each with `detect : TypedContext -> Located<Finding> list`. One law → one detector → one evidence class. Constitution‑first.
4. **Total the walk.** Return `Result<Located<Finding> list, AnalyzerError>`. Replace `try … with _ -> ()` with explicit error propagation; put "semantic layer degraded" *in the envelope*, never silently.
5. **Dedup by set.** Findings are a `Set<Located<Finding>>`, not a mutated list. 776→236 disappears for free.
6. **Kill the reflection fallback.** Exhaustively match the `FSharpExpr` cases you support; for the rest, recurse via the SDK's typed children, not `GetProperty("ImmediateSubExpressions")`.
7. **Scope suppression by symbol range,** not `source.Contains`. An attribute suppresses only its declaration's subtree.
8. **Seal the evidence envelope.** Emit SARIF that carries a hash of `(sourceText, rulesetVersion)`; a scan whose hash doesn't match the checked‑in code is rejected in CI. This alone would have caught the stale `out.sarif`.
9. **Dogfood in CI as a gate.** FsAssay must pass FsAssay (mutable/downcast/swallow scrubbed from its own core) before it's allowed to grade anyone else. The examiner sits the exam first.

---

## 6. One‑line summary for the commit log

> FsAssay has the right thesis and a real 5‑rule TAST core, but ~85% of it is `source.Contains` running at `range0`, it triple‑counts and never dedupes (776→236, 13× repeats), 71% of its flagship CanonFlow findings are phantom nulls from DU/option TAST nodes, its committed SARIF references deleted rules, and its delta doc launders those false positives as a confirmed hypothesis. It violates parse‑don't‑validate, evidence‑integrity, totality, and constitution‑first — the four laws it exists to enforce. Not there yet; the fix is subtraction, not more rules.
