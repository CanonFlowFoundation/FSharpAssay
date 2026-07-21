namespace FsAssay.Runner

open FSharp.Analyzers.SDK
open FsAssay.Analyzers
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text

module Orchestrator =
    
    let checker = FSharpChecker.Create(keepAssemblyContents = true)
    
    let evaluateFile (options: FSharpProjectOptions) (file: string) = async {
        if not (System.IO.File.Exists(file)) then return Skipped UnrelatedFile
        else
            let source = System.IO.File.ReadAllText(file)
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
                        ProjectOptions = Unchecked.defaultof<_>
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
