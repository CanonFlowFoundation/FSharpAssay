namespace FsAssay.Analyzers

open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open System.Text.RegularExpressions

module Rules =

    let createViolation code msg range =
        {
            Type = code
            Message = msg
            Code = code
            Severity = Severity.Error
            Range = range
            Fixes = []
        }

    [<CliAnalyzer "FSA_All">]
    let antiPatternAnalyzer : Analyzer<CliContext> =
        fun ctx ->
            async {
                let source = ctx.SourceText.ToString()
                let mutable violations = []
                
                if ctx.TypedTree.IsSome then
                    let rec visitExpr (expr: FSharpExpr) =
                        match expr with
                        | FSharpExprPatterns.Call(obj, func, _, _, args) ->
                            let name = func.FullName
                            // Print the full name to the console so we can debug the tests
                            System.Console.WriteLine("CALL: " + name)
                            if name = "Microsoft.FSharp.Core.OptionModule.GetValue" ||
                               name.EndsWith("FSharpOption`1.get_Value") ||
                               name = "Microsoft.FSharp.Collections.ListModule.Head" ||
                               name = "Microsoft.FSharp.Collections.SeqModule.Head" ||
                               name.EndsWith("FSharpList`1.get_Head") then
                                violations <- createViolation "FSA1002" "Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching." expr.Range :: violations
                            
                            args |> List.iter visitExpr
                            obj |> Option.iter visitExpr
                        | _ ->
                            let prop = expr.GetType().GetProperty("ImmediateSubExpressions")
                            if not (isNull prop) then
                                let subExprs = prop.GetValue(expr) :?> seq<FSharpExpr>
                                for e in subExprs do
                                    visitExpr e

                    let rec visitDecl (decl: FSharpImplementationFileDeclaration) =
                        match decl with
                        | FSharpImplementationFileDeclaration.Entity(e, decls) ->
                            decls |> List.iter visitDecl
                        | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(v, args, body) ->
                            visitExpr body
                        | FSharpImplementationFileDeclaration.InitAction(expr) ->
                            visitExpr expr

                    ctx.TypedTree.Value.Declarations |> List.iter visitDecl
                
                // FSA1001: Mutation Overuse
                if source.Contains("mutable ") then
                    violations <- createViolation "FSA1001" "Mutation Overuse: Avoid 'mutable'. Use record copies with 'with' instead." Range.range0 :: violations
                
                // FSA1003: Null Reference
                if Regex.IsMatch(source, @"\bnull\b") || source.Contains("Unchecked.defaultof") then
                    violations <- createViolation "FSA1003" "Null Reference: Avoid 'null'. Use 'Option' types to represent missing values." Range.range0 :: violations
                    
                // FSA1004: Primitive Obsession
                if Regex.IsMatch(source, @"type\s+[A-Za-z0-9_]+\s*=\s*(string|int|float|bool|decimal|DateTime)\b") then
                    violations <- createViolation "FSA1004" "Primitive Obsession: Do not use type aliases for primitives. Use Single-Case Discriminated Unions to make illegal states unrepresentable." Range.range0 :: violations

                // FSA1005: Boolean Validation
                if Regex.IsMatch(source, @"let\s+is[A-Z][a-zA-Z0-9_]*\b") || source.Contains("isValid") then
                    violations <- createViolation "FSA1005" "Parse, Don't Validate: Functions should return Result<ParsedType, Error> rather than a boolean validity flag." Range.range0 :: violations

                // FSA1006: Generic Catch
                if source.Contains(":? Exception") || source.Contains("catch (Exception") || source.Contains(":? System.Exception") then
                    violations <- createViolation "FSA1006" "Generic Catch: Do not catch generic exceptions for flow control. Use Result types instead." Range.range0 :: violations

                // FSA1007: Imperative Loops
                if Regex.IsMatch(source, @"\bwhile\b") then
                    violations <- createViolation "FSA1007" "Imperative Loops: Avoid 'while' loops. Use Seq.fold or recursion." Range.range0 :: violations

                // FSA1008: OOP Inheritance
                if Regex.IsMatch(source, @"\binherit\b") || Regex.IsMatch(source, @"\babstract\s+member\b") || Regex.IsMatch(source, @"\binterface\b.*with") then
                    violations <- createViolation "FSA1008" "OOP Inheritance: Avoid OOP inheritance and interfaces. Use records of functions or Discriminated Unions." Range.range0 :: violations

                // FSA1009: Mutable Collections
                if Regex.IsMatch(source, @"\bResizeArray\b") || source.Contains("System.Collections.Generic.List") || source.Contains("System.Collections.Generic.Dictionary") then
                    violations <- createViolation "FSA1009" "Mutable Collections: Avoid C# mutable collections. Use F# immutable Map, Set, or list." Range.range0 :: violations

                return violations |> List.rev
            }
