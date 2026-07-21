namespace FsAssay.Analyzers

open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open System.Text.RegularExpressions

module Rules =

    let createViolation code msg =
        {
            Type = code
            Message = msg
            Code = code
            Severity = Severity.Error
            Range = Range.Zero
            Fixes = []
        }

    [<CliAnalyzer "FSA_All">]
    let antiPatternAnalyzer : Analyzer<CliContext> =
        fun ctx ->
            async {
                let source = ctx.SourceText.ToString()
                let mutable violations = []
                
                // FSA1001: Mutation Overuse
                if source.Contains("mutable ") then
                    violations <- createViolation "FSA1001" "Mutation Overuse: Avoid 'mutable'. Use record copies with 'with' instead." :: violations
                
                // FSA1002: Partial Access
                if Regex.IsMatch(source, @"(\.Value\b|Option\.get\b|\.Head\b|List\.head\b)") then
                    violations <- createViolation "FSA1002" "Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching." :: violations
                    
                // FSA1003: Null Reference
                if Regex.IsMatch(source, @"\bnull\b") || source.Contains("Unchecked.defaultof") then
                    violations <- createViolation "FSA1003" "Null Reference: Avoid 'null'. Use 'Option' types to represent missing values." :: violations
                    
                // FSA1004: Primitive Obsession
                if Regex.IsMatch(source, @"type\s+[A-Za-z0-9_]+\s*=\s*(string|int|float|bool|decimal|DateTime)\b") then
                    violations <- createViolation "FSA1004" "Primitive Obsession: Do not use type aliases for primitives. Use Single-Case Discriminated Unions to make illegal states unrepresentable." :: violations

                // FSA1005: Boolean Validation
                if Regex.IsMatch(source, @"let\s+is[A-Z][a-zA-Z0-9_]*\b") || source.Contains("isValid") then
                    violations <- createViolation "FSA1005" "Parse, Don't Validate: Functions should return Result<ParsedType, Error> rather than a boolean validity flag." :: violations

                // FSA1006: Generic Catch
                if source.Contains(":? Exception") || source.Contains("catch (Exception") || source.Contains(":? System.Exception") then
                    violations <- createViolation "FSA1006" "Generic Catch: Do not catch generic exceptions for flow control. Use Result types instead." :: violations

                // FSA1007: Imperative Loops
                if Regex.IsMatch(source, @"\bwhile\b") then
                    violations <- createViolation "FSA1007" "Imperative Loops: Avoid 'while' loops. Use Seq.fold or recursion." :: violations

                return violations |> List.rev
            }
