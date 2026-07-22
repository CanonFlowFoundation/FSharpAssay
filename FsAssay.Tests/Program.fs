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
    let sysRuntime = typeof<System.Action>.Assembly.Location
    let options = { optionsUnresolved with OtherOptions = Array.append optionsUnresolved.OtherOptions [| "-r:" + fsCore; "-r:" + sysLib; "-r:" + sysRuntime |] }
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
    let y = Unchecked.defaultof<int>
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

        testCase "FSA1003: Null Reference" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let x: string = null
    x
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1003" results

        testCase "FSA1002: Partial Access" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let x = Some 5
    x.Value
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1002" results

        testCase "FSA1101: Blocking Call" <| fun _ ->
            let sourceCode = """
module BadCode
open System.Threading.Tasks
let doSomething () =
    let t = Task.Run(fun () -> ())
    t.Wait()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1101" results

        testCase "FSA1401: Async Start Unwrapped" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let a = async { return 1 }
    Async.RunSynchronously(a)
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1401" results

        testCase "Opus.md Requirement: No range0 in findings" <| fun _ ->
            let sourceCode = """
module BadCode
open System.Threading.Tasks
let doSomething () =
    let mutable x = 5
    let y: string = null
    let z = Some 5
    z.Value + x
"""
            let results = runFsAssay sourceCode
            let hasRange0 = results |> List.exists (fun m -> m.Range = Range.range0 || m.Range.StartLine = 0)
            Expect.isFalse hasRange0 "No finding should have range0"

        testCase "Opus.md Requirement: Phantom Nulls Deduplicated" <| fun _ ->
            let sourceCode = """
module DomainTypes
type CustomerId = CustomerId of System.Guid
type OptionTest = SomeCase | NoneCase
"""
            let results = runFsAssay sourceCode
            let fsa1003Count = results |> List.filter (fun m -> m.Code = "FSA1003") |> List.length
            Expect.equal fsa1003Count 0 "Expected 0 phantom FSA1003 violations on DU/Option structures"

        testCase "Opus.md Requirement: Dedup by Set" <| fun _ ->
            let sourceCode = """
module DupCode
let doSomething () =
    let mutable x = 5
    x <- 10
"""
            let results = runFsAssay sourceCode
            let fsa1001Count = results |> List.filter (fun m -> m.Code = "FSA1001") |> List.length
            // Only 2 expected (one on declaration, one on assignment) instead of multiple overlapping from reflection
            Expect.equal fsa1001Count 2 "Expected deduplicated findings"
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv tests
