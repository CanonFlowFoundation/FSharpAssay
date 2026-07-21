FSharpAssay — hard scrutiny

Reviewed main at commit 21fbde5, dated 21 July 2026.

Verdict

Keep the thesis. Reset the implementation.

FSharpAssay currently has:

A strong verification constitution: 9/10

A useful product direction: 8/10

A real static-analysis engine: 1/10

Trustworthy tests: 1/10

Release-gate capability: 0/10

Production readiness: 0/10


The repository is approximately 1,275 lines of Markdown versus 246 lines of F#. The specification is far ahead of the executable system.

Today this is a whole-file lexical experiment, not yet an F# Verification Officer. Call it v0.0.1 lexical prototype, not a completed v0.1 engine.


---

P0: The current scanner cannot be trusted as a gate

1. It does not analyze F# syntax or symbols

The analyzer converts the entire source file to a string and runs nine substring/regex checks. It never uses ParseFileResults, TypedTree, or compiler symbols in its rules. See the current analyzer.

Therefore these innocent sources can trigger violations:

// Avoid Option.get and mutable in domain code.

let documentation = "Do not write null or while loops."

let pairValue = keyValuePair.Value

And these can evade detection:

let mutable
    total = 0

open System.Collections.Generic
let cache = Dictionary<string, int>()

try work ()
with _ -> recover ()

FSA1002 cannot distinguish Option.Value from KeyValuePair.Value, a user-defined Value property, or anything else named Value.

2. Every finding points to nowhere

Every diagnostic uses:

Range = Range.Zero

So the tool cannot identify:

The violating line

The violating expression

Multiple occurrences

Whether suppression covers the finding

Whether a remediation matches the source snapshot


The repository’s own scan log also reports that Range.Zero is deprecated.

3. The runner always exits successfully

The runner returns 0 even when it prints violations.

That means:

100 violations → exit 0 → CI success

This alone disqualifies it from being called a verification gate.

4. The runner fabricates compiler context

It constructs CliContext using:

ParseFileResults = Unchecked.defaultof<_>
CheckFileResults = Unchecked.defaultof<_>
TypedTree = None
CheckProjectResults = Unchecked.defaultof<_>
ProjectOptions = Unchecked.defaultof<_>

This violates the central architecture described in FSharpAssay.md. It also calls the analyzer function directly, bypassing:

Analyzer assembly loading

RunAnalyzersSafely

SDK suppression filtering

Load-failure detection

Analyzer identity

Compatibility verification


The v1.1 plan correctly identifies RunAnalyzersSafely as essential, but the executable path does not use it.

5. It spectacularly fails self-analysis

If FSharpAssay scans itself:

Library.fs triggers seven of its own rules because rule patterns and messages contain words such as mutable, Option.get, null, while, and ResizeArray.

Tests/Program.fs triggers all nine because hostile specimens are embedded in strings.

Runner/Program.fs triggers its null rule because it uses Unchecked.defaultof.


A static analyzer must understand that strings and comments are not executable constructs.


---

P0: The rule philosophy contradicts its own source material

The linked stylish-fsharp material explicitly says F# is functional-first, not functional-only. It permits:

Local mutation in measured hot paths

Arrays and mutable buffers when performance requires them

Classes for encapsulated identity

Interfaces and inheritance for .NET interop

Object expressions as idiomatic F#

while loops in streaming implementations


See the source skill’s objects, async and performance guidance.

But the current analyzer makes all of these unconditional Errors.

That means FSharpAssay presently rejects examples recommended by the constitution it is supposed to enforce.

Correct rule classification

Current rule	Correct direction

Mutation	Contextual. Block escaped/shared mutation in core; permit contained mutation in performance/interop scopes.
Partial access	Strong first deterministic rule, but identify actual symbols through TAST.
Null	AST-based and profile-aware. Block in owned domain types; permit reviewed interop boundaries.
Primitive alias	Usually advisory unless a domain contract proves a constrained type is required.
isValid*	Remove as a blocking rule. Boolean predicates are normal and idiomatic F#.
Generic catch	Contextual. Catching once at a shell boundary and converting to Result is recommended.
while	Contextual. Analyze purpose and scope; never ban globally.
Inheritance/interfaces	Permit in interop, framework adapters and tests. Block only where policy declares a pure core.
Mutable collections	Analyze escape, lifetime and sharing—not merely the type name.


FSA1005 is especially dangerous. Functions such as isEmpty, isReady, isWeekend, and predicates passed to List.filter legitimately return bool. “Parse, don’t validate” applies to construction boundaries, not every Boolean function.


---

P0: Tests prove only that regexes match their own examples

The test suite contains ten positive tests. Each test asks only:

Did any diagnostic with this code appear?

An analyzer that emitted all nine codes for every file would pass the entire suite.

Missing tests include:

Passing examples

Near misses

Comments and strings

Shadowed/user-defined symbols

Exact diagnostic count

Exact source range

Multiple occurrences

.fsi files

Invalid F# and compiler errors

Missing TAST

Analyzer exceptions

Load failures

Suppression behavior

Project references

Conditional compilation

Multiple TFMs

Deterministic ordering


All tests also write to the same temporary Test.fs. Expecto runs tests in parallel by default, so this introduces a file race. The upstream Expecto documentation recommends its integrated executable runner and describes the parallel default. dotnet run is appropriate, but each specimen needs isolated state.

The milestone claim of “100% test coverage” is unsupported: no coverage package, report, threshold or CI evidence exists.


---

P0: Toolchain pinning is demonstrably broken

The checked-in scan output records repeated NU1608 warnings:

FSharp.Analyzers.SDK requires FSharp.Core 10.1.201
FSharpAssay resolved FSharp.Core 10.1.301

Yet Milestones.md marks deterministic toolchain pinning complete.

Upstream SDK v0.37.2 pins FSharp.Core exactly to 10.1.201 in its central package configuration.

Required correction:

Add global.json

Add Directory.Packages.props

Explicitly pin compatible FSharp.Core, FCS and analyzer SDK versions

Add packages.lock.json

Restore in locked mode

Treat NU1608 and compatibility warnings as errors

Begin with the upstream-tested net8.0 analyzer baseline unless net10 compatibility is independently proven



---

Documentation has split into incompatible realities

There are at least three rule catalogues:

Rule	v1.1 plan	Executable/README

FSA1001	Null literal	Mutation
FSA1002	Partial access	Partial access
FSA1003	Exception control flow	Null
FSA1101	Blocking thread	Not implemented


AntiPatterns_TDD_Plan.md and Demonstration.md use the plan numbering, while the implementation and README use another numbering.

This breaks:

Baselines

Suppressions

SARIF history

Documentation links

CI policies

Future migration


Freeze identifiers now. Put them in one machine-readable catalogue and generate human documentation from it.


---

Required course correction

1. Reopen Phase 0

Mark current milestone status honestly:

Phase 0: In progress
Regex prototype: completed
Verification engine: not started
Rule catalogue: unstable
Release gate: unavailable

Archive or clearly label the promotional real-world scan conclusions. Finding substrings in public repositories does not establish that those repositories contain genuine violations.

Do not add FSA1010 or more regex rules.

2. Preserve the prototype—but remove its authority

Rename:

FsAssay.Runner → FsAssay.LexicalProbe

It may remain useful for:

Corpus discovery

Generating candidate locations

Comparing lexical versus semantic precision


It must never produce Pass or a release verdict.

3. Build one narrow vertical trust slice

The first real rule should be FSA1002:

Option.get / Option.Value partial access

Acceptance requirements:

Resolve the exact FSharp.Core symbol through TAST

Ignore comments and strings

Ignore user-defined Option.get

Ignore unrelated .Value properties

Emit one finding per occurrence

Emit exact source ranges

Missing TAST returns Skipped

Analyzer exceptions return Failed

Required skipped rule makes the run Inconclusive

Same input produces byte-equivalent normalized JSON


Only after this works should you implement AST-based null detection.

4. Build around a pure domain model

flowchart TD
    CLI --> Orchestrator
    Orchestrator --> ProjectSystem
    ProjectSystem --> FCS
    Orchestrator --> RuleEngine
    RuleEngine --> ASTTAST["AST/TAST adapter"]
    Orchestrator --> Policy
    Orchestrator --> Evidence

Recommended projects:

FsAssay.Domain
FsAssay.SdkAdapter
FsAssay.ProjectSystem
FsAssay.Rules
FsAssay.Policy
FsAssay.Reporting
FsAssay.Cli

FsAssay.UnitTests
FsAssay.IntegrationTests
FsAssay.AcceptanceTests
FsAssay.CorpusTests

FsAssay.Domain should have no dependency on FCS or the analyzer SDK.

5. Make the verdict executable before expanding rules

Implement these outcomes first:

type RuleEvaluation =
    | Completed of Finding list
    | Skipped of SkipReason
    | Failed of RuleFailure

type AssayVerdict =
    | Pass
    | Fail
    | Inconclusive
    | ToolFailure

Then prove the exit contract:

Condition	Exit

Full pass	0
Blocking finding	1
Required evidence missing	2
Analyzer/compiler/project failure	3
Invalid invocation	64


No new idiom rule should land until these semantics exist.

6. Establish profiles before controversial rules

At minimum:

core: strict functional domain

shell: effects, exception conversion and resource handling allowed

interop: classes, interfaces, null conversion and mutable framework objects allowed

performance: reviewed local mutation and arrays allowed

test: test builders and failure helpers allowed


Without profiles, FSharpAssay will punish correct boundary code and encourage developers to hide necessary interop.

7. Enforce Law Zero operationally

A law written in Markdown is not a trust boundary.

Add:

CODEOWNERS for rules, policies, gold fixtures and workflows

Branch protection

Mandatory independent approval when those files change

CI path checks identifying judge-changing pull requests

Eventually, a separately versioned and signed rule/policy package


The AI may change production code and receive analyzer feedback, but it must not silently weaken the judge that evaluates that change.


---

Exact PR order

1. Truth reset: correct milestones, rule IDs and maturity claims.


2. Toolchain lock: clean zero-warning build, locked restore and CI.


3. Verdict kernel: pure outcomes, normalized findings and exit codes.


4. Project loading: real .fsproj/solution loading with compiler diagnostics.


5. FSA1002 TAST slice: exact symbol identity and ranges.


6. FSA1001 AST slice: null expression with profile-aware disposition.


7. Evidence: canonical JSON, SARIF and toolchain record.


8. Profiles and suppression: core, shell, interop; visible authorization.


9. Corpus adjudication: precision/recall per rule against manually labelled specimens.


10. Editor integration: only after CLI verification is trustworthy.



Bottom line

FSharpAssay’s architecture document is the valuable asset. The current nine-rule implementation should be treated as disposable scaffolding.

The right move is not to refine its regexes. The right move is:

> Freeze rule expansion, reopen Phase 0, build the verdict kernel, and prove one TAST rule end-to-end.



I could not independently execute the .NET build because this review environment lacks dotnet; the source review and the repository’s checked-in build log nevertheless expose the structural and dependency failures above.
