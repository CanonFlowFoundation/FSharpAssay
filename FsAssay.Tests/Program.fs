open Expecto
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open FSharp.Analyzers.SDK
open FsAssay.Analyzers
open System.IO

let checker = FSharpChecker.Create(keepAssemblyContents = true)

let runFsAssay (source: string) =
    let file = Path.Combine(Path.GetTempPath(), "Test.fs")
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

        testCase "Phase 4 Auto-Fix Remediation Test" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething (x: int option) =
    let v = x.Value
    v + 1
"""
            let results = runFsAssay sourceCode
            let violation = results |> List.find (fun m -> m.Code = "FSA1002")
            Expect.isFalse (List.isEmpty violation.Fixes) "Expected non-empty Fixes list for FSA1002"
            Expect.isTrue (violation.Fixes.[0].ToText.Contains("match opt with")) "Expected pattern match fix recommendation"
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv tests

