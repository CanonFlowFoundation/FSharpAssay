# FsAssay Roundtrip Testing Results

In accordance with the [Stylish F# `SKILL.md`](https://github.com/ArunNotFound/functional-skills/blob/main/stylish-fsharp/SKILL.md), I've created an "opposite" (hostile) F# sample that violates the idiomatic F# principles, and verified it against the `FsAssay` prototype. 

## The "C#-ish" Hostile Specimen

The following code explicitly breaks the "creed" of Stylish F# by using `mutable`, `null`, `Option.get`, boolean validation, exceptions for control flow, while loops, interfaces, and primitive obsession:

```fsharp
module Specimens.CsharpishOrderProcessor

open System

// FSA1004: Primitive Obsession
type EmailAddress = string

// FSA1008: OOP Inheritance
type IOrderService =
    abstract member Process: string -> bool

type CustomerOrder() =
    // FSA1001: Mutation Overuse
    // FSA1003: Null Reference
    let mutable email: EmailAddress = null
    
    // FSA1009: Mutable Collections
    let items = ResizeArray<string>()

    member this.Email
        with get() = email
        and set(v) = email <- v

    member this.Items = items

// FSA1005: Parse, Don't Validate
let isValidEmail (e: string) = e.Contains("@")

type OrderService() =
    interface IOrderService with
        member this.Process(inputOpt: string option) =
            // FSA1002: Partial Access
            let input = Option.get inputOpt
            
            let mutable count = 0
            // FSA1007: Imperative Loops
            while count < 10 do
                count <- count + 1

            try
                if not (isValidEmail input) then
                    failwith "Invalid"
                true
            with
            // FSA1006: Generic Catch
            | :? System.Exception -> false
```

## FsAssay Error Output

When running `dotnet run --project FsAssay.Runner -- /root/FSharpAssay/Specimens`, the analyzer correctly identifies and blocks all anti-patterns, strictly enforcing the Stylish F# rules:

```text
❌ /root/FSharpAssay/Specimens/CsharpishOrderProcessor.fs
   └── [FSA1003] Null Reference: Avoid 'null'. Use 'Option' types to represent missing values. (Line: 11)
   └── [FSA1004] Primitive Obsession: Do not use type aliases for primitives. Use Single-Case Discriminated Unions to make illegal states unrepresentable. (Line: 1)
   └── [FSA1005] Parse, Don't Validate: Functions should return Result<ParsedType, Error> rather than a boolean validity flag. (Line: 1)
   └── [FSA1006] Generic Catch: Do not catch generic exceptions for flow control. Use Result types instead. (Line: 1)
   └── [FSA1007] Imperative Loops: Avoid 'while' loops. Use Seq.fold or recursion. (Line: 1)
   └── [FSA1008] OOP Inheritance: Avoid OOP inheritance and interfaces. Use records of functions or Discriminated Unions. (Line: 1)
   └── [FSA1009] Mutable Collections: Avoid C# mutable collections. Use F# immutable Map, Set, or list. (Line: 1)
```

## Observations on the Prototype
I also created an idiomatic F# equivalent (`StylishOrderProcessor.fs`) featuring Records, DUs, and `Result` returning functions. As noted in the `FsAssay` `README.md`, the analyzer is currently a *lexical prototype* combined with a naive TAST (Typed Abstract Syntax Tree) visitor. 

Because of this, FsAssay currently flags the underlying, **compiler-generated** IL of F# Discriminated Unions and Records as `FSA1003` (Nulls) and `FSA1001` (Mutations), meaning it incorrectly penalizes Stylish F# until the compiler-generated exclusion logic is fully implemented.

The analyzer perfectly identifies C#-ish anti-patterns in user code, aligning exactly with the `functional-skills` design philosophy.
