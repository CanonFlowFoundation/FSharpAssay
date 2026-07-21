# Brainstorm Extension: Enforcing "Elite" F# (Haskellish/OCamlish) via Code Smells

*Continuing from FSA2007. The following extends the taxonomy into the remaining paradigm gaps between "C#-in-F#" and true algebraic, pure, expression-oriented functional programming.*

---

## 5. The Algebraic Data Types / Exhaustiveness Gap

Haskell and OCaml treat sum types as the *primary* modeling tool. Every `data` declaration in Haskell or `type` in OCaml is a closed algebraic type with compiler-enforced exhaustiveness. F# has DUs, but developers from C# reach for `enum`, `class` hierarchies, or `obj` instead.

### Smell: C#-Style `enum` for Domain States

*   **Detection:** AST reveals `SynTypeDefn` with `SynTypeDefnRepr.Enum`. Alternatively, TAST reveals a type deriving from `System.Enum`.
*   **Why it's C#-ish:** C# `enum` is an open integer with no data payload. You cannot attach per-case data, you cannot pattern-match with exhaustiveness guarantees in the type system, and you can cast any `int` into the enum unsafely.
*   **Elite F# Solution:** Discriminated Unions. `type PaymentState = | Unpaid | Authorized of AuthToken | Captured of CaptureId` — each case carries exactly the data it needs, and the compiler forces you to handle every case.
*   **Proposed Rule:** **FSA2008: Enum Instead of DU** — "C#-style `enum` detected. Replace with a Discriminated Union to attach per-case data and gain exhaustive pattern matching."

### Smell: Wildcard Catch-All in Pattern Matching

*   **Detection:** AST reveals `SynMatchClause` with `SynPat.Wild` (`_`) as the *final* clause in a `match` expression where the matched type is a DU or `Option`/`Result`.
*   **Why it's C#-ish:** The `_ -> ...` wildcard is the F# equivalent of C#'s `default:` in a `switch`. It silently swallows new cases when the DU is extended, defeating the entire purpose of algebraic types. In Haskell, omitting a case is a compile error (with `-Wall`). In OCaml, the compiler warns. In F#, the wildcard *hides* the warning.
*   **Elite F# Solution:** Remove the wildcard. Let the compiler's incomplete-match warning (`FS0025`) guide you. If a catch-all is truly needed (e.g., for forward compatibility), use `[<RequireQualifiedAccess>]` and explicitly list all cases with a commented `// Future cases` marker.
*   **Proposed Rule:** **FSA2009: Exhaustiveness Evasion** — "Wildcard `_` pattern on a closed Discriminated Union suppresses exhaustiveness checking. Enumerate all cases explicitly to preserve algebraic safety."

### Smell: `obj` / `System.Object` as a Poor Man's Sum Type

*   **Detection:** TAST reveals a function parameter, return type, or record field typed as `obj` or `System.Object`. AST reveals `SynType.LongIdent` resolving to `obj`.
*   **Why it's C#-ish:** Using `obj` is the C# `object` pattern — "I'll accept anything and cast later." It erases all type safety, forces runtime `:?>` downcasts, and makes illegal states representable.
*   **Elite F# Solution:** Model the actual cases as a DU. If the set of types is truly open, use a generic parameter `'T` or a DU with explicit cases for each expected type.
*   **Proposed Rule:** **FSA2010: Object Erasure** — "`obj` / `System.Object` used where a Discriminated Union or generic parameter would preserve type safety. Replace with an explicit sum type."

### Smell: `if/elif/else` Chains on DU Case Tags

*   **Detection:** AST reveals nested `SynExpr.IfThenElse` where the condition is a `SynExpr.App` calling a DU case test (e.g., `match x with | CaseA _ -> ...` rewritten as `if (match x with CaseA _ -> true | _ -> false)`), or where the condition checks `x.Case` properties.
*   **Why it's C#-ish:** This is a `switch` statement wearing F# syntax. It loses exhaustiveness, loses data extraction, and creates boolean spaghetti.
*   **Elite F# Solution:** A single `match` expression with one clause per case.
*   **Proposed Rule:** **FSA2011: Conditional Dispatch on Sum Types** — "If/elif chain dispatching on type/case identity detected. Replace with a single `match` expression for exhaustive, data-extracting pattern matching."

---

## 6. The Immutability / Persistent Collections Gap

Haskell's `Data.List`, `Data.Map`, and `Data.Set` are *persistent* and *immutable* by default. OCaml's `List` and `Map` are the same. F# provides `List<'T>`, `Map<'T>`, and `Set<'T>` as immutable defaults, but the .NET BCL's mutable collections are one `open System.Collections.Generic` away.

### Smell: Mutable BCL Collections in Domain Code

*   **Detection:** TAST resolves a type reference to `System.Collections.Generic.List<'T>` (a.k.a. `ResizeArray<'T>`), `System.Collections.Generic.Dictionary<'TKey, 'TValue>`, or `System.Collections.Generic.HashSet<'T>`.
*   **Why it's C#-ish:** These are reference-semantics, in-place-mutation collections. They break referential transparency, create aliasing bugs, and make reasoning about state impossible. In Haskell, reaching for `IORef [a]` instead of `[a]` would be a code smell of the same magnitude.
*   **Elite F# Solution:**
    *   `ResizeArray<'T>` → `List<'T>` (F# immutable linked list)
    *   `Dictionary<'K, 'V>` → `Map<'K, 'V>` (F# immutable balanced tree)
    *   `HashSet<'T>` → `Set<'T>` (F# immutable balanced tree)
*   **Proposed Rule:** **FSA2012: Mutable Collection Intrusion** — "BCL mutable collection (`ResizeArray`/`Dictionary`/`HashSet`) detected in domain logic. Replace with F# immutable equivalents (`List`/`Map`/`Set`) to preserve referential transparency."

### Smell: In-Place Mutation via Indexers or `.Add()` / `.Remove()`

*   **Detection:** AST reveals `SynExpr.App` calling `.Add(`, `.Remove(`, `.Clear(`, or indexer assignment `expr.[i] <- value` on a collection-typed expression.
*   **Why it's C#-ish:** This is the imperative mutation pattern. In Haskell, you would `map`, `filter`, `foldl'`, or construct a new list. In OCaml, you'd use `List.map`, `List.fold_left`.
*   **Elite F# Solution:** Use `List.map`, `List.filter`, `List.fold`, `Map.add`, `Map.remove`, `Set.add`, `Set.remove` — all of which return *new* collections.
*   **Proposed Rule:** **FSA2013: Destructive Collection Mutation** — "In-place collection mutation (`.Add`/`.Remove`/indexer assignment) detected. Use immutable transformation functions (`List.map`, `Map.add`, etc.) that return new collections."

### Smell: `mutable` Accumulator + `for` Loop Instead of `fold`

*   **Detection:** AST reveals a `SynBinding` with `IsMutable = true` whose type is a collection or numeric accumulator, followed by a `SynExpr.For` or `SynExpr.ForEach` that mutates it.
*   **Why it's C#-ish:** This is the C# `var total = 0; foreach (var x in items) { total += x; }` pattern. It introduces mutable state, loop variables, and side effects where a pure fold would suffice.
*   **Elite F# Solution:** `List.fold`, `List.sumBy`, `List.foldBack`, `Seq.fold`, or a tail-recursive helper function.
*   **Proposed Rule:** **FSA2014: Imperative Accumulation** — "Mutable accumulator with loop detected. Replace with `List.fold` / `Seq.fold` or a tail-recursive function to eliminate mutable state."

---

## 7. The Type Inference / Casting Gap

Haskell and OCaml have powerful type inference (Hindley-Milner). You almost never write explicit type annotations, and you *never* downcast. F# has HM inference too, but C# developers bring habits of explicit typing, `as` casts, and `typeof<>` reflection.

### Smell: Gratuitous Type Annotations

*   **Detection:** AST reveals `SynBinding` or `SynPat.Typed` where the annotated type is *identical* to what the compiler would infer. Specifically: `let x : int = 5`, `let f (a : string) : string = a`, or annotating a `let` binding whose RHS is a literal or a function whose type is fully determined.
*   **Why it's C#-ish:** In C#, you write `int x = 5;` because the language requires it. In F#, `let x = 5` is sufficient. Over-annotation adds noise, fights inference, and signals "I'm still thinking in C#." In Haskell, you write type signatures for *top-level* functions (as documentation), but never for local `let` bindings.
*   **Elite F# Solution:** Remove redundant annotations. Keep annotations only on module-level `let` bindings as documentation (Haskell-style top-level signatures), and only when the inferred type is non-obvious (e.g., SRTP constraints).
*   **Proposed Rule:** **FSA2015: Redundant Type Annotation** — "Type annotation matches the compiler-inferred type. Remove it to reduce noise and trust Hindley-Milner inference. Retain annotations only on public module-level signatures as documentation."

### Smell: `:?>` Downcast / `:>` Upcast / `box` / `unbox`

*   **Detection:** AST reveals `SynExpr.Downcast` (`:?>`), `SynExpr.Upcast` (`:>`), `SynExpr.App` calling `box` or `unbox`.
*   **Why it's C#-ish:** `(Foo)bar` and `bar as Foo` are C# idioms. They bypass the type system, introduce runtime `InvalidCastException` risk, and indicate that the type hierarchy is wrong. In Haskell, you *cannot* downcast. In OCaml, `Obj.magic` exists but is considered a last resort.
*   **Elite F# Solution:** If you need to dispatch on runtime type, model the cases as a DU. If you need generic behavior, use generics (`'T`) or SRTP. If you need dynamic dispatch, use a Record of Functions (dictionary passing).
*   **Proposed Rule:** **FSA2016: Unsafe Cast** — "Runtime cast (`:?>`, `:>`, `box`, `unbox`) detected. Model the type alternatives as a Discriminated Union or use generics/SRTP to eliminate the cast."

### Smell: `typeof<>` / `GetType()` / Reflection for Dispatch

*   **Detection:** AST/TAST reveals calls to `typeof<'T>`, `GetType()`, `Type.IsAssignableFrom`, `Activator.CreateInstance`, or `System.Reflection` namespace usage.
*   **Why it's C#-ish:** This is C#'s `if (obj is Foo)` / `switch (obj.GetType())` pattern. It moves type dispatch from compile time to runtime, erases type safety, and creates fragile string-based logic.
*   **Elite F# Solution:** DU + pattern matching. The compiler verifies exhaustiveness. No reflection needed.
*   **Proposed Rule:** **FSA2017: Reflection-Based Dispatch** — "Runtime type inspection (`typeof`/`GetType`/Reflection) used for dispatch. Replace with a Discriminated Union and pattern matching for compile-time safety."

---

## 8. The Composition / Inheritance Gap

Haskell has *no* inheritance. OCaml has objects but the community strongly prefers modules and functors. Both languages compose behavior via typeclasses, modules, and higher-order functions. F# supports `inherit` and `interface`, but elite F# avoids them in domain code.

### Smell: Deep Inheritance Hierarchy (> 2 levels)

*   **Detection:** TAST reveals a type whose `BaseType` chain exceeds 2 hops (e.g., `C : B : A : obj`). AST reveals `SynTypeDefnRepr.ObjectModel` with `SynTypeDefnKind.Class` and `SynType.Inherit`.
*   **Why it's C#-ish:** Deep inheritance is the hallmark of Java/C# "Enterprise" design. It creates tight coupling, fragile base class problems, and makes behavior impossible to reason about locally. Haskell solves this with typeclasses (ad-hoc polymorphism without hierarchy). OCaml solves it with functors (module-level parameterization).
*   **Elite F# Solution:** Composition via Record of Functions (FSA2002), higher-order functions, or generic parameters with constraints. If polymorphism is needed, use `interface` with *one* method (which F# can auto-convert to a function) or SRTP.
*   **Proposed Rule:** **FSA2018: Inheritance Depth** — "Inheritance chain exceeds 2 levels. Flatten the hierarchy using composition (Record of Functions, higher-order functions) or generic constraints."

### Smell: `abstract` Class with `override` / `virtual`

*   **Detection:** AST reveals `SynMemberDefn.AbstractSlot` with `IsOverride = true` or `SynMemberDefn.Member` with `SynBindingKind.Do` containing `override` keyword. TAST reveals `FSharpMemberOrFunctionOrValue.IsOverrideOrExplicitInterfaceImplementation`.
*   **Why it's C#-ish:** `virtual`/`override` is C#/Java's mechanism for runtime polymorphism. It requires a class hierarchy, couples base and derived types, and makes the "open/closed principle" a source of bugs. In Haskell, you'd define a typeclass and implement it independently for each type. In OCaml, you'd use a module functor.
*   **Elite F# Solution:** Define a Record of Functions (the "typeclass dictionary"). Each "implementation" is just a record value. No inheritance, no `override`, no coupling.
    ```fsharp
    type Serializer<'T> = {
        Serialize : 'T -> string
        Deserialize : string -> Result<'T, string>
    }
    let jsonSerializer : Serializer<Order> = {
        Serialize = fun o -> JsonSerializer.Serialize(o)
        Deserialize = fun s -> ...
    }
    ```
*   **Proposed Rule:** **FSA2019: Virtual Dispatch** — "`abstract`/`virtual`/`override` members detected. Replace with a Record of Functions (dictionary passing) to decouple implementations from a rigid type hierarchy."

### Smell: Static Class as Namespace

*   **Detection:** AST reveals `SynTypeDefnRepr.ObjectModel` with `SynTypeDefnKind.Class` where all members are `static`, and the class has no constructor logic. TAST reveals `FSharpEntity.IsClass` with all `FSharpMemberOrFunctionOrValue.IsStatic`.
*   **Why it's C#-ish:** In C#, you write `public static class MathHelpers { public static int Add(int a, int b) => a + b; }` because C# requires all functions to live in a class. F# has `module` for exactly this purpose.
*   **Elite F# Solution:** `module MathHelpers` with top-level `let` bindings.
*   **Proposed Rule:** **FSA2020: Static Class as Module** — "Class with only static members detected. Replace with an F# `module` — it exists precisely for this purpose and requires no instantiation."

---

## 9. The Error Handling / Exception Gap

Haskell uses `Either e a` (or `ExceptT e m a`). OCaml uses `('a, 'b) result` (since 4.03) or exceptions for truly exceptional cases. Both treat errors as *values in the type signature*. F# has `Result<'T, 'E>`, but C# developers reach for `try/catch` and `throw`.

### Smell: `try/with` for Control Flow

*   **Detection:** AST reveals `SynExpr.TryWith` where the `with` clauses catch specific exception types (not just `_ ->`) and the body is not a resource cleanup (`use`/`finally`). Heuristic: the `try` body contains function calls that return `Result` or `Option`, suggesting the exception is being used as an error channel.
*   **Why it's C#-ish:** C# uses exceptions for *all* error handling — `FileNotFoundException`, `ArgumentNullException`, `InvalidOperationException`. This makes errors invisible in the type signature. In Haskell, `readFile :: FilePath -> IO String` tells you it can fail; `readFileEither :: FilePath -> IO (Either IOError String)` makes the error type explicit.
*   **Elite F# Solution:** Return `Result<'T, 'E>` or `Option<'T>`. Use `try/with` *only* at the system boundary (the "Imperative Shell") to convert BCL exceptions into `Result` values.
*   **Proposed Rule:** **FSA2021: Exception as Error Channel** — "`try/with` used for domain error handling. Return `Result<'T, 'E>` or `Option<'T>` to make errors visible in the type signature. Reserve exceptions for truly unrecoverable failures."

### Smell: `raise` / `failwith` / `invalidArg` in Domain Code

*   **Detection:** AST reveals `SynExpr.App` calling `raise`, `failwith`, `failwithf`, `invalidArg`, `invalidOp`, `nullArg`. TAST confirms the call target is in the `Microsoft.FSharp.Core.Operators` or `System` namespace.
*   **Why it's C#-ish:** `throw new ArgumentException(...)` is C#'s standard guard pattern. It creates hidden control flow, makes the function's failure modes invisible, and forces callers to wrap everything in `try/catch`.
*   **Elite F# Solution:** Return `Result<'T, 'E>` with a descriptive error DU. The caller *must* handle the error case because it's in the type.
    ```fsharp
    // C#-ish
    let divide a b = if b = 0 then failwith "Division by zero" else a / b
    // Elite F#
    let divide a b = if b = 0 then Error DivisionByZero else Ok (a / b)
    ```
*   **Proposed Rule:** **FSA2022: Exception Throwing in Domain** — "`raise`/`failwith`/`invalidArg` in domain logic. Return `Result<'T, 'E>` to make failure explicit in the function signature."

### Smell: `try/finally` Without `use`

*   **Detection:** AST reveals `SynExpr.TryFinally` where the `finally` block calls `.Dispose()` or `.Close()`.
*   **Why it's C#-ish:** C# uses `try/finally` or `using` blocks. F# has the `use` keyword which is syntactically cleaner and semantically identical.
*   **Elite F# Solution:** `use resource = new SomeDisposable()` — auto-disposes at scope exit.
*   **Proposed Rule:** **FSA2023: Manual Dispose** — "`try/finally` with `.Dispose()` detected. Use F#'s `use` binding for automatic resource cleanup."

---

## 10. The Expression-Oriented / Statement Gap

In Haskell, *everything* is an expression. `if/then/else` returns a value. `case/of` returns a value. There are no "statements." OCaml is similar. F# is expression-oriented too, but C# developers write statement-style code: assign a mutable, mutate it in branches, return it at the end.

### Smell: Mutable Variable + Branch Assignment

*   **Detection:** AST reveals a `SynBinding` with `IsMutable = true`, followed by `SynExpr.IfThenElse` or `SynExpr.Match` where the branches contain `SynExpr.LongIdentSet` or `SynExpr.NamedIndexedPropertySet` assigning to that mutable variable, followed by a final reference to it.
*   **Why it's C#-ish:** This is the C# pattern:
    ```csharp
    string result;
    if (condition) result = "A";
    else result = "B";
    return result;
    ```
    In F#, `if/then/else` and `match` are *expressions* that return values. The mutable is entirely unnecessary.
*   **Elite F# Solution:**
    ```fsharp
    let result = if condition then "A" else "B"
    // or
    let result = match x with | CaseA -> "A" | CaseB -> "B"
    ```
*   **Proposed Rule:** **FSA2024: Statement-Style Branching** — "Mutable variable assigned inside branches and returned afterward. Use F#'s expression-oriented `if/then/else` or `match` to bind the result directly: `let result = if ... then ... else ...`."

### Smell: Ignoring Return Values (Discarding Expressions)

*   **Detection:** AST reveals `SynExpr.Sequential` where the first expression has a non-`unit` return type (resolved via TAST) and its value is discarded.
*   **Why it's C#-ish:** In C#, method calls are statements; you can ignore return values freely. In F#, discarding a non-`unit` value is a compiler warning (`FS0020`) for good reason — it usually indicates a bug (e.g., calling `List.map` but forgetting to bind the result).
*   **Elite F# Solution:** Bind the result with `let`, or explicitly discard with `ignore` if the side effect is intentional (and document why).
*   **Proposed Rule:** **FSA2025: Discarded Computation** — "Non-unit expression result is silently discarded. Bind it with `let` or explicitly `ignore` it with a comment explaining the intentional discard."

### Smell: Semicolon Sequencing of Pure Expressions

*   **Detection:** AST reveals `SynExpr.Sequential` with `SynExpr.App` calls to pure functions (no side effects per TAST) where the intermediate results are unused.
*   **Why it's C#-ish:** This is "do thing A; do thing B; do thing C;" statement sequencing. In a pure functional style, you compose: `a >> b >> c` or `x |> a |> b |> c`.
*   **Elite F# Solution:** Function composition (`>>`, `<<`) or pipeline (`|>`).
*   **Proposed Rule:** **FSA2026: Imperative Sequencing of Pure Functions** — "Multiple pure function calls sequenced with `;`. Compose them with `>>` or pipeline with `|>` for declarative data flow."

---

## 11. The Domain Modeling / Primitive Obsession Gap (Extended)

FSA2003 covers consecutive same-type primitives. This extends into the broader "Make Illegal States Unrepresentable" philosophy.

### Smell: `string` for Structured Domain Values

*   **Detection:** TAST reveals function parameters or record fields typed as `string` with names matching domain patterns: `email`, `url`, `uri`, `phone`, `zipCode`, `postalCode`, `ssn`, `isbn`, `sku`, `iban`, `currencyCode`, `countryCode`.
*   **Why it's C#-ish:** C# developers use `string` for everything because creating a wrapper type requires a class, constructor, validation, `Equals`, `GetHashCode`, `ToString` — 30 lines of boilerplate. In F#, a Single-Case DU is one line: `type Email = Email of string`.
*   **Elite F# Solution:** Single-Case DUs with smart constructors:
    ```fsharp
    type Email = private Email of string
    module Email =
        let create (s: string) : Result<Email, string> =
            if s.Contains "@" then Ok (Email s) else Error "Invalid email"
        let value (Email e) = e
    ```
*   **Proposed Rule:** **FSA2027: Stringly-Typed Domain Value** — "Domain value (`email`/`url`/`phone`/etc.) modeled as raw `string`. Wrap in a Single-Case DU with a validating smart constructor to make invalid values unrepresentable."

### Smell: Missing Units of Measure

*   **Detection:** TAST reveals `float`, `decimal`, or `int` parameters/fields with names suggesting physical or domain quantities: `distance`, `weight`, `price`, `temperature`, `duration`, `speed`, `latitude`, `longitude`, `balance`, `rate`, `percentage`.
*   **Why it's C#-ish:** C# has no units of measure, so `decimal price` and `decimal weight` are the same type. You can accidentally add them. F# has **Units of Measure** — a unique feature with no C# equivalent.
*   **Elite F# Solution:**
    ```fsharp
    [<Measure>] type kg
    [<Measure>] type m
    [<Measure>] type s
    let speed (d: float<m>) (t: float<s>) : float<m/s> = d / t
    ```
*   **Proposed Rule:** **FSA2028: Unmeasured Quantity** — "Numeric value representing a physical/domain quantity lacks a Unit of Measure. Annotate with `[<Measure>]` types to prevent unit-mismatch bugs at compile time."

### Smell: `DateTime` Without Timezone Discipline

*   **Detection:** TAST reveals `System.DateTime` usage (as opposed to `System.DateTimeOffset` or `NodaTime.Instant`).
*   **Why it's C#-ish:** `DateTime` is ambiguous — is it UTC? Local? Server time? This is a well-known C# footgun. Haskell's `time` library distinguishes `UTCTime`, `LocalTime`, `ZonedTime`, and `Day` as separate types.
*   **Elite F# Solution:** Use `DateTimeOffset` (or `NodaTime.Instant`) and wrap in a DU:
    ```fsharp
    type Timestamp = Utc of DateTimeOffset | Local of DateTimeOffset * TimeZoneInfo
    ```
*   **Proposed Rule:** **FSA2029: Ambiguous DateTime** — "`System.DateTime` used without timezone context. Use `DateTimeOffset` or a wrapping DU to distinguish UTC from local time at the type level."

### Smell: Boolean Parameter Pairs (Flag Arguments)

*   **Detection:** AST/TAST reveals a function with two or more `bool` parameters, especially with names like `isX`, `hasY`, `shouldZ`, `includeW`.
*   **Why it's C#-ish:** `ProcessOrder(order, true, false, true)` — what do those booleans mean? This is the C# "flag argument" anti-pattern. In Haskell, you'd use a record or a sum type for configuration.
*   **Elite F# Solution:** Replace with a configuration record or a DU:
    ```fsharp
    type OrderOptions = { IncludeTax: bool; GiftWrap: bool; ExpressShip: bool }
    // or
    type Shipping = | Standard | Express | Pickup
    ```
*   **Proposed Rule:** **FSA2030: Boolean Flag Parameters** — "Function accepts multiple `bool` parameters. Replace with a configuration record or Discriminated Union to make call sites self-documenting."

---

## 12. The Concurrency / Shared State Gap

Haskell's `IO` monad and STM (Software Transactional Memory) enforce structured concurrency. OCaml has `Lwt` and `Async` with explicit effect typing. F# has `Async<'T>` and `MailboxProcessor<'T>`, but C# developers bring `Thread`, `lock`, and shared mutable state.

### Smell: `lock` / `Monitor` / `Mutex` for Shared State

*   **Detection:** AST/TAST reveals calls to `lock`, `Monitor.Enter`, `Monitor.Exit`, `Mutex.WaitOne`, `SemaphoreSlim.Wait`, or `ReaderWriterLockSlim`.
*   **Why it's C#-ish:** Lock-based concurrency is the C#/Java default. It's deadlock-prone, non-compositional, and impossible to reason about locally. Haskell's STM provides composable, deadlock-free atomic transactions. F#'s `MailboxProcessor` (actor model) eliminates shared state entirely.
*   **Elite F# Solution:**
    *   **Actor model:** `MailboxProcessor<'Msg>` — no shared state, message-passing only.
    *   **STM-like:** `Atomic` computation expressions (via `FSharp.Control.Reactive` or custom).
    *   **Immutable + Async:** If data is immutable, no locks are needed.
*   **Proposed Rule:** **FSA2031: Lock-Based Concurrency** — "Explicit lock/mutex/monitor detected. Replace shared mutable state with `MailboxProcessor` (actor model) or ensure data immutability to eliminate the need for locks."

### Smell: `Thread` / `Thread.Sleep` / `Thread.Start`

*   **Detection:** TAST reveals `System.Threading.Thread` usage.
*   **Why it's C#-ish:** Raw thread management is the lowest-level C# concurrency primitive. It's unstructured, uncomposable, and error-prone. In Haskell, you use `forkIO` with `MVar`/`TVar`, or `async`/`wait`. In F#, `Async.Start` / `Async.Parallel` / `Async.Sleep` are the structured equivalents.
*   **Elite F# Solution:** `Async` workflows with `Async.Parallel`, `Async.Sequential`, `Async.Sleep`. For fire-and-forget, `Async.Start`. For coordination, `MailboxProcessor`.
*   **Proposed Rule:** **FSA2032: Raw Thread Management** — "`System.Threading.Thread` used directly. Use F#'s `Async` workflows (`Async.Start`, `Async.Parallel`, `Async.Sleep`) for structured, composable concurrency."

### Smell: `async { ... }` with `let!` on Non-Async (Unnecessary Wrapping)

*   **Detection:** AST reveals `SynExpr.ComputationExpr` with `async` CE where `let!` binds a value that TAST resolves as *not* being `Async<'T>` (i.e., the developer wrapped a pure value in `async { return x }` unnecessarily).
*   **Why it's C#-ish:** This is the C# `Task.FromResult(x)` habit — wrapping everything in a Task "just in case." It adds unnecessary overhead and obscures which operations are actually asynchronous.
*   **Elite F# Solution:** Only use `let!` for genuinely async operations. Use `let` for pure computations within the CE.
*   **Proposed Rule:** **FSA2033: Unnecessary Async Wrapping** — "Pure value wrapped in `async { return ... }` and bound with `let!`. Use `let` for synchronous computations inside the CE to clarify the async boundary."

---

## 13. The Generic Programming / SRTP Gap

Haskell's typeclasses enable ad-hoc polymorphism with zero runtime cost. OCaml's functors enable module-level parameterization. F# has **Statically Resolved Type Parameters (SRTP)** — a unique feature with no C# equivalent — but most F# developers (especially from C#) never use it.

### Smell: `interface` Constraint Where SRTP Would Suffice

*   **Detection:** AST reveals a generic parameter with `SynTypeConstraint.WhereTyp` constraining to an interface with 1–2 methods, where the interface is defined solely for this constraint (not a BCL interface like `IDisposable`).
*   **Why it's C#-ish:** In C#, you write `where T : ISerializable` because that's the only way to constrain generics. In F#, SRTP lets you constrain on *member signatures* without defining an interface:
    ```fsharp
    let inline serialize (x: ^T when ^T : (member ToJson : unit -> string)) = x.ToJson()
    ```
*   **Elite F# Solution:** SRTP inline constraints for structural typing, or Record of Functions for runtime dictionary passing.
*   **Proposed Rule:** **FSA2034: Interface Constraint Where SRTP Applies** — "Single-purpose interface used as a generic constraint. Consider SRTP (`^T when ^T : (member ...)`) for structural, inheritance-free polymorphism."

### Smell: `obj` Parameter Where Generic `'T` Would Work

*   **Detection:** TAST reveals a function parameter typed as `obj` where the function body only calls members available on a generic type (e.g., `.ToString()`, comparison, equality).
*   **Why it's C#-ish:** Pre-generics C# used `object` for everything. C# 2.0+ has generics, but the habit persists. In F#, `'T` is zero-cost and type-safe.
*   **Elite F# Solution:** Replace `obj` with `'T` and add constraints as needed.
*   **Proposed Rule:** **FSA2035: Object Parameter Where Generic Applies** — "`obj` parameter where a generic `'T` with constraints would preserve type safety. Replace with `'T` and appropriate `when` clauses."

### Smell: Not Using `inline` for Small Higher-Order Functions

*   **Detection:** AST reveals a `let` binding (not `let inline`) for a small function (body < 3 expressions) that takes a function parameter and is called in a performance-sensitive context (heuristic: called inside a `List.map`, `Seq.fold`, or tight loop).
*   **Why it's C#-ish:** In C#, every method call has overhead. In F#, `inline` enables the compiler to eliminate the call and specialize the function, similar to Haskell's `INLINE` pragma or C++ templates.
*   **Elite F# Solution:** Mark small, frequently-called higher-order functions as `inline`.
*   **Proposed Rule:** **FSA2036: Missing Inline on Hot HOF** — "Small higher-order function called in a tight loop without `inline`. Add `let inline` to enable specialization and eliminate call overhead."

---

## 14. The Module / Structure / Access Gap

Haskell organizes code into modules with explicit export lists. OCaml uses modules and signatures (`.mli` files) for encapsulation. F# has `module` and `namespace`, but C# developers bring `public`/`private`/`internal`/`protected` access modifier habits.

### Smell: Excessive Access Modifiers

*   **Detection:** AST reveals `SynAccess` annotations (`private`, `internal`, `public`) on more than 30% of bindings in a module, or `private` on every helper function.
*   **Why it's C#-ish:** In C#, everything defaults to `private` and you sprinkle `public` everywhere. In F#, module-level `let` bindings are public by default (like Haskell's module exports), and you use `private` sparingly. Over-modifiering signals "I'm structuring this like a C# class."
*   **Elite F# Solution:** Use F#'s default visibility. Use `[<AutoOpen>]` for utility modules. Use `private` only for truly internal helpers. Use module signatures (`.fsi` files) for explicit export control (like OCaml's `.mli`).
*   **Proposed Rule:** **FSA2037: Access Modifier Overuse** — "Excessive `private`/`internal`/`public` annotations in a module. Rely on F#'s default module visibility and use `.fsi` signature files for explicit API boundaries."

### Smell: `namespace` with Classes Instead of `module` with Functions

*   **Detection:** AST reveals `SynModuleOrNamespace` with `SynModuleDecl.Types` containing only classes with static methods, no instance state, and no inheritance.
*   **Why it's C#-ish:** C# requires `namespace` + `class` for all code. F# `module` is the direct equivalent and requires zero boilerplate.
*   **Elite F# Solution:** `module` with top-level `let` functions.
*   **Proposed Rule:** **FSA2038: Namespace-Class Instead of Module** — "`namespace` + static class used where an F# `module` would be idiomatic. Convert to `module` with top-level `let` bindings."

### Smell: Missing `[<RequireQualifiedAccess>]` on DUs

*   **Detection:** AST reveals `SynTypeDefn` with `SynTypeDefnRepr.Union` that lacks the `[<RequireQualifiedAccess>]` attribute, and the DU has cases with common names (e.g., `Success`, `Error`, `None`, `Some`, `Ok`, `Fail`).
*   **Why it's C#-ish:** Without `[<RequireQualifiedAccess>]`, DU cases pollute the namespace like C# enum values. `Success` could come from any DU. In Haskell, you use qualified imports or distinct constructor names. In OCaml, polymorphic variants or module scoping handles this.
*   **Elite F# Solution:** Add `[<RequireQualifiedAccess>]` to force `MyDU.Success` instead of bare `Success`.
*   **Proposed Rule:** **FSA2039: Unqualified DU Cases** — "Discriminated Union with common case names lacks `[<RequireQualifiedAccess>]`. Add the attribute to prevent namespace pollution and ambiguous case references."

---

## 15. The Point-Free / Composition Gap

Haskell's point-free style (`f = g . h . k`) and OCaml's function composition (`let f = g % h % k`) are natural. F# has `>>` and `<<` but C# developers nest calls or use lambdas everywhere.

### Smell: Lambda Where Point-Free Composition Works

*   **Detection:** AST reveals `SynExpr.Lambda` whose body is a single `SynExpr.App` chain that simply forwards the argument: `fun x -> f x`, `fun x -> g (f x)`, `fun x -> x |> f |> g`.
*   **Why it's C#-ish:** In C#, you write `x => Foo(x)` because there's no point-free style. In F#, `fun x -> f x` is just `f`. `fun x -> g (f x)` is `f >> g`.
*   **Elite F# Solution:**
    ```fsharp
    // C#-ish
    let process x = validate x |> transform x |> save x
    // Point-free
    let process = validate >> transform >> save
    ```
*   **Proposed Rule:** **FSA2040: Redundant Lambda (Eta Expansion)** — "`fun x -> f x` detected. This is η-expansion. Replace with `f` directly, or use `>>` / `<<` for composition chains."

### Smell: Deep Right-Nested Application Instead of Pipeline

*   **Detection:** AST reveals `SynExpr.App` nested 3+ levels deep: `f (g (h (k x)))`.
*   **Why it's C#-ish:** This is mathematical/C-style function application. F#'s `|>` operator exists specifically to flatten this into a readable top-to-bottom data flow.
*   **Elite F# Solution:** `x |> k |> h |> g |> f`
*   **Proposed Rule:** **FSA2041: Deep Application Nesting** — "Function application nested 3+ levels deep. Refactor to forward pipeline (`|>`) for top-to-bottom readability." *(Extends FSA2006 with a concrete depth threshold.)*

---

## Detection Feasibility Matrix

| Rule | AST | TAST | Text | Difficulty | False-Positive Risk |
|------|-----|------|------|------------|-------------------|
| FSA2001 (Nested Match) | ✅ `SynExpr.Match` nesting | ✅ Type is Option/Result | — | Medium | Low |
| FSA2002 (Small Interface) | ✅ `SynTypeDefnRepr.ObjectModel` | ✅ Method count | — | Low | Medium (legitimate interfaces) |
| FSA2003 (Primitive Obsession) | — | ✅ Param types | — | Medium | Medium (legitimate `int` params) |
| FSA2004 (Flag State Machine) | ✅ Record fields | ✅ Field types are `bool` | — | Medium | Medium |
| FSA2005 (Impure Core) | — | ✅ Return type + call graph | ✅ API names | **High** | **High** (heuristic) |
| FSA2006 (Nested App) | ✅ `SynExpr.App` depth | — | — | Low | Low |
| FSA2007 (If/Elif Chain) | ✅ `SynExpr.IfThenElse` depth | — | — | Low | Medium |
| FSA2008 (Enum) | ✅ `SynTypeDefnRepr.Enum` | — | — | **Trivial** | Low |
| FSA2009 (Wildcard on DU) | ✅ `SynPat.Wild` | ✅ Matched type is DU | — | Medium | Low |
| FSA2010 (obj usage) | — | ✅ Type is `obj` | — | **Trivial** | Low |
| FSA2012 (Mutable Collection) | — | ✅ Type resolves to BCL | — | **Trivial** | Low |
| FSA2014 (Mutable Accumulator) | ✅ `IsMutable` + `For` | — | — | Medium | Low |
| FSA2015 (Redundant Annotation) | ✅ `SynPat.Typed` | ✅ Inferred = annotated | — | **High** | Medium |
| FSA2016 (Unsafe Cast) | ✅ `SynExpr.Downcast` | — | — | **Trivial** | Low |
| FSA2021 (try/with) | ✅ `SynExpr.TryWith` | — | — | Low | Medium |
| FSA2024 (Mutable + Branch) | ✅ `IsMutable` + `IfThenElse` + `LongIdentSet` | — | — | Medium | Low |
| FSA2027 (Stringly Typed) | — | ✅ Type is `string` | ✅ Name heuristic | Medium | **High** |
| FSA2028 (No Units) | — | ✅ Type is `float`/`decimal` | ✅ Name heuristic | Medium | **High** |
| FSA2031 (Lock) | — | ✅ Call target | ✅ `lock` keyword | **Trivial** | Low |
| FSA2040 (Eta Expansion) | ✅ `SynExpr.Lambda` body | — | — | Low | Low |

---

## Implementation Priority Tiers

### Tier 1 — Trivial AST/TAST checks, zero false positives (Week 1)
FSA2008, FSA2010, FSA2012, FSA2016, FSA2031, FSA2040

### Tier 2 — Moderate AST walking, low false positives (Week 2–3)
FSA2001, FSA2006, FSA2009, FSA2014, FSA2021, FSA2024, FSA2041

### Tier 3 — TAST-dependent, moderate false positives (Week 4–6)
FSA2002, FSA2003, FSA2004, FSA2015, FSA2017, FSA2018, FSA2019, FSA2020

### Tier 4 — Heuristic / high false-positive risk, needs suppression system (Month 2+)
FSA2005, FSA2027, FSA2028, FSA2029, FSA2030, FSA2034, FSA2036

### Tier 5 — Requires whole-program analysis or cross-file context (Month 3+)
FSA2007 (needs domain understanding), FSA2033 (needs async flow analysis), FSA2037 (needs module-level statistics)

---

## Closing Principle

> **Every rule in this taxonomy answers one question: "Is this code exploiting F#'s algebraic, immutable, expression-oriented, type-inferred nature — or is it fighting it?"**
>
> The C#-ish path is always *available* in F#. The elite path is always *better*. FsAssay's job is to make the C#-ish path *visible*, *named*, and *shameful* — until the developer internalizes the functional mindset and the warnings go silent.
