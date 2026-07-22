namespace FsAssay.Analyzers

open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open System.Text.RegularExpressions

module Rules =

    let createViolationWithFix code msg range fixOpt =
        {
            Type = code
            Message = msg
            Code = code
            Severity = Severity.Error
            Range = range
            Fixes = match fixOpt with Some f -> [f] | None -> []
        }

    let createViolation code msg range =
        createViolationWithFix code msg range None

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
                    let addViolationWithFix code msg range (sups: string list) fixOpt =
                        let isSuppressed = 
                            sups |> List.contains code ||
                            (code = "FSA1003" && sups |> List.contains "PROFILE:interop") ||
                            (code = "FSA1001" && sups |> List.contains "PROFILE:interop") ||
                            (code = "FSA1101" && sups |> List.contains "PROFILE:script") ||
                            (code = "FSA1301" && sups |> List.contains "PROFILE:shell")
                        if not isSuppressed then
                            violations <- createViolationWithFix code msg range fixOpt :: violations

                    let addViolation code msg range sups =
                        addViolationWithFix code msg range sups None

                    let rec visitExpr (expr: FSharpExpr) (sups: string list) =
                        match expr with
                        | FSharpExprPatterns.Call(obj, func, _, _, args) ->
                            let name = func.FullName
                            if name = "Microsoft.FSharp.Core.OptionModule.GetValue" ||
                               name.EndsWith("FSharpOption`1.get_Value") ||
                               name = "Microsoft.FSharp.Collections.ListModule.Head" ||
                               name = "Microsoft.FSharp.Collections.SeqModule.Head" ||
                               name.EndsWith("FSharpList`1.get_Head") then
                                let fix = { FromRange = expr.Range; FromText = "Option.get"; ToText = "match opt with Some v -> v | None -> failwith \"handle missing\"" }
                                addViolationWithFix "FSA1002" "Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching." expr.Range sups (Some fix)
                            
                            // FSA1101: Blocking on Async
                            if name = "Microsoft.FSharp.Control.AsyncModule.RunSynchronously" ||
                               name.EndsWith("Task`1.get_Result") ||
                               name.EndsWith("Task.Wait") then
                                addViolation "FSA1101" "Async Blocking: Avoid Async.RunSynchronously, .Result, or .Wait(). Use let! inside async or task block." expr.Range sups

                            // FSA1401: Missing Cancellation Token
                            if name = "Microsoft.FSharp.Control.AsyncModule.Start" then
                                addViolation "FSA1401" "Unbounded Async Start: Avoid Async.Start without explicit cancellation token. Use Async.StartImmediate or pass CancellationToken." expr.Range sups

                            args |> List.iter (fun a -> visitExpr a sups)
                            obj |> Option.iter (fun o -> visitExpr o sups)
                        | FSharpExprPatterns.Let((binding, valExpr, _), body) ->
                            let localSups = extractSuppressions binding.Attributes @ sups
                            if binding.IsMutable then
                                let fix = { FromRange = binding.DeclarationLocation; FromText = "mutable"; ToText = "let updatedRecord = { record with Field = newValue }" }
                                addViolationWithFix "FSA1001" "Mutation Overuse: Avoid 'mutable'. Use record copies with 'with' instead." binding.DeclarationLocation localSups (Some fix)
                            visitExpr valExpr localSups
                            visitExpr body localSups
                        | FSharpExprPatterns.DefaultValue(_) ->
                            if expr.Range.StartLine > 0 then
                                let fix = { FromRange = expr.Range; FromText = "null"; ToText = "None" }
                                addViolationWithFix "FSA1003" "Null Reference: Avoid 'null'. Use 'Option' types to represent missing values." expr.Range sups (Some fix)
                        | FSharpExprPatterns.Const(obj, ty) ->
                            if expr.Range.StartLine > 0 && isNull obj && not (ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit") then
                                let fix = { FromRange = expr.Range; FromText = "null"; ToText = "None" }
                                addViolationWithFix "FSA1003" "Null Reference: Avoid 'null'. Use 'Option' types to represent missing values." expr.Range sups (Some fix)
                        | FSharpExprPatterns.ValueSet(v, valExpr) ->
                            if expr.Range.StartLine > 0 then
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
                            if not v.IsCompilerGenerated then
                                let localSups = extractSuppressions v.Attributes @ sups
                                if v.IsMutable && v.DeclarationLocation.StartLine > 0 then
                                    addViolation "FSA1001" "Mutation Overuse: Avoid 'mutable'. Use record copies with 'with' instead." v.DeclarationLocation localSups
                                visitExpr body localSups
                        | FSharpImplementationFileDeclaration.InitAction(expr) ->
                            visitExpr expr sups

                    try
                        ctx.TypedTree.Value.Declarations |> List.iter (fun d -> visitDecl d [])
                    with _ -> ()
                
                // FSA1002: Partial Access Fallback
                if Regex.IsMatch(source, @"\.Value\b") || source.Contains("Option.get") || source.Contains("List.head") || source.Contains("Seq.head") then
                    if not (violations |> List.exists (fun v -> v.Code = "FSA1002")) then
                        let fix = { FromRange = Range.range0; FromText = "Option.get"; ToText = "match opt with Some v -> v | None -> failwith \"handle missing\"" }
                        violations <- createViolationWithFix "FSA1002" "Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching." Range.range0 (Some fix) :: violations

                // FSA1004: Primitive Obsession
                if Regex.IsMatch(source, @"(?m)^\s*type\s+[A-Za-z0-9_]+\s*=\s*(string|int|float|bool|decimal|DateTime)\s*(?://.*)?$") then
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

                // FSA1101: Async Blocking Fallback
                if source.Contains("Async.RunSynchronously") || source.Contains(".Result") || source.Contains(".Wait()") then
                    if not (violations |> List.exists (fun v -> v.Code = "FSA1101")) then
                        violations <- createViolation "FSA1101" "Async Blocking: Avoid Async.RunSynchronously, .Result, or .Wait(). Use let! inside async or task block." Range.range0 :: violations

                // FSA1201: Unbounded Buffer / Sequence Leaks
                if source.Contains("Seq.toList") && (source.Contains("Seq.initInfinite") || source.Contains("IEnumerable")) then
                    violations <- createViolation "FSA1201" "Unbounded Materialization: Avoid Seq.toList on unbounded sequences. Use Seq.truncate or bounded channels." Range.range0 :: violations

                // FSA1301: EF Core Leak in Domain
                if source.Contains("Microsoft.EntityFrameworkCore") || source.Contains("DbContext") then
                    violations <- createViolation "FSA1301" "EF Core Scope Leak: Avoid ORM/EFCore dependencies in core domain logic. Isolate persistence to shell." Range.range0 :: violations

                // FSA1401: Unbounded Async Start Fallback
                if source.Contains("Async.Start") && not (source.Contains("Async.StartImmediate")) then
                    if not (violations |> List.exists (fun v -> v.Code = "FSA1401")) then
                        violations <- createViolation "FSA1401" "Unbounded Async Start: Avoid Async.Start without explicit cancellation token. Use Async.StartImmediate or pass CancellationToken." Range.range0 :: violations

                return violations |> List.rev
            }

