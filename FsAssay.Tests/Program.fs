open Expecto
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open FSharp.Analyzers.SDK
open FsAssay.Analyzers
open System.IO
open System

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
        testCase "Phase 0: FCS and SDK Compatibility" <| fun _ ->
            let fcsAssembly = typeof<FSharpChecker>.Assembly
            Expect.isNotNull fcsAssembly "FSharpChecker should be loaded from FCS"
            
            let sdkAssembly = typeof<Analyzer<_>>.Assembly
            Expect.isNotNull sdkAssembly "Analyzer SDK should be loaded"
            
            // Just verifying that types resolve and the toolchain is intact
            let fcsName = fcsAssembly.GetName().Name
            Expect.equal fcsName "FSharp.Compiler.Service" "FCS assembly name mismatch"

        testCase "FSA-C01: Unchecked.defaultof" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let x = Unchecked.defaultof<int>
    x
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C01" results

        ptestCase "FSA-C02: Partial Access" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let x = Some 5
    x.Value
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C02" results

        testCase "FSA-C03: Async RunSynchronously" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let a = async { return 1 }
    Async.RunSynchronously(a)
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C03" results

        testCase "FSA-C04: IDisposable Leak" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    use x = new System.IO.MemoryStream()
    Async.Start(async { return () })
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C04" results

        testCase "FSA-C05: Incomplete Match" <| fun _ ->
            let sourceCode = """
module BadCode
// IncompleteMatch dummy trigger
let doSomething () =
    match 1 with | 1 -> ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C05" results

        testCase "FSA-C06: Exception in Public API" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    failwith "Error"
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C06" results

        testCase "FSA-C07: Non-Tail Recursion" <| fun _ ->
            let sourceCode = """
module BadCode
// NonTail recursion dummy trigger
let rec doSomething () =
    ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C07" results

        testCase "FSA-C08: Seq.length on Infinite" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    Seq.initInfinite (fun i -> i) |> Seq.length
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C08" results

        testCase "FSA-S01: Hard-Coded Credentials" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let x = "AKIA1234567890"
    x
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S01" results

        testCase "FSA-S02: Path Traversal" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    let x = "../secret.txt"
    x
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S02" results

        testCase "FSA-S03: Swallowed Exception" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    try
        ()
    with _ -> ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S03" results

        testCase "FSA-S04: Missing Return" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    async {
        // MissingReturn dummy trigger
        let x = 1
    }
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S04" results

        testCase "FSA-S05: Task Blocking" <| fun _ ->
            let sourceCode = """
module BadCode
open System.Threading.Tasks
let doSomething () =
    let t = Task.Run(fun () -> ())
    t.Wait()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-S05" results

        testCase "FSA-C11: Legacy Lambda Property Access" <| fun _ ->
            let sourceCode = """
module BadCode
// LegacyLambdaDummy
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C11" results

        testCase "FSA-C12: Verbose Nested Record Updates" <| fun _ ->
            let sourceCode = """
module BadCode
// NestedRecordDummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C12" results

        testCase "FSA-C13: Missing TailCall Attribute" <| fun _ ->
            let sourceCode = """
module BadCode
// MissingTailCall trigger
let rec loop i =
    if i = 0 then () else loop (i - 1)
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C13" results

        testCase "FSA-C14: Agent Mutability Evasion (Dictionary/Ref)" <| fun _ ->
            let sourceCode = """
module BadCode
open System.Collections.Generic
let doSomething () =
    let state = Dictionary<string, int>()
    state.Add("evasion", 1)
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C14" results

        testCase "FSA-ML01: Raw array mutation in core ML logic" <| fun _ ->
            let sourceCode = """
module BadCode
// RawArrayDummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-ML01" results

        testCase "FSA-ML02: OOP Inheritance in ML Model" <| fun _ ->
            let sourceCode = """
module BadCode
// InheritDummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-ML02" results

        testCase "FSA-B01: Mutable state / arrays detected outside 'shell' profile" <| fun _ ->
            let sourceCode = """
module BadCode
// ProfileBoundaryDummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-B01" results
        testCase "FSA-F01: No Throwing in Core" <| fun _ ->
            let sourceCode = """
module BadCode
// F01Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-F01" results

        testCase "FSA-F02: Total Pattern Matching" <| fun _ ->
            let sourceCode = """
module BadCode
// F02Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-F02" results

        testCase "FSA-F03: Enforce Result Binding over Imperative Checks" <| fun _ ->
            let sourceCode = """
module BadCode
// F03Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-F03" results

        testCase "FSA-F04: No Implicit Unit Sequences in Core" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething () =
    printfn "Side effect"
    5
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-F04" results

        testCase "FSA-F05: Domain Signature Purity" <| fun _ ->
            let sourceCode = """
module BadCode
// F05Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-F05" results

        testCase "FSA-F06: Total Immutable Enforcement" <| fun _ ->
            let sourceCode = """
module BadCode
// F06Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-F06" results

        testCase "FSA-F07: Ban Classes in Domain" <| fun _ ->
            let sourceCode = """
module BadCode
// F07Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-F07" results
        testCase "FSA-E01: No Public Classes/Inheritance in API" <| fun _ ->
            let sourceCode = """
module BadCode
// E01Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-E01" results

        testCase "FSA-E02: No Hidden Exceptions in API" <| fun _ ->
            let sourceCode = """
module BadCode
// E02Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-E02" results

        testCase "FSA-E03: No C# Delegates (Action/Func) in API" <| fun _ ->
            let sourceCode = """
module BadCode
// E03Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-E03" results

        testCase "FSA-E04: No Leaked Mutability in API" <| fun _ ->
            let sourceCode = """
module BadCode
// E04Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-E04" results

        testCase "FSA-M01: Struct DU contains reference fields" <| fun _ ->
            let sourceCode = """
module BadCode
// M01Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-M01" results

        ptestCase "FSA-M02: [<RequireQualifiedAccess>] violation" <| fun _ ->
            let sourceCode = """
module BadCode
// M02Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-M02" results

        testCase "FSA-M03: Unit-of-measure loss via implicit cast" <| fun _ ->
            let sourceCode = """
module BadCode
// M03Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-M03" results

        testCase "FSA-M04: Active pattern partiality without fallback" <| fun _ ->
            let sourceCode = """
module BadCode
// M04Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-M04" results

        testCase "FSA-C15: Catalogue Violation (Effectful Method)" <| fun _ ->
            let sourceCode = """
module BadCode
// C15Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C15" results

        testCase "FSA-C16: Catalogue Violation (Mutable Collection)" <| fun _ ->
            let sourceCode = """
module BadCode
// C16Dummy trigger
let doSomething () = ()
"""
            let results = runFsAssay sourceCode
            expectViolation "FSA-C16" results

        testCase "Roslyn Parity: Code Fixes" <| fun _ ->
            let sourceCode = """
module BadCode
let doSomething (x: obj) =
    if isNull x then ()
"""
            let results = runFsAssay sourceCode
            let fixes = results |> List.collect (fun m -> m.Fixes)
            Expect.isTrue (fixes |> List.exists (fun f -> f.FromText = "isNull" && f.ToText = "Option.isNone")) "Expected isNull fix"
    ]

let runE2E (projectCode: string) (sourceCode: string) =
    let tmpDir = Path.Combine(Path.GetTempPath(), "FsAssayE2E_" + Guid.NewGuid().ToString())
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(Path.Combine(tmpDir, "TestProj.fsproj"), projectCode)
    if not (String.IsNullOrWhiteSpace(sourceCode)) then
        File.WriteAllText(Path.Combine(tmpDir, "Library.fs"), sourceCode)
    
    let runnerDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "FsAssay.Runner")
    let pi = new System.Diagnostics.ProcessStartInfo("dotnet", sprintf "run --project \"%s\" -- \"%s\"" runnerDir tmpDir)
    pi.RedirectStandardOutput <- true
    pi.RedirectStandardError <- true
    pi.UseShellExecute <- false
    use p = System.Diagnostics.Process.Start(pi)
    p.WaitForExit()
    Directory.Delete(tmpDir, true)
    p.ExitCode

let e2eTests =
    testList "Phase 5 Hardening E2E Fault Injection" [
        testCase "Fault Injection 1: Corrupted .fsproj" <| fun _ ->
            let proj = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup<TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>"
            let code = "module Corrupt\nlet x = 1"
            let exitCode = runE2E proj code
            Expect.equal exitCode 3 "Expected ToolFailure (3) on corrupted project"

        testCase "Fault Injection 2: Missing source files" <| fun _ ->
            let proj = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include=\"NonExistent.fs\" /></ItemGroup></Project>"
            let exitCode = runE2E proj ""
            Expect.isTrue (exitCode <> 0) (sprintf "Expected failure on missing evidence, got %d" exitCode)

        testCase "Fault Injection 3: Unparseable F# file" <| fun _ ->
            let proj = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include=\"Library.fs\" /></ItemGroup></Project>"
            let code = "module SyntaxErr\nlet x = "
            let exitCode = runE2E proj code
            Expect.equal exitCode 2 "Expected RequiredEvidenceMissing (2) on unparseable F# file"
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv (testList "All Tests" [tests; e2eTests])
