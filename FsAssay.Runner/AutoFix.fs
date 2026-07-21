namespace FsAssay.Runner

open System.IO
open FSharp.Analyzers.SDK

module AutoFix =
    let applyAutoFixes (file: string) (violations: Message list) =
        if not (File.Exists(file)) || List.isEmpty violations then 0
        else
            let lines = File.ReadAllLines(file)
            let mutable fixedCount = 0

            for v in violations do
                let lineIdx = max 0 (v.Range.StartLine - 1)
                if lineIdx < lines.Length then
                    let line = lines.[lineIdx]
                    match v.Code with
                    | "FSA1001" when line.Contains("let mutable ") ->
                        lines.[lineIdx] <- line.Replace("let mutable ", "let ")
                        fixedCount <- fixedCount + 1
                    | "FSA1003" when line.Contains(" = null") ->
                        lines.[lineIdx] <- line.Replace(" = null", " = None")
                        fixedCount <- fixedCount + 1
                    | "FSA1003" when line.Contains("null") ->
                        lines.[lineIdx] <- line.Replace("null", "None")
                        fixedCount <- fixedCount + 1
                    | _ -> ()

            if fixedCount > 0 then
                File.WriteAllLines(file, lines)
            fixedCount
