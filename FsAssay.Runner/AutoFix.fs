namespace FsAssay.Runner

open System.IO
open FSharp.Analyzers.SDK

module AutoFix =
    let applyAutoFixes (file: string) (violations: Message list) =
        if not (File.Exists(file)) || List.isEmpty violations then 0
        else
            let lines = File.ReadAllLines(file)
            
            let applyFixOnLine (line: string) (code: string) =
                match code with
                | "FSA1001" when line.Contains("let mutable ") -> line.Replace("let mutable ", "let "), true
                | "FSA1003" when line.Contains(" = null") -> line.Replace(" = null", " = None"), true
                | "FSA1003" when line.Contains("null") -> line.Replace("null", "None"), true
                | _ -> line, false

            let (updatedLines, fixesApplied) =
                violations
                |> List.fold (fun (currLines: string array, count: int) v ->
                    let lineIdx = max 0 (v.Range.StartLine - 1)
                    if lineIdx < currLines.Length then
                        let (newLine, wasFixed) = applyFixOnLine currLines.[lineIdx] v.Code
                        if wasFixed then
                            currLines.[lineIdx] <- newLine
                            (currLines, count + 1)
                        else (currLines, count)
                    else (currLines, count)
                ) (lines, 0)

            if fixesApplied > 0 then
                File.WriteAllLines(file, updatedLines)
            fixesApplied
