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
                        | FSharpExprPatterns.Const(obj, ty) ->
                            if isNull obj && not (ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit") then
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

                // Helper for generating precise line-range violations for remaining valid regex checks
                let checkRegex code msg pattern (opts: RegexOptions) =
                    for m in Regex.Matches(source, pattern, opts) do
                        let line = source.Substring(0, m.Index).Split('\n').Length
                        let r = Range.mkRange ctx.FileName (Position.mkPos line 0) (Position.mkPos line 0)
                        violations <- createViolation code msg r :: violations

                // Lexical rules with precise line ranges & fixed regexes
                checkRegex "FSA1004" "Primitive Obsession: Do not use type aliases for primitives. Use Single-Case Discriminated Unions to make illegal states unrepresentable." @"type\s+[A-Za-z0-9_]+\s*=\s*(string|int|float|bool|decimal|DateTime)\b" RegexOptions.None
                checkRegex "FSA1005" "Parse, Don't Validate: Functions should return Result<ParsedType, Error> rather than a boolean validity flag." @"let\s+is[A-Z][a-zA-Z0-9_]*\b|isValid" RegexOptions.None
                checkRegex "FSA1006" "Generic Catch: Do not catch generic exceptions for flow control. Use Result types instead." @"\:\? Exception|catch \(Exception|\:\? System\.Exception" RegexOptions.None
                checkRegex "FSA1007" "Imperative Loops: Avoid 'while' loops. Use Seq.fold or recursion." @"\bwhile\b" RegexOptions.None
                checkRegex "FSA1008" "OOP Inheritance: Avoid OOP inheritance and interfaces. Use records of functions or Discriminated Unions." @"\binherit\b|\babstract\s+member\b|\binterface\b.*with" RegexOptions.None
                checkRegex "FSA1009" "Mutable Collections: Avoid C# mutable collections. Use F# immutable Map, Set, or list." @"\bResizeArray\b|System\.Collections\.Generic\.List|System\.Collections\.Generic\.Dictionary" RegexOptions.None
                checkRegex "FSA2008" "Enum Instead of DU: C#-style enum detected. Replace with a Discriminated Union." @"type\s+[A-Za-z0-9_]+\s*=\s*\|\s*[A-Za-z0-9_]+\s*=\s*\d+" RegexOptions.None
                checkRegex "FSA2012" "Mutable Collection Intrusion: BCL mutable collection detected in domain logic." @"\bHashSet\b|System\.Collections\.Generic\.HashSet" RegexOptions.None
                checkRegex "FSA2014" "Imperative Accumulation: Mutable accumulator with loop detected." @"let\s+mutable.*\n*.*(while|for)" RegexOptions.None
                
                // Fixed FSA2016 (escaped :\?> instead of unescaped :?>)
                checkRegex "FSA2016" "Unsafe Cast: Runtime cast detected. Model alternatives as a DU." @"(:\?>|\bbox\b|\bunbox\b)" RegexOptions.None
                
                checkRegex "FSA2017" "Reflection-Based Dispatch: Runtime type inspection used for dispatch." @"(typeof<|\.GetType\(\)|Activator\.CreateInstance|System\.Reflection)" RegexOptions.None
                checkRegex "FSA2018" "Inheritance Depth: override or virtual detected. Use composition." @"(\boverride\b|\bvirtual\b)" RegexOptions.None
                checkRegex "FSA2019" "Missing Computation Expression: Nested Result/Option matching detected. Use a Computation Expression." @"match.*with\s*\|\s*(Some|Ok).*->\s*match" RegexOptions.Singleline
                checkRegex "FSA2020" "Signature Blindness: Consecutive primitive arguments of the same type detected." @"\([A-Za-z0-9_]+\s*:\s*(int|string|float|decimal|bool)\)\s*\([A-Za-z0-9_]+\s*:\s*\1\)" RegexOptions.None
                checkRegex "FSA2021" "Flag-Based State Machine: Multiple boolean flags detected in a type. Use a DU." @"(Is[A-Z][a-zA-Z0-9_]*\s*:\s*bool).*?(Is[A-Z][a-zA-Z0-9_]*\s*:\s*bool)" RegexOptions.Singleline
                checkRegex "FSA2022" "Impure Core: I/O side effects detected without explicit functional boundaries." @"\bSystem\.IO\.File\b|\bHttpClient\b|\bConsole\.Write" RegexOptions.None
                checkRegex "FSA2023" "Nested Function Application: Deep nesting detected. Consider using the |> operator." @"\b[A-Za-z0-9_]+\s*\(\s*[A-Za-z0-9_]+\s*\(" RegexOptions.None
                checkRegex "FSA2024" "Missed Active Pattern: Complex if/elif chains detected. Consider an Active Pattern." @"\bif\b.*\belif\b.*\belif\b" RegexOptions.Singleline
                checkRegex "FSA2025" "Boolean Parameter Blindness: Consecutive boolean parameters/types detected." @"let\s+[A-Za-z0-9_]+\s+(true|false)\s+(true|false)|\bbool\b\s*->\s*\bbool\b\s*->" RegexOptions.None
                checkRegex "FSA2026" "Option Constellation: Multiple optional fields detected. Represent states with a DU." @"[A-Za-z0-9_]+\s*option.*?[A-Za-z0-9_]+\s*option.*?[A-Za-z0-9_]+\s*option" RegexOptions.Singleline
                checkRegex "FSA2027" "Stringly Error Channel: Result returning a primitive string error. Use a domain error DU." @"Result<[^,]+,\s*string>" RegexOptions.None
                checkRegex "FSA2028" "Static Class as Module: C#-style static class detected. Use an F# module." @"\[<AbstractClass[^>]*Sealed[^>]*>\]\s*type" RegexOptions.None
                checkRegex "FSA2030" "Manual Dispose: Explicit .Dispose() call detected. Use the 'use' keyword instead." @"\b[A-Za-z0-9_]+\.Dispose\(\)" RegexOptions.None

                return violations |> List.rev
            }
