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
                | "FSA1004" when line.Contains("type ") && line.Contains(" = string") ->
                    let nameIdx = line.IndexOf("type ") + 5
                    let eqIdx = line.IndexOf(" = string")
                    if eqIdx > nameIdx then
                        let name = line.Substring(nameIdx, eqIdx - nameIdx).Trim()
                        line.Replace(" = string", sprintf " = %s of string" name), true
                    else line, false
                | "FSA1004" when line.Contains("type ") && line.Contains(" = int") ->
                    let nameIdx = line.IndexOf("type ") + 5
                    let eqIdx = line.IndexOf(" = int")
                    if eqIdx > nameIdx then
                        let name = line.Substring(nameIdx, eqIdx - nameIdx).Trim()
                        line.Replace(" = int", sprintf " = %s of int" name), true
                    else line, false
                | "FSA1009" when line.Contains("ResizeArray<") -> line.Replace("ResizeArray<", "List<"), true
                | "FSA1009" when line.Contains("System.Collections.Generic.List") -> line.Replace("System.Collections.Generic.List", "List"), true
                | "FSA2012" when line.Contains("HashSet<") -> line.Replace("HashSet<", "Set<"), true
                | "FSA2030" when line.Contains(".Dispose()") ->
                    if line.TrimStart().StartsWith("let ") then
                        line.Replace("let ", "use ").Replace(".Dispose()", ""), true
                    else
                        let varName = line.Replace(".Dispose()", "").Trim()
                        sprintf "// Auto-fixed: use binding preferred for %s" varName, true
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

