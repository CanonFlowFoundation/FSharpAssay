namespace FsAssay.Runner

open System.IO
open FSharp.Analyzers.SDK

module AutoFix =
    let applyAutoFixes (file: string) (violations: Message list) =
        if not (File.Exists(file)) || List.isEmpty violations then 0
        else
            let lines = File.ReadAllLines(file)
            
            let (updatedLines, fixesApplied) =
                violations
                |> List.fold (fun (currLines: string array, count: int) v ->
                    let mutable newCount = count
                    let mutable modLines = Array.copy currLines
                    for fix in v.Fixes do
                        let lineIdx = max 0 (fix.FromRange.StartLine - 1)
                        if lineIdx < modLines.Length then
                            let line = modLines.[lineIdx]
                            if line.Contains(fix.FromText) then
                                modLines.[lineIdx] <- line.Replace(fix.FromText, fix.ToText)
                                newCount <- newCount + 1
                    (modLines, newCount)
                ) (lines, 0)

            if fixesApplied > 0 then
                File.WriteAllLines(file, updatedLines)
            fixesApplied

