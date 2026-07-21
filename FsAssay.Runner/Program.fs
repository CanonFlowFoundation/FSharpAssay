open System.IO
open FSharp.Analyzers.SDK
open FsAssay.Analyzers
open FSharp.Compiler.Text

let rec getFiles dir =
    seq {
        yield! Directory.GetFiles(dir, "*.fs")
        for d in Directory.GetDirectories(dir) do
            if not (d.Contains(".git") || d.Contains("obj") || d.Contains("bin")) then
                yield! getFiles d
    }

let scanFile file =
    let source = File.ReadAllText(file)
    let sourceText = SourceText.ofString source
    let context : CliContext = {
        FileName = file
        SourceText = sourceText
        ParseFileResults = Unchecked.defaultof<_>
        CheckFileResults = Unchecked.defaultof<_>
        TypedTree = None
        CheckProjectResults = Unchecked.defaultof<_>
        ProjectOptions = Unchecked.defaultof<_>
        AnalyzerIgnoreRanges = Map.empty
    }
    
    let violations = Rules.antiPatternAnalyzer context |> Async.RunSynchronously
    if not (List.isEmpty violations) then
        printfn "\n❌ %s" file
        for v in violations do
            printfn "   └── [%s] %s" v.Code v.Message

[<EntryPoint>]
let main argv =
    let path = if argv.Length > 0 then argv.[0] else "/root/fsharp-realworld"
    printfn "Scanning repository: %s" path
    getFiles path |> Seq.iter scanFile
    printfn "\nScan complete!"
    0
