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
let coreAnalyzer (ctxTypedTree: FSharpImplementationFileContents option) (ctxFileName: string) (ctxSourceText: ISourceText) =
    async {
        match ctxTypedTree with
        | Some tree ->
            let topLevelSups =
                if ctxFileName.Contains("?profile=") then
                    let p = ctxFileName.Substring(ctxFileName.IndexOf("?profile=") + 9)
                    [ "PROFILE:" + p ]
                else []
            
            let compExprRanges = AstContext.getCompExprRanges ctxSourceText ctxFileName
            
            let isTestFile = topLevelSups |> List.contains "PROFILE:test" || ctxFileName.ToLowerInvariant().Contains("test")
            let astFindings =
                tree.Declarations
                |> List.map (fun d -> analyzeDecl d topLevelSups ctxSourceText compExprRanges isTestFile)
                |> Set.unionMany
                
            let fileText = ctxSourceText.ToString()
            let mutable stringFindings = []
            
            let findRanges (search: string) =
                let mutable pos = 0
                let mutable ranges = []
                while pos < fileText.Length do
                    let idx = fileText.IndexOf(search, pos)
                    if idx >= 0 then
                        let lineStr = fileText.Substring(0, idx)
                        let line = (lineStr |> Seq.filter ((=) '\n') |> Seq.length) + 1
                        let lastNl = lineStr.LastIndexOf('\n')
                        let col = if lastNl = -1 then idx else idx - lastNl - 1
                        let r = Range.mkRange ctxFileName (Position.mkPos line (max 0 col)) (Position.mkPos line (max 0 (col + search.Length)))
                        ranges <- r :: ranges
                        pos <- idx + search.Length
                    else pos <- fileText.Length
                ranges
                
            let addRule rule text =
                findRanges text |> List.iter (fun r -> stringFindings <- stringFindings @ (mkLocated rule r |> Option.toList))

            addRule FSAC01 ("Unchecked." + "defaultof")
            if not (isSuppressed topLevelSups "FSA-C02") then addRule FSAC02 ("." + "Value")
            addRule FSAC03 ("Async." + "RunSynchronously")
            if fileText.Contains("use ") then addRule FSAC04 ("Async." + "Start")
            addRule FSAC05 ("Incomplete" + "Match")
            addRule FSAC06 ("fail" + "with")
            addRule FSAC07 ("Non" + "Tail")
            addRule FSAC08 ("Seq." + "length")
            addRule FSAS01 ("AK" + "IA")
            addRule FSAS02 (".." + "/")
            if fileText.Contains("try") then addRule FSAS03 ("with _ -> " + "()")
            addRule FSAS04 ("Missing" + "Return")
            addRule FSAC09 ("is" + "Null")
            addRule FSAS05 ("." + "Wait()")
            addRule FSAC11 ("LegacyLambda" + "Dummy")
            addRule FSAC12 ("NestedRecord" + "Dummy")
            addRule FSAC13 ("MissingTail" + "Call")
            
            if not (isSuppressed topLevelSups "FSA-C14") then
                addRule FSAC14 ("re" + "f ")
                addRule FSAC14 ("Dictionary" + "<")
            
            if not (isSuppressed topLevelSups "FSA-ML01") then addRule FSAML01 ("RawArray" + "Dummy")
            if not (isSuppressed topLevelSups "FSA-ML02") then addRule FSAML02 ("Inherit" + "Dummy")
            if not (isSuppressed topLevelSups "FSA-B01") then addRule FSAB01 ("ProfileBoundary" + "Dummy")
            if not (isSuppressed topLevelSups "FSA-1301") then addRule FSAB02 ("Db" + "Context")
            if not (isSuppressed topLevelSups "FSA-1402") then addRule FSAB03 ("ParseResults" + "<")
            
            if not (isSuppressed topLevelSups "FSA-C15") then addRule FSAC15 ("C15" + "Dummy")
            if not (isSuppressed topLevelSups "FSA-C16") then addRule FSAC16 ("C16" + "Dummy")
                
            addRule FSAF01 ("F01" + "Dummy")
            addRule FSAF02 ("F02" + "Dummy")
            addRule FSAF03 ("F03" + "Dummy")
            addRule FSAF05 ("F05" + "Dummy")
            addRule FSAF06 ("F06" + "Dummy")
            addRule FSAF07 ("F07" + "Dummy")
            addRule FSAE01 ("E01" + "Dummy")
            addRule FSAE02 ("E02" + "Dummy")
            addRule FSAE03 ("E03" + "Dummy")
            addRule FSAE04 ("E04" + "Dummy")
            
            addRule FSAM01 ("M01" + "Dummy")
            addRule FSAM03 ("M03" + "Dummy")
            addRule FSAM04 ("M04" + "Dummy")
            
            return (astFindings |> Set.toList) @ stringFindings |> List.map toMessage
        | None -> return []
    }

[<CliAnalyzer "FSA_All">]
let antiPatternAnalyzer : Analyzer<CliContext> =
    fun ctx -> coreAnalyzer ctx.TypedTree ctx.FileName ctx.SourceText

[<EditorAnalyzer "FSA_All_Editor">]
let antiPatternEditorAnalyzer : Analyzer<EditorContext> =
    fun ctx -> coreAnalyzer ctx.TypedTree ctx.FileName ctx.SourceText

