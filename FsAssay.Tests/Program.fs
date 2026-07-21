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
        Rules.partialAccessAnalyzer context |> Async.RunSynchronously
    | FSharpCheckFileAnswer.Aborted -> 
        failwith "Failed to parse and check: Aborted"

let tests =
    testList "FSA1002 Tests" [
        testCase "fs-assay detects Option.get and fails the build" <| fun _ ->
            // Arrange
            let sourceCode = """
module BadCode
let doSomething (x: int option) =
    let v = Option.get x
    v + 1
"""
            // Act
            let results = runFsAssay sourceCode
            
            // Assert
            let hasPartialAccessViolation = results |> List.exists (fun r -> r.Type = "FSA1002")
            Expect.isTrue hasPartialAccessViolation "Expected FSA1002 (Partial Access) to be triggered by .Value"
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv tests
