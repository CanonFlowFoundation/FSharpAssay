open System.IO
open FsAssay.Runner

[<EntryPoint>]
let main argv =
    let path = if argv.Length > 0 then argv.[0] else Directory.GetCurrentDirectory()
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

        for options in optionsList do
            for file in options.SourceFiles do
                if file.EndsWith(".fs") && not (file.Contains("AssemblyAttributes.fs")) && not (file.Contains("AssemblyInfo.fs")) then
                    totalFiles <- totalFiles + 1
                    let verdict = Orchestrator.evaluateFile options file |> Async.RunSynchronously
                    match verdict with
                    | Completed violations ->
                        if not (List.isEmpty violations) then
                            totalViolations <- totalViolations + violations.Length
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

        if failedFiles > 0 then ExitCodes.ToolFailure
        elif totalViolations > 0 then ExitCodes.BlockingFinding
        else ExitCodes.Success
