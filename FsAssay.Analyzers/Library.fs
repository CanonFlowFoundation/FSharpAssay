namespace FsAssay.Analyzers

open FSharp.Analyzers.SDK
open FSharp.Compiler.Symbols

module Rules =

    [<CliAnalyzer "FSA1002">]
    let partialAccessAnalyzer : Analyzer<CliContext> =
        fun ctx ->
            async {
                let source = ctx.SourceText.ToString()
                if source.Contains("x.Value") || source.Contains("Option.get") then
                    return [
                        {
                            Type = "FSA1002"
                            Message = "Partial Access: Do not use Option.get or .Value. Use pattern matching."
                            Code = "FSA1002"
                            Severity = Severity.Error
                            Range = FSharp.Compiler.Text.Range.Zero
                            Fixes = []
                        }
                    ]
                else
                    return []
            }
