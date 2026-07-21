# fs-assay Demonstration: C#-ish F# vs. Stylish F#

## The Premise
Because large language models are trained on vastly more C# code than F# code, AI naturally gravitates toward generating "C#-ish F#". This means F# code that compiles perfectly fine but completely misses the point of the language—relying on mutable state, classes, nulls, exceptions for control flow, and partial functions.

**The Goal:** Write a hefty, domain-rich F# module written entirely in this "C#-ish" style, feed it to `fs-assay`, and prove that `fs-assay` definitively rejects it. Then, provide the idiomatic, stylish F# equivalent that `fs-assay` accepts.

---

## Part 1: The "C#-ish F#" Baseline (The Target)

We will build a domain model—for example, a **Customer Order Processing Pipeline**—that uses anti-patterns common to object-oriented backgrounds.

### Characteristics of the Hostile Code:
1. **Classes over Records/DUs:** Using explicit `class` definitions with `val mutable` fields instead of immutable records.
2. **Exception-Driven Control Flow:** Throwing `ArgumentException` or custom exceptions for validation failures instead of returning `Result`.
3. **Null Usage:** Returning `null` or using `Unchecked.defaultof<_>` to represent missing data instead of `Option`.
4. **Partial Functions:** Rampant use of `.Value`, `Option.get`, `Seq.head`, and `Map.find` without checking bounds.
5. **Synchronous Blocking:** Calling `.Result` or `.Wait()` on Tasks inside an `async` or `task` block.
6. **Primitive Obsession:** Using raw strings and integers for domain concepts (e.g., `string` for an Email, `decimal` for Order Total) instead of single-case Discriminated Unions.

### How `fs-assay` Disproves It:
When `fs-assay verify` runs on this codebase, it will fire deterministic failures:
* `FSA1001`: **Forbidden Null Literal** - Rejects the explicit use of `null`.
* `FSA1002`: **Partial Access** - Blocks `Option.get` and `.Value`.
* `FSA1003`: **Exception Control Flow** - Flags the throwing of validation exceptions.
* `FSA1101`: **Blocking Thread** - Catches `.Result` used on a Task.
* `FSA-Domain`: **Missing Domain Wrapper** - Flags primitives used where domain models are expected (enforced via custom domain rules).

---

## Part 2: The "Stylish F#" Resolution (The Goal)

Once `fs-assay` blocks the C#-ish code, the AI (or developer) is forced to refactor it into idiomatic F# to pass the assay.

### Characteristics of the Stylish Code:
1. **Algebraic Data Types:**
   ```fsharp
   type Email = private Email of string
   type OrderId = OrderId of Guid
   ```
2. **Discriminated Unions for State:**
   ```fsharp
   type OrderStatus = 
       | Pending of OrderData
       | Validated of ValidatedOrder
       | Shipped of TrackingInfo
       | Failed of Reason
   ```
3. **Railway Oriented Programming (ROP):**
   Using `Result<T, DomainError>` and computation expressions (`result { ... }`) to chain validation logic without ever throwing an exception.
4. **Total Functions:**
   Replacing `Option.get` with pattern matching (`match opt with | Some x -> ... | None -> ...`) or `Option.map`.
5. **Async Purity:**
   Properly awaiting tasks (`let! res = asyncCall()`) without blocking threads.

---

## Execution Plan for this Demonstration

1. **Create the `CsharpishOrderProcessor.fs`**: A fully compiling but highly anti-idiomatic F# file.
2. **Run `fs-assay`**: Capture the SARIF and JSON evidence showing exact rule violations.
3. **Refactor**: Create `StylishOrderProcessor.fs`.
4. **Run `fs-assay` again**: Show the clean bill of health and the generated `Pass` evidence bundle.

This demonstrates the exact value proposition of `fs-assay`: It forces AI (and humans) to write F# the way it was meant to be written, using the compiler and strict rules as an unbreakable gate.
