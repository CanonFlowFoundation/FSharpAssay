open System
open System.IO
open System.Text.RegularExpressions
open FsAssay.Runner
open FSharp.Analyzers.SDK
open Argu

type Arguments =
    | [<MainCommand; Last>] Target of path:string
    | [<AltCommandLine("-j")>] Out_Json of path:string
    | [<AltCommandLine("-s")>] Out_Sarif of path:string
    | [<AltCommandLine("-t")>] Out_Toolchain of path:string
    | [<AltCommandLine("-a")>] Adjudicate
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Target _ -> "Target directory or file to scan."
                | Out_Json _ -> "Output file path for canonical JSON."
                | Out_Sarif _ -> "Output file path for SARIF."
                | Out_Toolchain _ -> "Output file path for toolchain record."
                | Adjudicate -> "Run in adjudication mode (evaluate Precision/Recall against // EXPECT comments)."

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
                            if not (results.Contains(Adjudicate)) then
                                printfn "\n❌ %s" file
                                for v in violations do
                                    printfn "   └── [%s] %s (Line: %d)" v.Code v.Message v.Range.StartLine
                    | Skipped reason ->
                        skippedFiles <- skippedFiles + 1
                    | Failed fail ->
                        failedFiles <- failedFiles + 1
                        printfn "\n💥 Exception in %s: %A" file fail

        if results.Contains(Adjudicate) then
            printfn "\n--- Adjudication Mode ---"
            let mutable totalExpected = 0
            let mutable truePositives = 0
            let mutable falsePositives = 0
            let mutable falseNegatives = 0

            let expectedMap = System.Collections.Generic.Dictionary<string, string list>()
            let actualMap = System.Collections.Generic.Dictionary<string, string list>()

            for options in optionsList do
                for file in options.SourceFiles do
                    if file.EndsWith(".fs") then
                        let lines = File.ReadAllLines(file)
                        for i = 0 to lines.Length - 1 do
                            let line = lines.[i]
                            let m = Regex.Match(line, @"//\s*EXPECT:\s*(FSA\d{4})")
                            if m.Success then
                                let code = m.Groups.[1].Value
                                let key = sprintf "%s:%d:%s" file (i+1) code
                                expectedMap.[key] <- []
                                totalExpected <- totalExpected + 1

            for (file, violations) in allResults do
                for v in violations do
                    let key = sprintf "%s:%d:%s" file v.Range.StartLine v.Code
                    actualMap.[key] <- []

            for key in expectedMap.Keys do
                if actualMap.ContainsKey(key) then
                    truePositives <- truePositives + 1
                else
                    falseNegatives <- falseNegatives + 1
                    printfn "FN: Expected %s but was missed." key

            for key in actualMap.Keys do
                if not (expectedMap.ContainsKey(key)) then
                    falsePositives <- falsePositives + 1
                    printfn "FP: Unexpected %s." key

            let precision = if truePositives + falsePositives = 0 then 1.0 else float truePositives / float (truePositives + falsePositives)
            let recall = if truePositives + falseNegatives = 0 then 1.0 else float truePositives / float (truePositives + falseNegatives)

            printfn "Precision: %.2f%%" (precision * 100.0)
            printfn "Recall:    %.2f%%" (recall * 100.0)
            printfn "TP: %d | FP: %d | FN: %d" truePositives falsePositives falseNegatives
        else
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
        elif results.Contains(Adjudicate) then ExitCodes.Success
        elif totalViolations > 0 then ExitCodes.BlockingFinding
        else ExitCodes.Success


