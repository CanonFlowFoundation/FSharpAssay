Extended Brainstorm: Enforcing “Elite” F# Through Missed Opportunities

The direction is strong, but FsAssay needs one constitutional distinction:

> An uncompromising mentor must still be an honest judge.



Many “Elite F#” improvements—computation expressions, active patterns, phantom types, dictionary passing and pipelines—are design opportunities, not mechanically provable defects. FsAssay should therefore have three dispositions:

Disposition	Meaning	Release effect

BLOCK	Mechanically proven violation inside an applicable profile	Fails verification
REVIEW	Strong evidence, but design context is required	Requires acknowledgement
COACH	Stylistic or abstraction opportunity	Educational only


Also distinguish:

type FindingKind =
    | Violation
    | MissedOpportunity

This allows FsAssay to be strict without pretending subjective style is mathematical truth.


---

Corrections to the original seven proposals

FSA2001: Missing Computation Expression — COACH

Good idea, but nested Option/Result matching is not automatically wrong.

Detection should require:

The outer and inner scrutinees resolve to Option<_> or Result<_,_>.

The inner computation depends upon the successful value of the outer match.

At least two failure branches merely propagate None or Error.

The nesting depth exceeds a configurable threshold.

The project has an approved CE builder available.


Do not assume result {} or option {} exists in every project. F# provides the generalized CE mechanism, but a specific Result/Option builder must come from an approved library or project implementation. CEs can model monadic, applicative and other contextual computations; they are not tied to one abstraction like Haskell’s do notation. Microsoft’s CE documentation confirms this broader role.

Suggested remediation:

This nested Result flow appears to propagate failures unchanged.
Consider the project-approved Result computation expression or
Result.bind pipeline.

FSA2002: Service Interface Obsession — REVIEW

A small internal interface may be replaceable by:

type Clock =
    { Now: unit -> DateTimeOffset }

But interfaces remain correct for:

.NET framework integration

C#-consumable libraries

Dependency-injection frameworks

Runtime substitution

Identity-bearing services

Existing public contracts


Trigger only when the interface is:

Internal

Implemented entirely inside the F# project

Not exposed through public APIs

Free of inheritance, events and framework attributes

Used as a capability bundle rather than an identity-bearing object


Record dictionary passing, SRTP/member constraints and ordinary functions are three different alternatives. FsAssay should explain them, not mechanically select one.

FSA2003: Signature Blindness — REVIEW

Useful when the names carry distinct domain meanings:

let transfer (sourceAccountId: int) (targetAccountId: int) = ...

Not useful for:

let add (x: int) (y: int) = x + y
let containsBetween low high value = ...

The finding should require semantic evidence such as:

Parameters end in Id, Code, Amount, Rate, Date, etc.

Names describe different domain roles.

The function crosses a public/domain boundary.

The parameters are not coordinates, ranges or ordinary mathematical operands.


Phantom types are only one remediation. Prefer this order:

1. Private single-case DU


2. Distinct record/DU state type


3. Unit of measure for numeric dimensions


4. Phantom state parameter when it genuinely protects transitions



FSA2004: Flag-Based State Machine — REVIEW

One Boolean field does not establish a state machine:

{ IsEnabled: bool }

Strong evidence appears when:

Two or more flags describe mutually dependent lifecycle states.

The same flags are repeatedly updated together.

Code contains combinations that are impossible or invalid.

Branches repeatedly inspect the same flag constellation.

A string status and Boolean flags duplicate the same state.


The finding should show an actual counterexample:

Current type permits IsPaid = false and IsRefunded = true.

That is much stronger than merely recommending a DU.

FSA2005: Impure Core — BLOCK when profile-proven

The return type does not establish purity.

File.ReadAllText : string -> string is effectful despite returning a pure-looking value.

Task<int> signals asynchronous execution, not purity.

Wrapping synchronous I/O in Task does not make it functional.


Correct rule:

A symbol classified as an external effect was called from a scope
declared as functional core.

Detection requires:

A reviewed core profile.

TAST-resolved effect symbols.

A versioned effect catalogue.

Propagation through locally known wrapper functions.


Remediation:

Move the operation into the shell.

Inject a capability function/record.

Pass plain input data into the core.

Return a decision describing the requested effect.


FSA2006: Nested Function Application — COACH

Use an AST application-depth threshold, but report only when piping improves the data flow.

This:

render (normalize (parse input))

may become:

input |> parse |> normalize |> render

But this may already be clearer:

Map.add key (normalize value) state

Avoid forcing pipelines when the transformed value is not the conceptual data stream or when lambdas are required merely to reverse argument order.

FSA2007: Missed Active Pattern — COACH

Active patterns are excellent for reusable classification and alternative views, but not every long conditional deserves one. Microsoft specifically recommends them as a way to expose stable pattern-matching views without revealing unstable internal representations. F# component guidelines

Trigger only when:

The same classification logic is repeated.

Several branches decompose the same input.

The classification has a meaningful domain name.

Pattern matching would simplify consumers.


Otherwise suggest a plain helper function, DU or match before recommending an active pattern.


---

5. The Totality and Exhaustiveness Gap

FSA2008: Exhaustiveness Erosion — REVIEW

Smell

Matching an owned DU using a wildcard:

match payment with
| Pending data -> handlePending data
| _ -> ignorePayment ()

When a new case is added, the wildcard silently absorbs it.

Detection

TAST establishes that the scrutinee is a project-owned DU.

A wildcard or variable catch-all consumes remaining cases.

The match occurs in core/domain code.

The wildcard does not merely extract irrelevant payload inside a known case.


Elite solution

Name every case and let the compiler reveal missing behavior after the DU evolves.


---

FSA2009: Boolean Parameter Blindness — REVIEW

Smell

let createInvoice customerId true false = ...

Elite solution

type DeliveryMode = Electronic | Printed
type TaxTreatment = Taxable | Exempt

let createInvoice customerId delivery taxTreatment = ...

Detection should focus on public functions with Boolean parameters representing modes—not predicates passed to filter, comparison functions or interop APIs.


---

FSA2010: Option Constellation — COACH

Smell

A record contains several optional fields whose legal combinations are correlated:

type Payment =
    { CardNumber: string option
      BankAccount: string option
      UpiId: string option }

It permits zero, one, two or three payment methods even if exactly one is required.

Elite solution

type PaymentMethod =
    | Card of CardDetails
    | Bank of BankDetails
    | Upi of UpiId

Detection evidence:

Several option fields

Branches repeatedly check mutually exclusive combinations

Constructors initialize most fields to None

Update code clears one field when setting another



---

FSA2011: Stringly Error Channel — REVIEW

Smell

string -> Result<Customer, string>

A raw string cannot be exhaustively handled, safely localized or reliably classified.

Elite solution

type CustomerError =
    | EmptyName
    | InvalidEmail of string
    | DuplicateCustomer of CustomerId

Default to REVIEW. A core-public-api policy may promote it to BLOCK.


---

FSA2012: Unsealed Constrained Type — REVIEW

Smell

A smart constructor exists, but callers can bypass it:

type Email = Email of string

module Email =
    let create value =
        if valid value then Ok (Email value)
        else Error InvalidEmail

Elite solution

type Email = private Email of string

Detection requires both:

Evidence that construction is intended to be constrained.

A publicly accessible DU case or constructor that bypasses the constraint.


A single-case DU without validation intent should not trigger.


---

FSA2013: Public Mutability Leak — BLOCK in core

Detect:

Public mutable fields

Public property setters

[<CLIMutable>] domain records

Mutable collections escaping from core functions

Returned aliases to internal mutable buffers


Contained local mutation may be valid. Mutation crossing the API boundary is the stronger smell.


---

6. The Applicative and Collection-Algebra Gap

FSA2014: Missed Applicative Validation — COACH

Smell

Independent validations are chained monadically, so only the first error is returned:

result {
    let! name = validateName input.Name
    let! email = validateEmail input.Email
    let! age = validateAge input.Age
    return create name email age
}

If the validations are independent, applicative validation can accumulate all errors. F# supports applicative CE syntax such as and! when the builder implements the required operations. Applicative CE guidance

Detection must prove that later validations do not depend upon earlier validated values.


---

FSA2015: Manual choose Pipeline — COACH

Smell

items
|> List.map tryParse
|> List.filter Option.isSome
|> List.map Option.get

Elite solution

items |> List.choose tryParse

This is an excellent TAST-based coaching rule because the transformation is recognizable and local.


---

FSA2016: map Used Only for Effects — REVIEW

Smell

customers
|> List.map sendEmail
|> ignore

When sendEmail returns unit, the intent is iteration rather than transformation.

Elite solution

customers |> List.iter sendEmail

Detect the resolved collection function, callback return type and discarded result.


---

FSA2017: Tuple Entropy — COACH

Flag public APIs returning or accepting:

Tuples with four or more elements

Multiple elements of the same primitive type

Tuples whose values are repeatedly accessed with fst, snd or deconstruction


Recommend a named record when the fields have durable domain meaning.


---

FSA2018: Quadratic List Growth — REVIEW; BLOCK in ETL

Smell

let rec loop acc remaining =
    match remaining with
    | x :: xs -> loop (acc @ [ x ]) xs
    | [] -> acc

Repeated tail append is typically quadratic.

Elite solution

Prepend and reverse, use a builder, or select an appropriate collection.


---

FSA2019: Repeated Sequence Enumeration — REVIEW

Detect a seq<_> binding consumed multiple times by terminal operations such as:

Seq.length rows
Seq.sumBy _.Amount rows
Seq.toList rows

The rule must account for:

Replayable versus one-shot sequences

Database/read-stream sources

Explicit caching

Bounded collections



---

7. The Structured Concurrency Gap

FSA2020: Sync-over-Async — BLOCK

Detect resolved calls to:

Task.Result

Task.Wait

GetAwaiter().GetResult()

Async.RunSynchronously


inside:

task {} or async {}

Request handlers

Actor loops

ETL worker functions

Library code declared async-safe


This is a strong TAST-plus-AST-context rule.


---

FSA2021: Unbounded Fan-Out — BLOCK in ETL; REVIEW elsewhere

Smell

items
|> Seq.map processAsync
|> Async.Parallel

or:

items |> Seq.map processTask |> Task.WhenAll

Detection should look for the absence of:

Chunking

Semaphore/channel/dataflow bounds

maxDegreeOfParallelism

A statically bounded input



---

FSA2022: Per-Item I/O — BLOCK in ETL profile

Detect effectful symbols inside lambdas passed to:

List.map

Seq.map

Array.map

fold

Row-processing loops


Examples include a database query, HTTP request or file open for every item.

The remediation should recommend batching, bounded workers or preloading an index—not merely replacing map with another function.


---

FSA2023: Disposable Lifetime Ambiguity — REVIEW/BLOCK

Detect an IDisposable value bound with let when it:

Does not escape

Is not returned

Is not passed to an owner

Is not explicitly disposed

Could safely use use


F#’s use binding guarantees predictable disposal at scope exit. Official resource-management guidance


---

FSA2024: Cancellation Loss — REVIEW

Detect a function that accepts CancellationToken but:

Calls cancellable APIs without forwarding it

Creates CancellationToken.None

Starts child tasks detached from the token

Converts cancellation into an ordinary generic failure



---

FSA2025: Fire-and-Forget Computation — BLOCK

Detect:

An unobserved Task

An ignored Async<_>

Async.Start without an explicit supervision policy

A discarded channel/dataflow completion task


A fire-and-forget operation must have a reviewed owner responsible for failure, cancellation and lifetime.


---

8. The Functional-Core Boundary Gap

FSA2030: Core Dependency Leak — BLOCK

A core project/module must not reference:

Database drivers

HTTP clients

Filesystem APIs

Logging frameworks

Web/UI frameworks

Dependency-injection containers

Clock/random/environment globals unless abstracted


This is stronger than searching for open System.IO; inspect resolved symbols and project references.


---

FSA2031: Infrastructure Type Leakage — BLOCK

Smell

val decideInvoice :
    OracleDataReader -> HttpClient -> Task<Invoice>

or a core public signature exposing:

DbConnection

HttpRequest

IConfiguration

ILogger

Framework parse/result types

Persistence entities


The core should receive plain domain values and return decisions.


---

FSA2032: Decision/Effect Entanglement — REVIEW

Detect functions that combine:

1. External reads/writes


2. Substantial domain branching


3. State transition decisions


4. Presentation or logging



Recommend splitting into:

load → decide → apply

This needs call-graph and effect-catalogue evidence, so it should not initially block.


---

FSA2033: Boundary Parser Leakage — BLOCK

Examples:

Argu ParseResults<_> escaping the CLI adapter

ASP.NET request types entering domain modules

Database rows/readers entering business rules

Raw JSON DOM values entering domain workflows


Boundary adapters must parse into owned types before calling the core.


---

FSA2034: Concrete Capability Coupling — COACH

Smell

A pure decision function directly accepts HttpClient, ILogger or DbConnection when it needs only one narrow operation.

Elite solution

type CustomerLookup =
    CustomerId -> Async<Result<Customer option, LookupError>>

or:

type CustomerCapabilities =
    { TryFind: CustomerId -> Async<Result<Customer option, LookupError>> }

Do not report this in shell code where the concrete dependency is appropriate.


---

9. The Type-Level Domain Gap

FSA2040: Unit Blindness — COACH

Detect several numeric values with dimension-bearing names:

let travel distance speed time = ...

Possible remediation:

Units of measure

Domain-specific single-case DUs

Money/currency types

Quantity records


Do not infer units blindly for generic mathematics or serialized DTO boundaries.


---

FSA2041: String Enum — REVIEW

Smell

match status with
| "pending" -> ...
| "approved" -> ...
| "rejected" -> ...
| _ -> ...

Elite solution

Parse once:

type ApprovalStatus =
    | Pending
    | Approved
    | Rejected

Then make the core exhaustive. F# DUs are specifically designed to represent values that can take one of several named forms. F# DU reference


---

FSA2042: Transition Blindness — COACH

Smell

The same record flows through functions named:

validate → approve → ship

while Boolean/status fields are updated at every stage.

Elite solution

Use distinct types or phantom-state markers:

ValidatedOrder -> ApprovedOrder -> ShippableOrder

A phantom type is justified only when it prevents an actual illegal transition. Otherwise distinct opaque records or a DU are simpler.


---

FSA2043: Raw Identifier Interchangeability — REVIEW

Extend FSA2003 beyond consecutive arguments:

Functions returning several IDs of the same primitive type

Records containing multiple raw GUID/string IDs

Collections keyed by one domain ID but queried with another

Joins between same-shaped identifier types


This is where single-case DUs provide substantial compile-time protection.


---

10. Preventing “Haskell Cosplay”

FsAssay also needs restraint rules. Elite F# means clarity and safety—not maximum abstraction.

FSA2901: Abstraction Without Reuse — COACH

A generic abstraction, SRTP helper or dictionary layer has one implementation and one call site while making types harder to understand.

FSA2902: Operator Opacity — COACH

Custom symbolic operators obscure domain meaning or require readers to memorize an undocumented algebra.

FSA2903: Computation-Expression Ceremony — COACH

A custom CE builder exists only to shorten a tiny workflow and introduces unfamiliar semantics for return, bind, loops or exception handling.

FSA2904: Phantom Without Transition — COACH

A phantom type parameter never changes, never restricts construction and never prevents an operation. It adds type noise without encoding a law.

FSA2905: Point-Free Obscurity — COACH

A composition-heavy expression hides arguments, branching or error policy. Recommend naming an intermediate value.

> The target is not “F# pretending to be Haskell.” The target is F# using its own algebraic strengths without surrendering .NET practicality.




---

Implementation priority

Tier A — build first

These can become trustworthy blocking rules through AST/TAST and profiles:

1. FSA2005 — known effect inside core


2. FSA2013 — public mutability leak


3. FSA2020 — sync-over-async


4. FSA2025 — discarded asynchronous computation


5. FSA2030 — core dependency leak


6. FSA2031 — infrastructure signature leakage


7. FSA2033 — parser/framework type leakage



Tier B — contextual review

1. Nested Option/Result flow


2. Flag-based state machines


3. Wildcards over owned DUs


4. Same-shaped domain arguments


5. Unbounded concurrency


6. Per-item I/O


7. Resource lifetime


8. Stringly status/error types



Tier C — mentor mode

1. Missing pipelines


2. Missed active patterns


3. Interface-to-function-record opportunities


4. Tuple entropy


5. Applicative validation


6. Phantom-state opportunities


7. Units of measure


8. Point-free or abstraction excess



Every Tier C message should include:

Why it triggered
Evidence observed
Suggested alternative
Recognized exceptions
Confidence
Whether it affects the verdict

The final personality of FsAssay should be:

> Compiler-strict about safety, architecture-strict about boundaries, and mentor-wise about style.
