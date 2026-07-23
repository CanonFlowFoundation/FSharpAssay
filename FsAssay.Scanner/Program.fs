open System
open System.Diagnostics
open System.IO
open Argu

type Arguments =
    | [<MainCommand; ExactlyOnce>] TargetUrl of string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | TargetUrl _ -> "GitHub URL of the repository to scan."

[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C10")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S05")>]
let runProc (cmd: string) (args: string) (cwd: string) =
    let startInfo = ProcessStartInfo(cmd, args)
    startInfo.WorkingDirectory <- cwd
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.UseShellExecute <- false
    use p = Process.Start(startInfo)
    let _ = p.WaitForExit()
    let out = p.StandardOutput.ReadToEnd()
    let err = p.StandardError.ReadToEnd()
    (p.ExitCode, out, err)

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "fsassay-scanner")
    try
        let results = parser.ParseCommandLine argv
        let url = results.GetResult(TargetUrl)
        let _ = printfn "🦈 FsAssay Ecosystem Scanner (FSA-ECO) - Hunting %s..." url

        let tempDir = Path.Combine(Path.GetTempPath(), "FsAssayScans", Guid.NewGuid().ToString("N"))
        let _ = Directory.CreateDirectory(tempDir)

        let _ = printfn "Cloning repository..."
        let (cloneCode, cloneOut, cloneErr) = runProc "git" (sprintf "clone --depth 1 %s ." url) tempDir
        
        if cloneCode <> 0 then
            let _ = printfn "❌ Failed to clone repository: %s" cloneErr
            1
        else
            let _ = printfn "Building repository to generate .dll and .fsi artifacts..."
            let (buildCode, buildOut, buildErr) = runProc "dotnet" "build -c Release" tempDir
            
            let _ = 
                if buildCode <> 0 then
                    printfn "⚠️ Build failed. FsAssay will attempt a source-only scan."
                else
                    printfn "✅ Build succeeded."

            let _ = printfn "Running FsAssay Engine on Target..."
            let runnerDir = Directory.GetCurrentDirectory()
            let runnerProj = Path.Combine(runnerDir, "FsAssay.Runner", "FsAssay.Runner.fsproj")
            let (scanCode, scanOut, scanErr) = runProc "dotnet" (sprintf "run --project %s %s" runnerProj tempDir) runnerDir

            let _ = printfn "\n--- SCAN RESULTS ---"
            let _ = printfn "%s" scanOut

            if scanOut.Contains("FSA-E0") || scanOut.Contains("FSA-C") || scanOut.Contains("FSA-F") then
                let _ = printfn "🚨 ECOSYSTEM VERDICT: Shark (Failed Purity Tests)"
                1
            else
                let _ = printfn "🐬 ECOSYSTEM VERDICT: Dolphin (Passed Elite F# Checks)"
                0
    with e ->
        let _ = printfn "%s" e.Message
        1
