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
    let options, _ = checker.GetProjectOptionsFromScript(file, sourceText) |> Async.RunSynchronously
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
    x
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA1001" results

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
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv tests
