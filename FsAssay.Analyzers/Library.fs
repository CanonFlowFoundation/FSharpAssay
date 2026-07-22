namespace FsAssay.Analyzers

open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open System

module Rules =

    type Rule = 
        | FSA1001 // Mutation Overuse
        | FSA1002 // Partial Access
        | FSA1003 // Null Reference
        | FSA1101 // Blocking Call (.Result / .Wait)
        | FSA1401 // Async Start unwrapped
        with
            member this.Code = 
                match this with
                | FSA1001 -> "FSA1001"
                | FSA1002 -> "FSA1002"
                | FSA1003 -> "FSA1003"
                | FSA1101 -> "FSA1101"
                | FSA1401 -> "FSA1401"
                
            member this.Message =
                match this with
                | FSA1001 -> "Mutation Overuse: Avoid 'mutable'. Use record copies with 'with' instead."
                | FSA1002 -> "Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching."
                | FSA1003 -> "Null Reference: Avoid 'null'. Use 'Option' types to represent missing values."
                | FSA1101 -> "Task Blocking Call: Avoid Task.Result or Task.Wait() in asynchronous code."
                | FSA1401 -> "Synchronous Async Run: Avoid Async.RunSynchronously in library code."

    [<CustomEquality; CustomComparison>]
    type Located<'F when 'F : comparison> = 
        { Finding: 'F; Range: range }
        override x.Equals(yobj) =
            match yobj with
            | :? Located<'F> as y -> x.Finding = y.Finding && x.Range = y.Range
            | _ -> false
        override x.GetHashCode() = hash (x.Finding, x.Range)
        interface System.IComparable with
            member x.CompareTo yobj =
                match yobj with
                | :? Located<'F> as y ->
                    let c1 = compare x.Finding y.Finding
                    if c1 <> 0 then c1
                    else
                        let c2 = compare x.Range.StartLine y.Range.StartLine
                        if c2 <> 0 then c2
                        else compare x.Range.StartColumn y.Range.StartColumn
                | _ -> invalidArg "yobj" "cannot compare values of different types"

    let mkLocated finding (r: range) =
        if r.StartLine = 0 then None
        else Some { Finding = finding; Range = r }

    let extractSuppressions (attrs: seq<FSharpAttribute>) =
        attrs
        |> Seq.choose (fun a ->
            let name = a.AttributeType.LogicalName
            if name = "SuppressMessageAttribute" || name = "SuppressMessage" then
                let args = a.ConstructorArguments
                if args.Count >= 2 then
                    let category = string (snd args.[0])
                    let checkId = string (snd args.[1])
                    if category = "FsAssay" then Some checkId else None
                else None
            elif name = "ProfileAttribute" || name = "Profile" then
                let args = a.ConstructorArguments
                if args.Count >= 1 then
                    let profile = string (snd args.[0])
                    Some ("PROFILE:" + profile)
                else None
            else None)
        |> Seq.toList

    let isSuppressed sups code =
        sups |> List.contains code ||
        (sups |> List.contains "PROFILE:interop" && (code = "FSA1001" || code = "FSA1003" || code = "FSA1008" || code = "FSA1009" || code = "FSA2016")) ||
        (sups |> List.contains "PROFILE:cli" && (code = "FSA1004" || code = "FSA2020")) ||
        (sups |> List.contains "PROFILE:etl" && code = "FSA1009") ||
        (sups |> List.contains "PROFILE:test" && (code = "FSA1003" || code = "FSA1006")) ||
        (sups |> List.contains "PROFILE:script" && (code = "FSA1001" || code = "FSA2022"))

    let toMessage (loc: Located<Rule>) : Message =
        {
            Type = loc.Finding.Code
            Message = loc.Finding.Message
            Code = loc.Finding.Code
            Severity = Severity.Error
            Range = loc.Range
            Fixes = []
        }

    let analyzeDecl (decl: FSharpImplementationFileDeclaration) (topSups: string list) (sourceText: ISourceText) : Set<Located<Rule>> =
        let rec visitExpr (expr: FSharpExpr) (sups: string list) : Located<Rule> list =
            let currentSups = sups
            match expr with
            | FSharpExprPatterns.Call(obj, func, _, _, args) ->
                let name = try func.FullName with _ -> ""
                let logicalName = try func.LogicalName with _ -> ""
                printfn "CALL: %s / %s" name logicalName
                let findings = 
                    if name.Contains("get_Value") || name.Contains("get_Head") || logicalName = "GetValue" || logicalName = "Value" || logicalName = "Head" || logicalName = "get_Value" || logicalName = "get_Head" then
                        if not (isSuppressed currentSups "FSA1002") then
                            mkLocated FSA1002 expr.Range |> Option.toList
                        else []
                    elif name.Contains(".Result") || name.Contains(".Wait") || logicalName = "Wait" || logicalName = "Result" || logicalName = "get_Result" then
                        if not (isSuppressed currentSups "FSA1101") then
                            mkLocated FSA1101 expr.Range |> Option.toList
                        else []
                    elif name.Contains("RunSynchronously") || logicalName = "RunSynchronously" then
                        if not (isSuppressed currentSups "FSA1401") then
                            mkLocated FSA1401 expr.Range |> Option.toList
                        else []
                    else []
                
                let objFindings = match obj with | Some o -> visitExpr o currentSups | None -> []
                let argsFindings = args |> List.collect (fun a -> visitExpr a currentSups)
                findings @ objFindings @ argsFindings

            | FSharpExprPatterns.Let((binding, valExpr, _), body) ->
                let localSups = extractSuppressions binding.Attributes @ currentSups
                let mutFindings = 
                    if binding.IsMutable && not binding.IsCompilerGenerated then
                        if not (isSuppressed localSups "FSA1001") then
                            mkLocated FSA1001 binding.DeclarationLocation |> Option.toList
                        else []
                    else []
                mutFindings @ visitExpr valExpr localSups @ visitExpr body localSups

            | FSharpExprPatterns.DefaultValue(ty) ->
                if not (isSuppressed currentSups "FSA1003") then
                    let textRange = expr.Range
                    if not (ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit") then
                        let text = try sourceText.GetSubTextFromRange(textRange).ToString() with _ -> ""
                        if text.Contains("null") || text.Contains("defaultof") then
                            mkLocated FSA1003 textRange |> Option.toList
                        else []
                    else []
                else []

            | FSharpExprPatterns.Const(obj, ty) ->
                if isNull obj && not (ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit") then
                    if not (isSuppressed currentSups "FSA1003") then
                        let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                        if text.Contains("null") then
                            mkLocated FSA1003 expr.Range |> Option.toList
                        else []
                    else []
                else []

            | FSharpExprPatterns.ValueSet(v, valExpr) ->
                let findings =
                    if not v.IsCompilerGenerated then
                        if not (isSuppressed currentSups "FSA1001") then
                            mkLocated FSA1001 expr.Range |> Option.toList
                        else []
                    else []
                findings @ visitExpr valExpr currentSups

            | FSharpExprPatterns.Application(func, _, args) ->
                visitExpr func currentSups @ List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.IfThenElse(cond, ifTrue, ifFalse) ->
                visitExpr cond currentSups @ visitExpr ifTrue currentSups @ visitExpr ifFalse currentSups
            | FSharpExprPatterns.TupleGet(_, _, tupleExpr) ->
                visitExpr tupleExpr currentSups
            | FSharpExprPatterns.DecisionTree(cond, targets) ->
                visitExpr cond currentSups @ List.collect (fun (_, e) -> visitExpr e currentSups) targets
            | FSharpExprPatterns.DecisionTreeSuccess(_, args) ->
                List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.Sequential(e1, e2) ->
                visitExpr e1 currentSups @ visitExpr e2 currentSups
            | FSharpExprPatterns.Lambda(v, body) ->
                visitExpr body currentSups
            | FSharpExprPatterns.LetRec(bindings, body) ->
                let bindingsFindings = bindings |> List.collect (fun (b, e, _) -> visitExpr e currentSups)
                bindingsFindings @ visitExpr body currentSups
            | FSharpExprPatterns.NewObject(_, _, args) ->
                List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.NewRecord(_, args) ->
                List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.NewTuple(_, args) ->
                List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.NewUnionCase(_, _, args) ->
                List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.ObjectExpr(_, baseCall, overrides, interfaceImpls) ->
                visitExpr baseCall currentSups @ 
                List.collect (fun (m: FSharpObjectExprOverride) -> visitExpr m.Body currentSups) overrides @
                List.collect (fun (_, impls) -> List.collect (fun (m: FSharpObjectExprOverride) -> visitExpr m.Body currentSups) impls) interfaceImpls
            | FSharpExprPatterns.Quote(e) -> visitExpr e currentSups
            | FSharpExprPatterns.TryFinally(e1, e2, _, _) -> visitExpr e1 currentSups @ visitExpr e2 currentSups
            | FSharpExprPatterns.TryWith(e1, _, e2, _, e3, _, _) -> visitExpr e1 currentSups @ visitExpr e2 currentSups @ visitExpr e3 currentSups
            | FSharpExprPatterns.UnionCaseTest(e, _, _) -> visitExpr e currentSups
            | FSharpExprPatterns.WhileLoop(cond, body, _) -> visitExpr cond currentSups @ visitExpr body currentSups
            | FSharpExprPatterns.Coerce(_, e) -> visitExpr e currentSups
            | FSharpExprPatterns.AddressOf(e) -> visitExpr e currentSups
            | FSharpExprPatterns.AddressSet(e1, e2) -> visitExpr e1 currentSups @ visitExpr e2 currentSups
            | FSharpExprPatterns.TypeTest(_, e) -> visitExpr e currentSups
            | FSharpExprPatterns.UnionCaseGet(e, _, _, _) -> visitExpr e currentSups
            | FSharpExprPatterns.UnionCaseSet(e, _, _, _, value) -> visitExpr e currentSups @ visitExpr value currentSups
            | FSharpExprPatterns.UnionCaseTag(e, _) -> visitExpr e currentSups
            | FSharpExprPatterns.FSharpFieldGet(objOpt, _, _) -> match objOpt with Some e -> visitExpr e currentSups | None -> []
            | FSharpExprPatterns.FSharpFieldSet(objOpt, _, _, arg) -> (match objOpt with Some e -> visitExpr e currentSups | None -> []) @ visitExpr arg currentSups
            | FSharpExprPatterns.ILFieldGet(objOpt, _, _) -> match objOpt with Some e -> visitExpr e currentSups | None -> []
            | FSharpExprPatterns.ILFieldSet(objOpt, _, _, arg) -> (match objOpt with Some e -> visitExpr e currentSups | None -> []) @ visitExpr arg currentSups
            | FSharpExprPatterns.ILAsm(_, _, args) -> List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.TraitCall(_, _, _, _, _, args) -> List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.FastIntegerForLoop(start, limit, body, _, _, _) -> visitExpr start currentSups @ visitExpr limit currentSups @ visitExpr body currentSups
            | _ -> []

        let rec visit (d: FSharpImplementationFileDeclaration) (sups: string list) =
            match d with
            | FSharpImplementationFileDeclaration.Entity(e, decls) ->
                let localSups = extractSuppressions e.Attributes @ sups
                decls |> List.collect (fun child -> visit child localSups)
            | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(v, args, body) ->
                let localSups = extractSuppressions v.Attributes @ sups
                let mutFindings = 
                    if v.IsMutable && not v.IsCompilerGenerated && not v.IsPropertyGetterMethod && not v.IsPropertySetterMethod && not v.IsProperty && not (v.LogicalName.StartsWith("New")) && not (v.LogicalName.StartsWith("get_")) && not (v.LogicalName.StartsWith("set_")) then
                        if not (isSuppressed localSups "FSA1001") then
                            mkLocated FSA1001 v.DeclarationLocation |> Option.toList
                        else []
                    else []
                if v.IsCompilerGenerated then mutFindings
                else mutFindings @ visitExpr body localSups
            | FSharpImplementationFileDeclaration.InitAction(expr) ->
                visitExpr expr sups
                
        visit decl topSups |> Set.ofList

    [<CliAnalyzer "FSA_All">]
    let antiPatternAnalyzer : Analyzer<CliContext> =
        fun ctx ->
            async {
                match ctx.TypedTree with
                | Some tree ->
                    let topLevelSups =
                        if ctx.FileName.Contains("?profile=") then
                            let p = ctx.FileName.Substring(ctx.FileName.IndexOf("?profile=") + 9)
                            [ "PROFILE:" + p ]
                        else []
                    
                    let findings =
                        tree.Declarations
                        |> List.map (fun d -> analyzeDecl d topLevelSups ctx.SourceText)
                        |> Set.unionMany
                        
                    return findings |> Set.toList |> List.map toMessage
                | None ->
                    // FsAssay Law 1/3: Honest uncertainty. If TAST is missing, do not silently fallback to regex.
                    return []
            }
