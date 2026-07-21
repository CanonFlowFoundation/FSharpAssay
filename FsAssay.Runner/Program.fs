open System
open System.IO
open FsAssay.Runner
open FSharp.Analyzers.SDK
open Argu

type Arguments =
    | [<MainCommand; Last>] Target of path:string
    | [<AltCommandLine("-j")>] Out_Json of path:string
    | [<AltCommandLine("-s")>] Out_Sarif of path:string
    | [<AltCommandLine("-t")>] Out_Toolchain of path:string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Target _ -> "Target directory or file to scan."
                | Out_Json _ -> "Output file path for canonical JSON."
                | Out_Sarif _ -> "Output file path for SARIF."
                | Out_Toolchain _ -> "Output file path for toolchain record."

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "fsassay")
    let results =
        try
            parser.ParseCommandLine argv
        with e ->
            printfn "%s" e.Message
            Environment.Exit(ExitCodes.InvalidInvocation)
            failwith ""

    let path = results.GetResult(Target, defaultValue = Directory.GetCurrentDirectory())
    printfn "Cracking projects in: %s" path
    
    let optionsList = ProjectSystem.getTargetProjects path
    if List.isEmpty optionsList then
        printfn "No projects found."
        ExitCodes.InvalidInvocation
    else
        let mutable totalViolations = 0
        let mutable totalFiles = 0
        let mutable failedFiles = 0
        let mutable skippedFiles = 0
        let allResults = ResizeArray<string * Message list>()

        for options in optionsList do
            for file in options.SourceFiles do
                if file.EndsWith(".fs") && not (file.Contains("AssemblyAttributes.fs")) && not (file.Contains("AssemblyInfo.fs")) then
                    totalFiles <- totalFiles + 1
                    let verdict = Orchestrator.evaluateFile options file |> Async.RunSynchronously
                    match verdict with
                    | Completed violations ->
                        if not (List.isEmpty violations) then
                            totalViolations <- totalViolations + violations.Length
                            allResults.Add(file, violations)
                            printfn "\n❌ %s" file
                            for v in violations do
                                printfn "   └── [%s] %s (Line: %d)" v.Code v.Message v.Range.StartLine
                    | Skipped reason ->
                        skippedFiles <- skippedFiles + 1
                    | Failed fail ->
                        failedFiles <- failedFiles + 1
                        printfn "\n💥 Exception in %s: %A" file fail

        printfn "\n--- Scan complete! ---"
        printfn "Files scanned: %d" totalFiles
        printfn "Skipped: %d" skippedFiles
        printfn "Failed: %d" failedFiles
        printfn "Total Violations: %d" totalViolations

        match results.TryGetResult(Out_Json) with
        | Some outPath ->
            Output.writeCanonicalJson (List.ofSeq allResults) outPath
            printfn "Wrote JSON output to %s" outPath
        | None -> ()

        match results.TryGetResult(Out_Sarif) with
        | Some outPath ->
            Output.writeSarif (List.ofSeq allResults) outPath
            printfn "Wrote SARIF output to %s" outPath
        | None -> ()

        match results.TryGetResult(Out_Toolchain) with
        | Some outPath ->
            Output.writeToolchainRecord outPath
            printfn "Wrote toolchain record to %s" outPath
        | None -> ()

        if failedFiles > 0 then ExitCodes.ToolFailure
        elif totalViolations > 0 then ExitCodes.BlockingFinding
        else ExitCodes.Success
