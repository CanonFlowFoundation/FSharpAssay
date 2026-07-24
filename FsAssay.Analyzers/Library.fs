module FsAssay.Analyzers.Library

open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open System

open FsAssay.Analyzers.Domain
open FsAssay.Analyzers.Suppression
open FsAssay.Analyzers.AstUtils
open FsAssay.Analyzers.Visitor

[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-F04")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C01")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C03")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C06")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C08")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S03")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C09")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C10")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S05")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C14")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-1301")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-F04")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C01")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C03")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C06")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C08")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S03")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C09")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C10")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S05")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C14")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-1301")>]
let coreAnalyzer (ctxTypedTree: FSharpImplementationFileContents option) (ctxFileName: string) (ctxSourceText: ISourceText) (profile: Profile) =
    async {
        match ctxTypedTree with
        | Some tree ->
            let topLevelSups = []
            
            let compExprRanges = AstContext.getCompExprRanges ctxSourceText ctxFileName
            
            let isTestFile = topLevelSups |> List.contains "PROFILE:test" || ctxFileName.ToLowerInvariant().Contains("test")
            let astFindings =
                tree.Declarations
                |> List.map (fun d -> analyzeDecl d topLevelSups ctxSourceText compExprRanges isTestFile)
                |> Set.unionMany
            return (astFindings |> Set.toList) |> List.map toMessage
        | None -> return []
    }

[<CliAnalyzer "FSA_All">]
let antiPatternAnalyzer : Analyzer<CliContext> =
    fun ctx -> coreAnalyzer ctx.TypedTree ctx.FileName ctx.SourceText Core

[<EditorAnalyzer "FSA_All_Editor">]
let antiPatternEditorAnalyzer : Analyzer<EditorContext> =
    fun ctx -> coreAnalyzer ctx.TypedTree ctx.FileName ctx.SourceText Core

