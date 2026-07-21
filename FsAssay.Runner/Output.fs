namespace FsAssay.Runner

open System
open System.IO
open System.Text.Json
open FSharp.Analyzers.SDK

module Output =
    type JsonViolation = {
        code: string
        message: string
        startLine: int
        startColumn: int
        endLine: int
        endColumn: int
    }
    
    type JsonFileResult = {
        file: string
        violations: JsonViolation[]
    }
    
    let writeCanonicalJson (results: (string * Message list) list) (outPath: string) =
        let jsonResults =
            results
            |> List.map (fun (file, violations) ->
                {
                    file = file
                    violations = 
                        violations |> List.map (fun v ->
                            {
                                code = v.Code
                                message = v.Message
                                startLine = v.Range.StartLine
                                startColumn = v.Range.StartColumn
                                endLine = v.Range.EndLine
                                endColumn = v.Range.EndColumn
                            }
                        ) |> List.toArray
                }
            )
            |> List.toArray
        let options = JsonSerializerOptions(WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
        let jsonStr = JsonSerializer.Serialize(jsonResults, options)
        File.WriteAllText(outPath, jsonStr)

    // Minimal SARIF generation using anonymous records
    let writeSarif (results: (string * Message list) list) (outPath: string) =
        let sarifResults =
            results
            |> List.collect (fun (file, violations) ->
                violations |> List.map (fun v ->
                    {|
                        ruleId = v.Code
                        message = {| text = v.Message |}
                        locations = [|
                            {|
                                physicalLocation = {|
                                    artifactLocation = {| uri = "file://" + file.Replace("\\", "/") |}
                                    region = {|
                                        startLine = max 1 v.Range.StartLine
                                        startColumn = max 1 (v.Range.StartColumn + 1)
                                        endLine = max 1 v.Range.EndLine
                                        endColumn = max 1 (v.Range.EndColumn + 1)
                                    |}
                                |}
                            |}
                        |]
                    |}
                )
            )
            |> List.toArray

        let sarifObj = {|
            version = "2.1.0"
            ``$schema`` = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json"
            runs = [|
                {|
                    tool = {|
                        driver = {|
                            name = "FsAssay"
                            informationUri = "https://github.com/CanonFlowFoundation/FSharpAssay"
                            version = "1.0.0"
                        |}
                    |}
                    results = sarifResults
                |}
            |]
        |}

        let options = JsonSerializerOptions(WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
        let jsonStr = JsonSerializer.Serialize(sarifObj, options)
        File.WriteAllText(outPath, jsonStr)

    let writeToolchainRecord (outPath: string) =
        let record = {|
            os = Environment.OSVersion.ToString()
            dotnet = Environment.Version.ToString()
            fsc = typeof<FSharp.Compiler.CodeAnalysis.FSharpChecker>.Assembly.GetName().Version.ToString()
        |}
        let options = JsonSerializerOptions(WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
        File.WriteAllText(outPath, JsonSerializer.Serialize(record, options))
