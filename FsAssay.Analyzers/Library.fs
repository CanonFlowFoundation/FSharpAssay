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

    let extractSuppressions (attrs: seq<FSharpAttribute>) =
        attrs
        |> Seq.choose (fun a ->
            let name = a.AttributeType.LogicalName
            if name = "SuppressMessageAttribute" || name = "SuppressMessage" then
                let args = a.ConstructorArguments
                if args.Count >= 2 then
                    let category = snd args.[0] :?> string
                    let checkId = snd args.[1] :?> string
                    if category = "FsAssay" then Some checkId else None
                else None
            elif name = "ProfileAttribute" || name = "Profile" then
                let args = a.ConstructorArguments
                if args.Count >= 1 then
                    let profile = snd args.[0] :?> string
                    Some ("PROFILE:" + profile)
                else None
            else None)
        |> Seq.toList

    [<CliAnalyzer "FSA_All">]
    let antiPatternAnalyzer : Analyzer<CliContext> =
        fun ctx ->
            async {
                let source = ctx.SourceText.ToString()
                let mutable violations = []
                
                if ctx.TypedTree.IsSome then
                    let addViolation code msg range (sups: string list) =
                        let isSuppressed = 
                            sups |> List.contains code ||
                            (code = "FSA1003" && sups |> List.contains "PROFILE:interop") ||
                            (code = "FSA1001" && sups |> List.contains "PROFILE:interop")
                        if not isSuppressed then
                            violations <- createViolation code msg range :: violations

                    let rec visitExpr (expr: FSharpExpr) (sups: string list) =
                        match expr with
                        | FSharpExprPatterns.Call(obj, func, _, _, args) ->
                            let name = func.FullName
                            if name = "Microsoft.FSharp.Core.OptionModule.GetValue" ||
                               name.EndsWith("FSharpOption`1.get_Value") ||
                               name = "Microsoft.FSharp.Collections.ListModule.Head" ||
                               name = "Microsoft.FSharp.Collections.SeqModule.Head" ||
                               name.EndsWith("FSharpList`1.get_Head") then
                                addViolation "FSA1002" "Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching." expr.Range sups
                            
                            args |> List.iter (fun a -> visitExpr a sups)
                            obj |> Option.iter (fun o -> visitExpr o sups)
                        | FSharpExprPatterns.Let((binding, valExpr, _), body) ->
                            let localSups = extractSuppressions binding.Attributes @ sups
                            if binding.IsMutable then
                                addViolation "FSA1001" "Mutation Overuse: Avoid 'mutable'. Use record copies with 'with' instead." binding.DeclarationLocation localSups
                            visitExpr valExpr localSups
                            visitExpr body localSups
                        | FSharpExprPatterns.DefaultValue(_) ->
                            addViolation "FSA1003" "Null Reference: Avoid 'null'. Use 'Option' types to represent missing values." expr.Range sups
                        | FSharpExprPatterns.Const(obj, _) ->
                            if isNull obj then
                                addViolation "FSA1003" "Null Reference: Avoid 'null'. Use 'Option' types to represent missing values." expr.Range sups
                        | FSharpExprPatterns.ValueSet(v, valExpr) ->
                            addViolation "FSA1001" "Mutation Overuse: Avoid mutation. Use record copies with 'with' instead." expr.Range sups
                            visitExpr valExpr sups
                        | _ ->
                            let prop = expr.GetType().GetProperty("ImmediateSubExpressions")
                            if not (isNull prop) then
                                let subExprs = prop.GetValue(expr) :?> seq<FSharpExpr>
                                for e in subExprs do
                                    visitExpr e sups

                    let rec visitDecl (decl: FSharpImplementationFileDeclaration) (sups: string list) =
                        match decl with
                        | FSharpImplementationFileDeclaration.Entity(e, decls) ->
                            let localSups = extractSuppressions e.Attributes @ sups
                            decls |> List.iter (fun d -> visitDecl d localSups)
                        | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(v, args, body) ->
                            let localSups = extractSuppressions v.Attributes @ sups
                            if v.IsMutable then
                                addViolation "FSA1001" "Mutation Overuse: Avoid 'mutable'. Use record copies with 'with' instead." v.DeclarationLocation localSups
                            visitExpr body localSups
                        | FSharpImplementationFileDeclaration.InitAction(expr) ->
                            visitExpr expr sups

                    ctx.TypedTree.Value.Declarations |> List.iter (fun d -> visitDecl d [])
                
                // Keep regex for the rest
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
