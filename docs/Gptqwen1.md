FsAssay Elite F# Opportunity Catalogue

Consolidated and Corrected Edition

This consolidates the original FSA2001–FSA2007 proposal, Qwen’s extension, and the earlier architectural review.

The central idea survives:

> FsAssay should detect places where valid F# fails to exploit algebraic types, immutability, expression orientation, explicit effects and functional composition.



But one distinction is constitutional:

> A missed opportunity is not automatically a correctness violation.



“Elite” F# must not become “Haskell translated into F#.” F# computation expressions are broader than Haskell monads, and F# deliberately supports objects, interfaces, mutation and .NET interoperability. Microsoft describes computation expressions as a general mechanism for contextual computations, while F# itself remains functional-first rather than functional-only.


---

1. Finding model

type FindingKind =
    | Violation
    | MissedOpportunity

type Disposition =
    | Block
    | Review
    | Coach

type Certainty =
    | Deterministic
    | Contextual
    | Heuristic

These dimensions are independent.

Example	Kind	Certainty	Disposition

Infrastructure dependency inside declared core	Violation	Deterministic	Block
Wildcard over an owned DU	Missed opportunity	Contextual	Review
Deep nested application	Missed opportunity	Heuristic	Coach
Discarded non-unit expression	Violation	Deterministic	Delegate to compiler


Laws:

Text/name heuristic alone ⇒ never Block
Heuristic certainty       ⇒ never Block
Missing required context  ⇒ Inconclusive, not Pass
Interop/performance scope  ⇒ reviewed exceptions remain legal
Compiler-owned diagnostic ⇒ delegate; do not duplicate blindly


---

2. Canonical rule catalogue

A. Contextual Computation and Composition

ID	Rule	Default

FSA2001	Missing Computation Expression	Coach
FSA2002	Service Interface Obsession	Review
FSA2003	Signature Blindness	Review
FSA2004	Flag-Based State Machine	Review
FSA2005	Impure Core	Block in core
FSA2006	Nested Function Application	Coach
FSA2007	Missed Active Pattern	Coach


FSA2001 — Missing Computation Expression

Detect nested Option/Result matches only when inner operations depend upon outer successful values and failure branches merely propagate None or Error.

Do not assume result {} exists. The remediation must name the project-approved builder or suggest Result.bind.

FSA2002 — Service Interface Obsession

Detect small, internal capability interfaces that:

Are implemented only within the F# solution

Are not exposed to C# or framework consumers

Have no identity, events or inheritance

Primarily bundle functions


Recommend either:

type Clock =
    { Now: unit -> DateTimeOffset }

or a function parameter. Interfaces remain valid at .NET and public API boundaries.

FSA2003 — Signature Blindness

Detect same-shaped domain parameters:

transfer sourceAccountId targetAccountId

Exclude mathematics, coordinates, ranges, comparison functions and generic algorithms.

FSA2004 — Flag-Based State Machine

Require evidence of correlated state:

Multiple lifecycle flags

Repeated joint updates

Invalid combinations

String status plus duplicate flags


The finding should demonstrate an illegal representable state.

FSA2005 — Impure Core

Correct formulation:

Known external-effect symbol called from a scope declared as functional core.

A Task/Async return type signals asynchronous execution—not purity. Moving synchronous I/O into task {} does not make it functional.

FSA2006 — Nested Function Application

Coach when application depth exceeds a configured threshold and a pipeline clearly exposes one data flow.

Do not force pipelines where the transformed value is not the main argument.

FSA2007 — Missed Active Pattern

Recommend active patterns only for reusable classification or alternate views. They are not a universal replacement for complex if expressions. Microsoft particularly recommends active patterns for exposing stable views while hiding evolving representations. F# component guidance


---

B. Algebraic Types and Exhaustiveness

ID	Rule	Default

FSA2008	Enum Instead of DU	Review
FSA2009	Exhaustiveness Evasion	Review
FSA2010	Object Erasure	Review
FSA2011	Conditional Dispatch on Sum Types	Review


FSA2008 — Enum Instead of DU

Do not globally reject F# enums. Enums remain appropriate for:

.NET interop

Flags

Wire protocols

Database values

Compact numeric representations

C#-consumable APIs


Trigger when an enum models a domain lifecycle and nearby code requires per-state data.

// Missed opportunity
type PaymentState =
    | Unpaid = 0
    | Authorized = 1
    | Captured = 2

// Algebraic model
type PaymentState =
    | Unpaid
    | Authorized of AuthToken
    | Captured of CaptureId

FSA2009 — Exhaustiveness Evasion

Detect a wildcard over a project-owned closed DU when it absorbs unnamed cases.

Do not claim that [<RequireQualifiedAccess>] restores exhaustiveness—it does not. A comment such as // Future cases is also not proof.

Permit reviewed wildcards for:

External/open-world input

Forward-compatible protocol handling

Intentionally uniform handling of remaining cases

Compiler-generated or framework types


FSA2010 — Object Erasure

Detect obj in domain signatures when actual alternatives are closed or the function could remain generic.

Permit it for:

Reflection and serialization infrastructure

Plugin registries

Heterogeneous framework APIs

Required .NET interop


FSA2011 — Conditional Dispatch on Sum Types

Detect repeated case tests or .IsCase checks followed by casts/extraction.

Recommend one exhaustive match.

A single Boolean query such as contact.IsEmail is not necessarily a smell; F# 9 deliberately exposes .Is* properties on DUs. DU reference


---

C. Immutability and Collections

ID	Rule	Default

FSA2012	Mutable Collection Intrusion	Review; Block on core escape
FSA2013	Destructive Collection Mutation	Review
FSA2014	Imperative Accumulation	Coach
FSA2015	Redundant Type Annotation	Coach
FSA2016	Unsafe Runtime Cast	Review
FSA2017	Reflection-Based Dispatch	Review


FSA2012 — Mutable Collection Intrusion

The type name alone is insufficient. A ResizeArray used as a local builder may be correct and observably pure.

Strong evidence is:

mutable collection
∧ created or stored in core
∧ escapes its function/module
∨ is shared across calls
∨ appears in a public domain signature

FSA2013 — Destructive Collection Mutation

Resolve the receiver type. Never flag every .Add, .Remove or .Clear call by name.

Permit:

Local builders

Measured hot paths

Interop adapters

Framework-owned collections


Review mutation of shared or escaping state.

FSA2014 — Imperative Accumulation

Detect:

mutable accumulator
→ loop
→ repeated assignment
→ final accumulator return

Recommend sumBy, fold, choose, map, or an immutable recursive function.

Keep local mutation legal when a performance profile contains measurement evidence.

FSA2015 — Redundant Type Annotation

This is difficult to prove safely. An annotation may:

Document a public contract

Resolve overload ambiguity

Stabilize inference

Preserve units of measure

Prevent unwanted generic inference

Improve diagnostics


Limit the coach rule to obvious local literal bindings. Never auto-remove public annotations.

FSA2016 — Unsafe Runtime Cast

Split the operations:

:?>, unbox: runtime-risking operations

box: type erasure and possible allocation

:>: compile-time-safe upcast


A safe upcast is not an “unsafe cast.” It is frequently required for .NET APIs.

FSA2017 — Reflection-Based Dispatch

Flag reflection only when used as ad hoc domain dispatch.

Permit reflection in:

Serialization

Dependency injection

Plugin loading

Compiler/analyzer tooling

Framework adapters

Metadata inspection



---

D. Object Hierarchies and Modules

ID	Rule	Default

FSA2018	Inheritance Depth	Review
FSA2019	Virtual Dispatch in Core	Review/Block
FSA2020	Static Class as Module	Coach


FSA2018 — Inheritance Depth

Do not use an arbitrary global depth limit. A framework adapter may inherit several external base classes without design authority over them.

Review deep project-owned hierarchies inside domain code.

FSA2019 — Virtual Dispatch in Core

Recommend records of functions or ordinary higher-order functions when implementations are pure internal capabilities.

Permit virtual dispatch for:

Framework extension points

Public object-oriented libraries

Runtime identity

UI controls

Serialization infrastructure

C# interoperability


FSA2020 — Static Class as Module

Merge Qwen’s FSA2038 Namespace-Class Instead of Module into this rule.

Trigger for a project-owned class with:

No instance state

No inheritance requirement

Only static methods/properties

No framework or C# surface justification



---

E. Errors and Resource Handling

ID	Rule	Default

FSA2021	Exception as Error Channel	Review
FSA2022	Exception Throwing in Domain	Review/Block
FSA2023	Manual Dispose	Coach/Safe fix
FSA2024	Statement-Style Branching	Coach/Safe fix
FSA2025	Discarded Computation	Delegate compiler
FSA2026	Imperative Sequencing of Pure Functions	Coach


FSA2021 — Exception as Error Channel

try/with is valid at the shell boundary to convert library exceptions into typed errors.

The Haskell analogy needs correction: IO<'T> exposes an effectful computation, but does not itself enumerate possible error types. Similarly, Task<'T> does not describe its exceptions.

Strong smell:

try/with inside core
∧ recoverable/domain condition
∧ exception immediately converted into ordinary branch behavior

FSA2022 — Exception Throwing in Domain

Detect resolved calls to:

raise

failwith

failwithf

invalidArg

invalidOp

nullArg


Allow reviewed use in:

Tests

Assertions

Impossible-state guards

Framework-required APIs

Process-fatal startup invariants


FSA2023 — Manual Dispose

Detect try/finally whose finalizer exists solely to call Dispose/Close.

Recommend use when ownership and lifetime match. F#’s use binding inserts disposal at scope exit. Resource-management reference

FSA2024 — Statement-Style Branching

A strong local transformation:

let mutable result = ""
if condition then
    result <- "A"
else
    result <- "B"
result

becomes:

let result =
    if condition then "A" else "B"

This may qualify for a safe fix if range and data-flow checks are exact.

FSA2025 — Discarded Computation

F# already reports discarded non-unit expressions through FS0020.

FsAssay should:

Ingest and preserve the compiler diagnostic

Add policy/evidence context if necessary

Avoid implementing a duplicate syntax rule


FSA2026 — Imperative Sequencing of Pure Functions

Also overlaps compiler diagnostics. Coach only when TAST proves the expressions are pure and their results unused.


---

F. Domain Modeling

ID	Rule	Default

FSA2027	Stringly-Typed Domain Value	Coach/Review
FSA2028	Unmeasured Quantity	Coach
FSA2029	Ambiguous DateTime	Review
FSA2030	Boolean Flag Parameters	Review


FSA2027 — Stringly-Typed Domain Value

No name-based rule may block.

Names such as email, iban, sku and postalCode are candidate evidence. Promotion to REVIEW requires a domain policy or repeated validation logic.

Prefer typed error DUs over Result<_, string> in stable public domain APIs.

FSA2028 — Unmeasured Quantity

Units of measure are excellent for physical dimensions, but not every numeric domain value is a physical unit.

Possible solutions:

Unit of measure

Money/currency type

Percentage/rate wrapper

Constrained single-case DU

Domain quantity record


price, balance, latitude and currency should not automatically receive arbitrary measure annotations.

FSA2029 — Ambiguous DateTime

Do not globally ban DateTime. Permit:

Existing APIs and databases

DateOnly-like concepts

Framework contracts

Values whose Kind policy is controlled


Review raw DateTime in new domain APIs where UTC/local/offset meaning is unspecified. DateTimeOffset preserves an offset but does not automatically mean UTC.

FSA2030 — Boolean Flag Parameters

Two or more public Boolean mode parameters are strong evidence:

process order true false true

Recommend a record or DU. A configuration record containing three booleans may improve naming but still permit illegal combinations; prefer a DU when the choices are mutually exclusive.


---

G. Concurrency and Shared State

ID	Rule	Default

FSA2031	Lock-Based Shared-State Design	Review
FSA2032	Raw Thread Management	Review
FSA2033	Redundant Async Wrapper	Coach
FSA2034	Interface Constraint Where SRTP May Apply	Coach
FSA2035	Object Parameter Where Generic Applies	Review
FSA2036	Missing Inline on Proven Hot HOF	Coach with benchmark evidence


FSA2031 — Lock-Based Shared-State Design

Do not flag every lock or semaphore:

Short, local locks may be safer and faster than actors.

SemaphoreSlim often bounds concurrency rather than protects mutable state.

ReaderWriterLockSlim may be appropriate for read-heavy infrastructure.

A MailboxProcessor changes scheduling, ordering and failure semantics.


Trigger on shared mutable domain state guarded across multiple functions or components. Recommend ownership redesign before automatically recommending an actor.

FSA2032 — Raw Thread Management

Raw threads remain valid for:

Thread-affine APIs

Dedicated event loops

COM/native integration

Special scheduling/priority requirements


Do not recommend Async.Start as a universal replacement; it is fire-and-forget and can lose failures. Prefer supervised tasks/workflows with explicit cancellation and ownership.

FSA2033 — Redundant Async Wrapper

A non-async value directly supplied to let! normally will not type-check. Detect the actual pattern:

let! value = async { return pureExpression }

Recommend:

let value = pureExpression

FSA2034 — Interface Constraint Where SRTP May Apply

SRTP is not automatically more elite. It brings:

inline propagation

More complex diagnostics

Possible code expansion

Reduced public API accessibility

Harder tooling and discoverability


Recommend SRTP mainly for numeric/operator-generic algorithms. For application capabilities, ordinary functions or records of functions are often clearer.

FSA2035 — Object Parameter Where Generic Applies

Strong when the function only uses members available on every type, such as equality or ToString, and no runtime type switching occurs.

FSA2036 — Missing Inline on Proven Hot HOF

Reject Qwen’s syntax-only version.

Static source cannot establish a hot path. Emit only when performance policy supplies benchmark/profile evidence. Otherwise this is premature optimization.


---

H. Structure and Readability

ID	Rule	Default

FSA2037	Access Surface Smell	Coach
FSA2038	Retired—merged into FSA2020	—
FSA2039	Unqualified Common DU Cases	Coach
FSA2040	Redundant Lambda	Coach
FSA2041	Retired—merged into FSA2006	—


FSA2037 — Access Surface Smell

Reject an arbitrary “30% modifiers” threshold.

Do not recommend [<AutoOpen>] casually; it can increase name pollution.

Useful signals:

Public implementation helpers accidentally exposed

Repeated public/private declarations that disagree with .fsi

Domain constructors exposed despite smart constructors

Public API surface differing from intended policy


FSA2039 — Unqualified Common DU Cases

[<RequireQualifiedAccess>] is a readability and namespace policy—not a universal algebraic law.

Recommend it for common or collision-prone cases such as:

Success, Failure, Started, Completed, Unknown

Do not demand it for every DU.

FSA2040 — Redundant Lambda

Examples:

fun x -> f x
fun x -> x |> f |> g

Possible simplifications:

f
f >> g

Coach only. Eta-expanded forms may improve:

Type inference

Error messages

Debugging

Parameter naming

Readability



---

3. Missing high-value rules added to the catalogue

Qwen’s extension still misses several more valuable, more mechanically defensible rules.

Effect and concurrency safety

ID	Rule	Default

FSA2042	Sync-over-Async	Block
FSA2043	Unbounded Fan-Out	Block in ETL; Review elsewhere
FSA2044	Per-Item I/O	Block in ETL
FSA2045	Disposable Lifetime Ambiguity	Review/Block
FSA2046	Cancellation Loss	Review
FSA2047	Fire-and-Forget Computation	Block/Review


FSA2042 — Sync-over-Async

Resolve actual symbols:

Task.Result

Task.Wait

GetAwaiter().GetResult()

Async.RunSynchronously


Join the TAST call range with AST context such as task {}, async {}, server handlers or worker loops.

FSA2043 — Unbounded Fan-Out

Detect Async.Parallel, Task.WhenAll, parallel map or task accumulation over an input with no proven bound.

FSA2044 — Per-Item I/O

Detect database, HTTP or filesystem effects inside mapping/folding/row loops. Recommend batching or bounded workers.

FSA2045 — Disposable Lifetime Ambiguity

Stronger than manual try/finally: find IDisposable values acquired with let that neither escape nor receive deterministic disposal.

FSA2046 — Cancellation Loss

Detect accepted cancellation tokens that are ignored, replaced with CancellationToken.None, or not forwarded to cancellable children.

FSA2047 — Fire-and-Forget Computation

Detect discarded Task/Async, unsupervised Async.Start, unobserved worker completion and detached pipelines.


---

Architectural boundaries

ID	Rule	Default

FSA2048	Core Dependency Leak	Block
FSA2049	Infrastructure Type Leakage	Block
FSA2050	Boundary Parser Leakage	Block
FSA2051	Stringly Error Channel	Review/Block
FSA2052	Option Constellation	Review
FSA2053	Unsealed Constrained Type	Review/Block
FSA2054	Public Mutability Leak	Block in core


FSA2048 — Core Dependency Leak

A declared functional core may not reference database drivers, HTTP clients, filesystems, logging frameworks, DI containers or UI/web frameworks.

FSA2049 — Infrastructure Type Leakage

Block infrastructure types in core public signatures:

DbConnection
OracleDataReader
HttpRequest
ILogger
IConfiguration
framework entities

FSA2050 — Boundary Parser Leakage

Block raw boundary types such as Argu ParseResults<_>, JSON DOM values, request types or database readers from escaping into domain logic.

FSA2051 — Stringly Error Channel

Review Result<_, string> in domain APIs. A versioned core policy may require an error DU.

FSA2052 — Option Constellation

Detect records containing correlated optional fields whose combinations imply hidden sum types.

FSA2053 — Unsealed Constrained Type

Detect a smart constructor that can be bypassed through a public DU case or constructor.

FSA2054 — Public Mutability Leak

Block settable members, public mutable fields and escaping mutable collections in core APIs.


---

Algebra and collection opportunities

ID	Rule	Default

FSA2055	Missed Applicative Validation	Coach
FSA2056	Manual Choose Pipeline	Coach
FSA2057	Map Used Only for Effects	Review/Coach
FSA2058	Tuple Entropy	Coach
FSA2059	Quadratic List Growth	Review; Block in ETL
FSA2060	Repeated Sequence Enumeration	Review
FSA2061	String Status Enumeration	Review
FSA2062	State Transition Blindness	Coach/Review



---

4. Anti-overengineering counter-rules

“Elite” must not reward abstraction theatre.

ID	Rule	Purpose

FSA2901	Abstraction Without Reuse	Detect generic machinery with no demonstrated reuse
FSA2902	Operator Opacity	Discourage undocumented symbolic operator systems
FSA2903	Computation-Expression Ceremony	Question custom builders with unclear semantics
FSA2904	Phantom Without Transition	Detect phantom parameters that enforce no state law
FSA2905	Point-Free Obscurity	Prefer named arguments when composition hides intent


These remain mentor-only.


---

5. Corrected detection matrix

Mechanism	Suitable evidence	Authority

Text/name matching	Domain-name hints, coaching candidates	Never blocks
Untyped AST	Expression shape, wildcard, enum, mutation, branch structure	Deterministic syntax only
TAST	Resolved symbol, actual type, member identity, public API	Semantic rule evidence
Project context	TFM, references, core/shell/profile boundaries	Architectural authority
Data flow	Escape, ownership, mutation, effect propagation	Contextual evidence
Compiler diagnostic	Incomplete match, discarded value, type/check error	Delegate to compiler
Benchmark/profile	Hot path, allocation and inline opportunity	Performance authority
Policy/domain contract	Required wrappers, allowed effects, state laws	Contextual blocking authority


No rule is “zero false positive” merely because its AST detection is trivial. Detecting an enum perfectly does not prove that the enum is wrong.


---

6. Revised implementation order

Phase 0 — Verification substrate

Before FSA2001+:

Real project and all-TFM loading

AST/TAST adapters

Exact ranges

Completed/Skipped/Failed

Pass/Fail/Inconclusive/ToolFailure

Toolchain locking

Rule catalogue

Profiles

Canonical JSON

RunAnalyzersSafely

CI and fault injection


Tier A — Deterministic/profile-backed rules

Start with:

1. FSA2048 Core Dependency Leak


2. FSA2049 Infrastructure Type Leakage


3. FSA2050 Boundary Parser Leakage


4. FSA2042 Sync-over-Async


5. FSA2047 Fire-and-Forget


6. FSA2054 Public Mutability Leak


7. FSA2023 Manual Dispose


8. FSA2024 Statement-Style Branching



Tier B — Contextual semantic rules

Exhaustiveness evasion

Mutable collection escape

Exception as error channel

Unbounded concurrency

Per-item I/O

Cancellation loss

Unsealed constrained types

Boolean state machines

String statuses/errors


Tier C — Mentor rules

Missing CE

Small service interface

Same-shaped domain arguments

Pipelines

Active patterns

Units of measure

Tuple entropy

Applicative validation

Eta reduction

SRTP opportunity


Do not schedule by arbitrary “Week 1/Week 2” labels. A rule graduates only when its proof obligations pass.


---

7. Rule acceptance contract

Every rule must declare:

ID and stable title
Kind: Violation | MissedOpportunity
Certainty
Default disposition
Applicable profiles
AST/TAST/data-flow mechanism
Required context
Positive specimen
Passing specimen
Near miss
Boundary exception
Falsifier
Exact range law
Remediation
Safe-fix eligibility
Compiler/FSharpLint overlap

Promotion to blocking requires:

semantic condition is mechanically proven
∧ applicable profile is unambiguous
∧ counterexamples are encoded
∧ missing context cannot become success
∧ no name/text heuristic participates in the proof


---

Closing principle

Qwen’s ending should be changed. FsAssay should not make code “shameful”; shame encourages suppression, gaming and tribal arguments.

The consolidated principle is:

> FsAssay asks whether the code exploits F#’s algebraic, immutable, expression-oriented and type-inferred strengths—and produces evidence when it does not.



Its job is to make the weaker path:

visible
→ precisely named
→ contextually judged
→ constructively explained
→ difficult to introduce accidentally

The desired personality is:

> Compiler-strict about correctness, architecture-strict about boundaries, profile-aware about pragmatism, and mentor-wise about style.
