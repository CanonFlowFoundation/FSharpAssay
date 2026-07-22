open Expecto
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open FSharp.Analyzers.SDK
open FsAssay.Analyzers
open System.IO

let checker = FSharpChecker.Create(keepAssemblyContents = true)

let runFsAssay (source: string) =
    let file = Path.Combine(Path.GetTempPath(), sprintf "Test_%s.fs" (System.Guid.NewGuid().ToString("N")))
    File.WriteAllText(file, source)
    let sourceText = SourceText.ofString source
    let optionsUnresolved, _ = checker.GetProjectOptionsFromScript(file, sourceText) |> Async.RunSynchronously
    let fsCore = typeof<option<int>>.Assembly.Location
    let sysLib = typeof<System.Object>.Assembly.Location
    let options = { optionsUnresolved with OtherOptions = Array.append optionsUnresolved.OtherOptions [| "-r:" + fsCore; "-r:" + sysLib |] }
    let parseResults, checkAnswer = checker.ParseAndCheckFileInProject(file, 0, sourceText, options) |> Async.RunSynchronously
    match checkAnswer with
    | FSharpCheckFileAnswer.Succeeded(checkResults) ->
        let context : CliContext = {
            FileName = file
            SourceText = sourceText
            ParseFileResults = parseResults
            CheckFileResults = checkResults
            TypedTree = checkResults.ImplementationFile
            CheckProjectResults = Unchecked.defaultof<_>
            ProjectOptions = Unchecked.defaultof<_>
            AnalyzerIgnoreRanges = Map.empty
        }
        Rules.antiPatternAnalyzer context |> Async.RunSynchronously
    | FSharpCheckFileAnswer.Aborted -> 
        failwith "Failed to parse and check: Aborted"

let expectViolation code (messages: Message list) =
    let hasViolation = messages |> List.exists (fun m -> m.Code = code)
    Expect.isTrue hasViolation (sprintf "Expected %s to be triggered. Actual messages: %A" code (messages |> List.map (fun m -> m.Code)))

let tests =
    testList "Elite F# Anti-Pattern Tests" [
        testCase "FSA1001: Mutation Overuse" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let mutable x = 5
    x <- 10
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1001" results

        testCase "Suppression: SuppressMessage and Profile" <| fun _ ->
            let sourceCode = """
module OkCode
open System

type ProfileAttribute(name: string) =
    inherit Attribute()

type SuppressMessageAttribute(category: string, checkId: string) =
    inherit Attribute()

[<Profile("interop")>]
let doInterop () =
    let mutable x = 5 // Suppressed
    let y = Unchecked.defaultof<int> // Suppressed
    ()

[<SuppressMessage("FsAssay", "FSA1001")>]
let doSuppress () =
    let mutable y = 5 // Suppressed
    y <- 6 // Suppressed
    ()

let doNotSuppress () =
    let mutable z = 10 // NOT suppressed
    z <- 20 // NOT suppressed
    ()
"""
            let results = runFsAssay sourceCode
            let fsa1001Count = results |> List.filter (fun m -> m.Code = "FSA1001") |> List.length
            Expect.equal fsa1001Count 2 "Expected exactly 2 FSA1001 violations from doNotSuppress"
            let hasFSA1003 = results |> List.exists (fun m -> m.Code = "FSA1003" && m.Range.StartLine = 13)
            Expect.isFalse hasFSA1003 "Expected no FSA1003 due to interop profile"

        testCase "FSA1002: Partial Access (.Value)" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething (x: int option) =
    let v = x.Value
    v + 1
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1002" results
            
        testCase "FSA1002: Partial Access (Option.get)" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething (x: int option) =
    Option.get x
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1002" results

        testCase "FSA1003: Null Reference" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let x: string = null
    x
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1003" results

        testCase "FSA1004: Primitive Obsession" <| fun _ ->
            let sourceCode = """
module BadCode
type EmailAddress = string
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1004" results

        testCase "FSA1005: Parse, Don't Validate" <| fun _ ->
            let sourceCode = """
module BadCode
let isValidEmail (e: string) =
    e.Contains("@")
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1005" results

        testCase "FSA1006: Generic Catch" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    try
        failwith "error"
    with
    | :? System.Exception -> "caught"
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1006" results

        testCase "FSA1007: Imperative Loops" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let mutable i = 0
    while i < 10 do
        i <- i + 1
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1007" results

        testCase "FSA1008: OOP Inheritance" <| fun _ ->
            let sourceCode = """
module BadCode
type IAnimal =
    abstract member Speak: unit -> string
type Dog() =
    interface IAnimal with
        member this.Speak() = "Woof"
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1008" results

        testCase "FSA1009: Mutable Collections" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let list = ResizeArray<int>()
    list.Add(5)
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1009" results

        testCase "FSA1101: Async Blocking" <| fun _ ->
            let sourceCode = """
module BadCode
let fetch () = async { return 42 }
let doBlocking () =
    let res = fetch () |> Async.RunSynchronously
    res
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1101" results

        testCase "FSA1201: Unbounded Materialization" <| fun _ ->
            let sourceCode = """
module BadCode
open System.Collections.Generic
let processStream (s: IEnumerable<int>) =
    Seq.initInfinite (fun i -> i) |> Seq.toList
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1201" results

        testCase "FSA1301: EF Core Scope Leak" <| fun _ ->
            let sourceCode = """
module Domain
open Microsoft.EntityFrameworkCore
type MyDbContext() = class end
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1301" results

        testCase "FSA1401: Unbounded Async Start" <| fun _ ->
            let sourceCode = """
module BadCode
let doStart () =
    Async.Start (async { return () })
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1401" results

        testCase "GoF Strategy Pattern: OOP Class vs Higher-Order Function" <| fun _ ->
            let oopStrategy = """
module BadStrategy
type IPaymentStrategy =
    abstract member Pay: decimal -> string

type CreditCardStrategy() =
    interface IPaymentStrategy with
        member this.Pay(amount) = sprintf "Paid %f via Credit Card" amount
"""
            let idiomaticStrategy = """
module GoodStrategy
type PaymentStrategy = decimal -> string
let payWithCreditCard (amount: decimal) = sprintf "Paid %f via Credit Card" amount
let executePayment (strategy: PaymentStrategy) amount = strategy amount
"""
            let oopResults = runFsAssay oopStrategy
            expectViolation "FSA1008" oopResults
            let goodResults = runFsAssay idiomaticStrategy
            Expect.isEmpty goodResults (sprintf "Idiomatic strategy should have zero violations, but got: %A" (goodResults |> List.map (fun m -> m.Code)))

        testCase "GoF State Pattern: OOP State vs Discriminated Union" <| fun _ ->
            let oopState = """
module BadState
type IOrderState =
    abstract member Process: unit -> string
type PendingState() =
    interface IOrderState with
        member this.Process() = "Pending"
"""
            let idiomaticState = """
module GoodState
type OrderState =
    | Pending
    | Processing of step: int
    | Completed of orderId: string

let processOrder state =
    match state with
    | Pending -> "Starting"
    | Processing step -> sprintf "Step %d" step
    | Completed id -> sprintf "Done %s" id
"""
            let oopResults = runFsAssay oopState
            expectViolation "FSA1008" oopResults
            let goodResults = runFsAssay idiomaticState
            Expect.isEmpty goodResults (sprintf "Idiomatic state should have zero violations, but got: %A" (goodResults |> List.map (fun m -> m.Code)))

        testCase "GoF Command Pattern: OOP Class vs Parameterless Function / DU" <| fun _ ->
            let oopCommand = """
module BadCommand
type ICommand =
    abstract member Execute: unit -> unit
type SaveCommand() =
    interface ICommand with
        member this.Execute() = ()
"""
            let idiomaticCommand = """
module GoodCommand
type Command =
    | SaveData of path: string
    | ResetState

let execute cmd =
    match cmd with
    | SaveData path -> printfn "Saving to %s" path
    | ResetState -> printfn "Resetting"
"""
            let oopResults = runFsAssay oopCommand
            expectViolation "FSA1008" oopResults
            let goodResults = runFsAssay idiomaticCommand
            Expect.isEmpty goodResults (sprintf "Idiomatic command should have zero violations, but got: %A" (goodResults |> List.map (fun m -> m.Code)))

        testCase "GoF Decorator Pattern: OOP Wrapper Class vs Function Composition" <| fun _ ->
            let oopDecorator = """
module BadDecorator
type IService =
    abstract member Run: string -> string
type LoggingDecorator(inner: IService) =
    interface IService with
        member this.Run(x) =
            printfn "Logging %s" x
            inner.Run(x)
"""
            let idiomaticDecorator = """
module GoodDecorator
let runCore x = sprintf "Hello %s" x
let logWrapper f x =
    printfn "Logging %s" x
    f x
let decoratedRun = logWrapper >> runCore
"""
            let oopResults = runFsAssay oopDecorator
            expectViolation "FSA1008" oopResults
            let goodResults = runFsAssay idiomaticDecorator
            Expect.isEmpty goodResults (sprintf "Idiomatic decorator should have zero violations, but got: %A" (goodResults |> List.map (fun m -> m.Code)))

        testCase "GoF Builder/Factory Pattern: OOP Factory vs Constructor Functions" <| fun _ ->
            let oopFactory = """
module BadFactory
type IWidgetFactory =
    abstract member CreateWidget: unit -> string
type StandardWidgetFactory() =
    interface IWidgetFactory with
        member this.CreateWidget() = "Widget"
"""
            let idiomaticFactory = """
module GoodFactory
type Widget = { Name: string; Weight: float }
module Widget =
    let create name weight = { Name = name; Weight = weight }
"""
            let oopResults = runFsAssay oopFactory
            expectViolation "FSA1008" oopResults
            let goodResults = runFsAssay idiomaticFactory
            Expect.isEmpty goodResults (sprintf "Idiomatic factory should have zero violations, but got: %A" (goodResults |> List.map (fun m -> m.Code)))

        testCase "Functional-First Profile: interop (Permits null, mutable, ResizeArray)" <| fun _ ->
            let sourceCode = """
module InteropBoundary
open System

type ProfileAttribute(name: string) = inherit Attribute()

[<Profile("interop")>]
let interopBridge () =
    let mutable buffer = ResizeArray<int>() // Permitted under interop
    let rawObj: string = null // Permitted under interop
    buffer.Add(10)
    rawObj
"""
            let results = runFsAssay sourceCode
            Expect.isEmpty results "Interop profile should permit local mutability, null, and ResizeArray for C# bridge"

        testCase "Functional-First Profile: shell (Permits EF Core & Persistence)" <| fun _ ->
            let sourceCode = """
module PersistenceShell
open System
open Microsoft.EntityFrameworkCore

type ProfileAttribute(name: string) = inherit Attribute()

type DbContext() = class end

[<Profile("shell")>]
let saveToDb () =
    let ctx = new DbContext()
    ()
"""
            let results = runFsAssay sourceCode
            Expect.isEmpty results "Shell profile should permit EF Core persistence infrastructure"

        testCase "Functional-First Profile: script (Permits Async.RunSynchronously & Loops)" <| fun _ ->
            let sourceCode = """
module ScriptRunner
open System

type ProfileAttribute(name: string) = inherit Attribute()

[<Profile("script")>]
let runScript () =
    let mutable i = 0
    while i < 5 do i <- i + 1
    let task = async { return 100 }
    let res = task |> Async.RunSynchronously
    res
"""
            let results = runFsAssay sourceCode
            Expect.isEmpty results "Script profile should permit imperative loops and synchronous blocking"

        testCase "Functional-First Profile: performance (Permits local hot-path mutability)" <| fun _ ->
            let sourceCode = """
module HotPath
open System

type ProfileAttribute(name: string) = inherit Attribute()

[<Profile("performance")>]
let computeFast () =
    let mutable total = 0
    total <- total + 5
    total
"""
            let results = runFsAssay sourceCode
            Expect.isEmpty results "Performance profile should permit measured local mutability in hot paths"

        testCase "Functional-First Profile: core (Strict zero-tolerance functional purity)" <| fun _ ->
            let sourceCode = """
module CoreDomain
open System

type ProfileAttribute(name: string) = inherit Attribute()

[<Profile("core")>]
let domainLogic () =
    let mutable total = 0 // Strictly blocked in core
    total <- total + 5
    total
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1001" results

        testCase "Qwen Scrutiny: Clean Idiomatic F# produces ZERO violations" <| fun _ ->
            let sourceCode = """
module CleanDomain
type Email = Email of string
type Order = { Id: string; Total: decimal }
let processOrder (order: Order) : Result<Order, string> =
    if order.Total > 0.0m then Ok order else Error "Invalid total"
"""
            let results = runFsAssay sourceCode
            Expect.isEmpty results "Clean idiomatic F# domain logic must produce zero violations"

        testCase "Qwen Scrutiny: Comments & String Literals containing keywords do NOT trigger violations" <| fun _ ->
            let sourceCode = """
module SafeLiterals
// This data structure is immutable, not mutable
// We do not return null here, null is not allowed
let doc = "Warning: while loops and Option.get are not recommended"
"""
            let results = runFsAssay sourceCode
            let fsa1001 = results |> List.filter (fun m -> m.Code = "FSA1001")
            let fsa1003 = results |> List.filter (fun m -> m.Code = "FSA1003")
            Expect.isEmpty fsa1001 "Comment containing 'mutable' must not trigger FSA1001"
            Expect.isEmpty fsa1003 "Comment or string containing 'null' must not trigger FSA1003"

        testCase "Qwen Scrutiny: Exact source locations & multiple violations granularity" <| fun _ ->
            let sourceCode = """
module MultiBad
let firstBad () =
    let mutable a = 1
    a <- 2
let secondBad () =
    let mutable b = 3
    b <- 4
"""
            let results = runFsAssay sourceCode
            let fsa1001List = results |> List.filter (fun m -> m.Code = "FSA1001")
            Expect.isTrue (fsa1001List.Length >= 2) "Multiple mutable violations in the same file must be reported individually"
            for v in fsa1001List do
                Expect.isTrue (v.Range.StartLine > 0) "Violation Range must report actual line number (> 0), not range0 / line 0"

        testCase "Qwen Scrutiny: Dogfooding FsAssay own source code" <| fun _ ->
            let domainSource = File.ReadAllText("/root/fsharp/FsAssay.Runner/Domain.fs")
            let results = runFsAssay domainSource
            Expect.isEmpty results "FsAssay's own Domain.fs must pass its own audit with zero violations"

        testCase "FSA-C01: Unchecked.defaultof in Non-Interop Code" <| fun _ ->
            let sourceCode = """
module BadDefault
let doDefault () = Unchecked.defaultof<int>
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C01" results

        testCase "FSA-C02: Option.get / .Value Without Guard" <| fun _ ->
            let sourceCode = """
module BadOpt
let getVal (x: int option) = x.Value
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C02" results

        testCase "FSA-C03: Async.RunSynchronously in Library Code" <| fun _ ->
            let sourceCode = """
module BadLibrary
let run () = Async.RunSynchronously (async { return 1 })
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C03" results

        testCase "FSA-C04: IDisposable Disposed Before Async Runs" <| fun _ ->
            let sourceCode = """
module BadDispose
open System.IO
let leak () =
    use ms = new MemoryStream()
    Async.Start (async { return () })
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C04" results

        testCase "FSA-C05: Incomplete Pattern Match on DU" <| fun _ ->
            let sourceCode = """
module BadMatch
type MyDU = A | B
let test x = match x with A -> 1
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C05" results

        testCase "FSA-C06: failwith / raise in Public API" <| fun _ ->
            let sourceCode = """
module BadPublicApi
let publicFunction x = if x < 0 then failwith "invalid" else x
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C06" results

        testCase "FSA-C07: Non-Tail Recursion in let rec" <| fun _ ->
            let sourceCode = """
module BadRec
let rec sum n = if n <= 0 then 0 else 1 + sum (n - 1)
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C07" results

        testCase "FSA-C08: Seq.length on Infinite Sequences" <| fun _ ->
            let sourceCode = """
module BadInfinite
let getLen () = Seq.initInfinite (fun i -> i) |> Seq.length
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C08" results

        testCase "FSA-S01: Hard-Coded Credentials / Secrets" <| fun _ ->
            let sourceCode = """
module BadSecret
let apiKey = "AKIAIOSFODNN7EXAMPLE"
let secretKey = "password=SuperSecretPassword123"
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S01" results

        testCase "FSA-S02: Path Traversal in File Operations" <| fun _ ->
            let sourceCode = """
module BadPath
open System.IO
let readFile userPath = File.ReadAllText(Path.Combine("/var/data", "../secret.txt"))
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S02" results

        testCase "FSA-S03: Swallowed Exceptions" <| fun _ ->
            let sourceCode = """
module BadSwallow
let doSwallow () = try failwith "err" with _ -> ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S03" results

        testCase "FSA-S04: async Missing return / return!" <| fun _ ->
            let sourceCode = """
module BadAsync
let asyncNoReturn () = async { printfn "hello" }
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S04" results

        testCase "FSA-S05: Task.Result / .Wait() Blocking Calls" <| fun _ ->
            let sourceCode = """
module BadTask
open System.Threading.Tasks
let block (t: Task<int>) = t.Result
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S05" results

        testCase "Enterprise Workout: processOrder C#-shaped F# detection" <| fun _ ->
            let sourceCode = """
module EnterpriseWorkout
open System

type Item = { Price: decimal }
type Customer = { Items: Item list }
type Repository = { find: string -> Customer option }

let processOrder (repository: Repository) (orderId: string) (customerId: string) =
    let customer = repository.find customerId |> Option.get
    let mutable total = 0m

    for item in customer.Items do
        total <- total + item.Price

    total
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1001" results
            expectViolation "FSA1002" results
            expectViolation "FSA-C02" results
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv tests






