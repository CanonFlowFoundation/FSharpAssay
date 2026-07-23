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
    | [<AltCommandLine("-r")>] RateCard_Md of path:string
    | [<AltCommandLine("-m")>] Material_Html of path:string
    | [<AltCommandLine("-w")>] Watch
    | [<AltCommandLine("-d")>] Diff of gitRef:string
    | [<AltCommandLine("-p")>] Serve of port:int
    | [<AltCommandLine("-a")>] Adjudicate
    | [<AltCommandLine("-c")>] Files of paths:string
    | [<AltCommandLine("-P")>] Profile of profileName:string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Target _ -> "Target directory or file to scan."
                | Out_Json _ -> "Output file path for canonical JSON."
                | Out_Sarif _ -> "Output file path for SARIF."
                | Out_Toolchain _ -> "Output file path for toolchain record."
                | RateCard_Md _ -> "Output file path for Markdown Code Quality Rate Card."
                | Material_Html _ -> "Output file path for Material Design 5 HTML Dashboard."
                | Watch -> "Watch directory for file changes and re-run scans continuously."
                | Diff _ -> "Compare quality findings against a Git reference branch."
                | Serve _ -> "Start live Material Design 5 HTML dashboard web server on specified port."
                | Adjudicate -> "Run in adjudication mode (evaluate Precision/Recall against // EXPECT comments)."
                | Files _ -> "Comma-separated list of explicit files to scan (Incremental mode)."
                | Profile _ -> "Specify active domain profile (core, interop, cli, etl, test, script)."

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
    let rawConfig = Config.loadConfig path
    let activeProfile = results.GetResult(Profile, defaultValue = rawConfig.profile)
    let config = { rawConfig with profile = activeProfile }

    printfn "🧪 FsAssay Engine v0.1.0 — Scanning target: %s [Profile: %s]" path config.profile
    
    let executeScan () =
        let optionsList = ProjectSystem.getTargetProjects path
        let allDiscoveredFiles =
            if List.isEmpty optionsList then
                if File.Exists(path) && path.EndsWith(".fs") then [ (path, None) ]
                elif Directory.Exists(path) then
                    Directory.GetFiles(path, "*.fs", SearchOption.AllDirectories)
                    |> Array.filter (fun f -> not (f.Contains("obj") || f.Contains("bin")))
                    |> Array.map (fun f -> (f, None))
                    |> Array.toList
                else []
            else
                optionsList
                |> List.collect (fun opts -> opts.SourceFiles |> Array.map (fun f -> (f, Some opts)) |> Array.toList)

        let filesToScan =
            match results.TryGetResult(Files) with
            | Some explicitPathsStr ->
                let explicitPaths = explicitPathsStr.Split(',', StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun p -> p.Trim())
                allDiscoveredFiles
                |> List.filter (fun (filePath, _) -> explicitPaths |> Array.exists (fun ep -> filePath.EndsWith(ep) || filePath = ep))
            | None -> allDiscoveredFiles

        if List.isEmpty filesToScan then
            printfn "No files found to scan."
            (0, 0, 0, 0, [])
        else
            let mutable totalViolations = 0
            let mutable totalFiles = 0
            let mutable failedFiles = 0
            let mutable skippedFiles = 0
            let allResults = ResizeArray<string * Message list>()

            for (file, optsOpt) in filesToScan do
                let isExcluded = config.exclude |> Array.exists (fun pat -> file.Contains(pat.Replace("*", "")))
                if not isExcluded && file.EndsWith(".fs") && not (file.Contains("AssemblyAttributes.fs")) && not (file.Contains("AssemblyInfo.fs")) then
                    totalFiles <- totalFiles + 1
                    let verdict =
                        match optsOpt with
                        | Some opts -> Orchestrator.evaluateFileWithProfile opts file config.profile |> Async.RunSynchronously
                        | None -> Orchestrator.evaluateSingleFileWithProfile file config.profile |> Async.RunSynchronously

                    match verdict with
                    | Completed violations ->
                        if not (List.isEmpty violations) then
                            totalViolations <- totalViolations + violations.Length
                            allResults.Add(file, violations)
                            if not (results.Contains(Adjudicate)) then
                                printfn "\n❌ %s" file
                                for v in violations do
                                    printfn "   └── [%s] %s (Line: %d, Col: %d)" v.Code v.Message v.Range.StartLine v.Range.StartColumn
                    | Skipped reason ->
                        skippedFiles <- skippedFiles + 1
                    | Failed fail ->
                        failedFiles <- failedFiles + 1
                        printfn "\n💥 Exception in %s: %A" file fail

            (totalFiles, skippedFiles, failedFiles, totalViolations, List.ofSeq allResults)

    let (totalFiles, skippedFiles, failedFiles, totalViolations, allResults) = executeScan ()

    if not (results.Contains(Adjudicate)) then
        printfn "\n--- Scan complete! ---"
        printfn "Files scanned: %d" totalFiles
        printfn "Skipped: %d" skippedFiles
        printfn "Failed: %d" failedFiles
        printfn "Total Violations: %d" totalViolations

    match results.TryGetResult(Out_Json) with
    | Some outPath ->
        Output.writeCanonicalJson allResults outPath
        printfn "Wrote JSON output to %s" outPath
    | None -> ()

    match results.TryGetResult(Out_Sarif) with
    | Some outPath ->
        Output.writeSarif allResults outPath
        printfn "Wrote SARIF output to %s" outPath
    | None -> ()

    match results.TryGetResult(Out_Toolchain) with
    | Some outPath ->
        Output.writeToolchainRecord outPath
        printfn "Wrote toolchain record to %s" outPath
    | None -> ()

    match results.TryGetResult(RateCard_Md) with
    | Some outPath ->
        Output.writeRateCard allResults outPath
        printfn "Wrote Markdown Rate Card to %s" outPath
    | None -> ()

    match results.TryGetResult(Material_Html) with
    | Some outPath ->
        Output.writeMaterialDashboard allResults outPath
        printfn "Wrote Material Design 5 HTML Dashboard to %s" outPath
    | None -> ()

    match results.TryGetResult(Serve) with
    | Some port ->
        Server.startLiveServer allResults totalFiles port
    | None -> ()

    if results.Contains(Watch) then
        printfn "\n👀 Watch Mode active on %s. Monitoring file changes..." path
        use watcher = new FileSystemWatcher(path, "*.fs")
        watcher.IncludeSubdirectories <- true
        watcher.EnableRaisingEvents <- true
        watcher.Changed.Add(fun _ ->
            printfn "\n🔄 File change detected! Re-analyzing..."
            executeScan () |> ignore
        )
        System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite)

    if failedFiles > 0 then ExitCodes.ToolFailure
    elif skippedFiles > 0 then ExitCodes.RequiredEvidenceMissing
    elif results.Contains(Adjudicate) then ExitCodes.Success
    elif totalViolations > 0 then ExitCodes.BlockingFinding
    else ExitCodes.Success
