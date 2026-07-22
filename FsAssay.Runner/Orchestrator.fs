namespace FsAssay.Runner

open System.IO
open System.Diagnostics.CodeAnalysis
open FSharp.Analyzers.SDK
open FsAssay.Analyzers
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text

module Orchestrator =
    
    let checker = FSharpChecker.Create(keepAssemblyContents = true)
    
    let evaluateFile (options: FSharpProjectOptions) (file: string) = async {
        if not (File.Exists(file)) then return Skipped UnrelatedFile
        else
            let source = File.ReadAllText(file)
            let sourceText = SourceText.ofString source
            
            let! (parseResults, checkAnswer) = checker.ParseAndCheckFileInProject(file, 1, sourceText, options)
            
            match checkAnswer with
            | FSharpCheckFileAnswer.Aborted -> 
                return Failed (AnalyzerException "FSharpCheckFileAnswer.Aborted")
            | FSharpCheckFileAnswer.Succeeded(checkResults) ->
                if checkResults.HasFullTypeCheckInfo && checkResults.ImplementationFile.IsSome then
                    let context : CliContext = {
                        FileName = file
                        SourceText = sourceText
                        ParseFileResults = parseResults
                        CheckFileResults = checkResults
                        TypedTree = checkResults.ImplementationFile
                        CheckProjectResults = Unchecked.defaultof<_>
                        ProjectOptions = AnalyzerProjectOptions.BackgroundCompilerOptions options
                        AnalyzerIgnoreRanges = Map.empty
                    }
                    
                    try
                        let! violations = Rules.antiPatternAnalyzer context
                        return Completed violations
                    with e ->
                        return Failed (AnalyzerException e.Message)
                else
                    return Skipped CompilerErrors
    }

    [<SuppressMessage("FsAssay", "FSA2017")>]
    [<SuppressMessage("FsAssay", "FSA-C01")>]
    let evaluateSingleFile (file: string) = async {
        if not (File.Exists(file)) then return Skipped UnrelatedFile
        else
            let source = File.ReadAllText(file)
            let sourceText = SourceText.ofString source
            let! (optionsUnresolved, _) = checker.GetProjectOptionsFromScript(file, sourceText)
            let fsCore = typeof<option<int>>.Assembly.Location
            let sysLib = typeof<System.Object>.Assembly.Location
            let sysRuntime = typeof<System.Action>.Assembly.Location
            let options = { optionsUnresolved with OtherOptions = Array.append optionsUnresolved.OtherOptions [| "-r:" + fsCore; "-r:" + sysLib; "-r:" + sysRuntime |] }
            
            let! (parseResults, checkAnswer) = checker.ParseAndCheckFileInProject(file, 0, sourceText, options)
            match checkAnswer with
            | FSharpCheckFileAnswer.Aborted ->
                return Failed (AnalyzerException "FSharpCheckFileAnswer.Aborted")
            | FSharpCheckFileAnswer.Succeeded(checkResults) ->
                let context : CliContext = {
                    FileName = file
                    SourceText = sourceText
                    ParseFileResults = parseResults
                    CheckFileResults = checkResults
                    TypedTree = checkResults.ImplementationFile
                    CheckProjectResults = Unchecked.defaultof<_>
                    ProjectOptions = AnalyzerProjectOptions.BackgroundCompilerOptions options
                    AnalyzerIgnoreRanges = Map.empty
                }
                try
                    let! violations = Rules.antiPatternAnalyzer context
                    return Completed violations
                with e ->
                    return Failed (AnalyzerException e.Message)
    }
