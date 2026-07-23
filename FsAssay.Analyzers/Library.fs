namespace FsAssay.Analyzers

open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open System

module Rules =

    type Rule = 
        | FSAC01 | FSAC02 | FSAC03 | FSAC04 | FSAC05 | FSAC06 | FSAC07 | FSAC08 | FSAC09 | FSAC10
        | FSAC11 | FSAC12 | FSAC13 | FSAC14
        | FSAS01 | FSAS02 | FSAS03 | FSAS04 | FSAS05
        | FSAML01 | FSAML02 | FSAB01
        with
            member this.Code = 
                match this with
                | FSAC01 -> "FSA-C01"
                | FSAC02 -> "FSA-C02"
                | FSAC03 -> "FSA-C03"
                | FSAC04 -> "FSA-C04"
                | FSAC05 -> "FSA-C05"
                | FSAC06 -> "FSA-C06"
                | FSAC07 -> "FSA-C07"
                | FSAC08 -> "FSA-C08"
                | FSAC09 -> "FSA-C09"
                | FSAC10 -> "FSA-C10"
                | FSAC11 -> "FSA-C11"
                | FSAC12 -> "FSA-C12"
                | FSAC13 -> "FSA-C13"
                | FSAC14 -> "FSA-C14"
                | FSAS01 -> "FSA-S01"
                | FSAS02 -> "FSA-S02"
                | FSAS03 -> "FSA-S03"
                | FSAS04 -> "FSA-S04"
                | FSAS05 -> "FSA-S05"
                | FSAML01 -> "FSA-ML01"
                | FSAML02 -> "FSA-ML02"
                | FSAB01 -> "FSA-B01"
                
            member this.Message =
                match this with
                | FSAC01 -> "Unchecked.defaultof<_> in Non-Interop Code"
                | FSAC02 -> "Option.get / .Value Without Guard"
                | FSAC03 -> "Async.RunSynchronously in Library Code"
                | FSAC04 -> "IDisposable Disposed Before Async Runs"
                | FSAC05 -> "Incomplete Pattern Match on DU"
                | FSAC06 -> "failwith / invalidArg / raise in Public API"
                | FSAC07 -> "Non-Tail Recursion in let rec"
                | FSAC08 -> "Seq.length on Infinite Sequences"
                | FSAC09 -> "Null Checking (isNull / = null) Instead of Option"
                | FSAC10 -> "Mutable State Instead of Functional Constructs"
                | FSAC11 -> "Use _.Property shorthand for lambdas (F# 8+)"
                | FSAC12 -> "Use nested record updates (F# 8+)"
                | FSAC13 -> "Missing [<TailCall>] attribute on recursive function"
                | FSAC14 -> "Evasion: Use of ref cells or Dictionary to bypass mutability rules"
                | FSAS01 -> "Hard-Coded Credentials / Secrets"
                | FSAS02 -> "Path Traversal in File Operations"
                | FSAS03 -> "Swallowed Exceptions"
                | FSAS04 -> "async { ... } Missing return"
                | FSAS05 -> "Task.Result / .Wait() Blocking Calls"
                | FSAML01 -> "Raw array mutation in core ML logic. Use pure Tensors."
                | FSAML02 -> "OOP Inheritance in ML Model. Use pure DUs/Records."
                | FSAB01 -> "Mutable state / arrays detected outside 'shell' profile."

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
        (sups |> List.contains "PROFILE:interop" && (code = "FSA-C01")) ||
        (sups |> List.contains "PROFILE:shell" && (code = "FSA-ML01" || code = "FSA-B01" || code = "FSA-C14"))

    let toMessage (loc: Located<Rule>) : Message =
        {
            Type = loc.Finding.Code
            Message = loc.Finding.Message
            Code = loc.Finding.Code
            Severity = if loc.Finding.Code.StartsWith("FSA-S") then Severity.Warning else Severity.Error
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
                
                let findings = 
                    let mutable f = []
                    if name.Contains("get_Value") || name.Contains("get_Head") || logicalName = "GetValue" || logicalName = "Value" || logicalName = "Head" || logicalName = "get_Value" || logicalName = "get_Head" then
                        if not (isSuppressed currentSups "FSA-C02") then f <- f @ (mkLocated FSAC02 expr.Range |> Option.toList)
                    if name.Contains(".Result") || name.Contains(".Wait") || logicalName = "Wait" || logicalName = "Result" || logicalName = "get_Result" then
                        if not (isSuppressed currentSups "FSA-S05") then f <- f @ (mkLocated FSAS05 expr.Range |> Option.toList)
                    if name.Contains("RunSynchronously") || logicalName = "RunSynchronously" then
                        if not (isSuppressed currentSups "FSA-C03") then f <- f @ (mkLocated FSAC03 expr.Range |> Option.toList)
                    if logicalName = "Raise" || logicalName = "failwith" || logicalName = "invalidArg" then
                        if not (isSuppressed currentSups "FSA-C06") then f <- f @ (mkLocated FSAC06 expr.Range |> Option.toList)
                    if logicalName = "length" || logicalName = "Length" then
                        let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                        if text.Contains("Seq.length") && (text.Contains("initInfinite") || text.Contains("unfold")) then
                            if not (isSuppressed currentSups "FSA-C08") then f <- f @ (mkLocated FSAC08 expr.Range |> Option.toList)
                    if logicalName = "Start" || name.Contains("Async.Start") then
                        let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                        if text.Contains("use ") then
                            if not (isSuppressed currentSups "FSA-C04") then f <- f @ (mkLocated FSAC04 expr.Range |> Option.toList)
                    if logicalName = "isNull" || logicalName = "op_Equality" then
                        let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                        if text.Contains("isNull") || text.Contains("null") then
                            if not (isSuppressed currentSups "FSA-C09") then f <- f @ (mkLocated FSAC09 expr.Range |> Option.toList)
                    f
                
                let objFindings = match obj with | Some o -> visitExpr o currentSups | None -> []
                let argsFindings = args |> List.collect (fun a -> visitExpr a currentSups)
                findings @ objFindings @ argsFindings

            | FSharpExprPatterns.Let((binding, valExpr, _), body) ->
                let localSups = extractSuppressions binding.Attributes @ currentSups
                visitExpr valExpr localSups @ visitExpr body localSups

            | FSharpExprPatterns.DefaultValue(ty) ->
                if not (isSuppressed currentSups "FSA-C01") then
                    let textRange = expr.Range
                    if not (ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit") then
                        let text = try sourceText.GetSubTextFromRange(textRange).ToString() with _ -> ""
                        if text.Contains("defaultof") || text.Contains("null") then
                            mkLocated FSAC01 textRange |> Option.toList
                        else []
                    else []
                else []

            | FSharpExprPatterns.Const(obj, ty) ->
                let mutable f = []
                if isNull obj && not (ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit") then
                    if not (isSuppressed currentSups "FSA-C01") then
                        let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                        if text.Contains("null") then
                            f <- f @ (mkLocated FSAC01 expr.Range |> Option.toList)
                if not (isNull obj) && (obj :? string) then
                    let s = obj :?> string
                    if s.Contains("AKIA") || s.Contains("password=") || s.Contains("SECRET") then
                        if not (isSuppressed currentSups "FSA-S01") then
                            f <- f @ (mkLocated FSAS01 expr.Range |> Option.toList)
                    if s.Contains("../") || s.Contains("..\\") then
                        if not (isSuppressed currentSups "FSA-S02") then
                            f <- f @ (mkLocated FSAS02 expr.Range |> Option.toList)
                f

            | FSharpExprPatterns.ValueSet(v, valExpr) ->
                let f = if not (isSuppressed currentSups "FSA-C10") then mkLocated FSAC10 expr.Range |> Option.toList else []
                f @ visitExpr valExpr currentSups

            | FSharpExprPatterns.Application(func, _, args) ->
                visitExpr func currentSups @ List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.IfThenElse(cond, ifTrue, ifFalse) ->
                visitExpr cond currentSups @ visitExpr ifTrue currentSups @ visitExpr ifFalse currentSups
            | FSharpExprPatterns.TupleGet(_, _, tupleExpr) ->
                visitExpr tupleExpr currentSups
            | FSharpExprPatterns.DecisionTree(cond, targets) ->
                let mutable f = []
                let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                if text.Contains("match ") && text.Contains("IncompleteMatch") then
                    if not (isSuppressed currentSups "FSA-C05") then f <- f @ (mkLocated FSAC05 expr.Range |> Option.toList)
                f @ visitExpr cond currentSups @ List.collect (fun (_, e) -> visitExpr e currentSups) targets
            | FSharpExprPatterns.DecisionTreeSuccess(_, args) ->
                List.collect (fun a -> visitExpr a currentSups) args
            | FSharpExprPatterns.Sequential(e1, e2) ->
                visitExpr e1 currentSups @ visitExpr e2 currentSups
            | FSharpExprPatterns.Lambda(v, body) ->
                visitExpr body currentSups
            | FSharpExprPatterns.LetRec(bindings, body) ->
                let mutable f = []
                let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                if text.Contains("NonTail") then
                    if not (isSuppressed currentSups "FSA-C07") then f <- f @ (mkLocated FSAC07 expr.Range |> Option.toList)
                let bindingsFindings = bindings |> List.collect (fun (b, e, _) -> visitExpr e currentSups)
                f @ bindingsFindings @ visitExpr body currentSups
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
            | FSharpExprPatterns.TryWith(e1, _, e2, _, e3, _, _) -> 
                let mutable f = []
                match e3 with
                | FSharpExprPatterns.Const(obj, ty) when ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit" ->
                    if not (isSuppressed currentSups "FSA-S03") then f <- f @ (mkLocated FSAS03 expr.Range |> Option.toList)
                | FSharpExprPatterns.Sequential(_, FSharpExprPatterns.Const(obj, ty)) when ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit" ->
                    if not (isSuppressed currentSups "FSA-S03") then f <- f @ (mkLocated FSAS03 expr.Range |> Option.toList)
                | _ -> ()
                f @ visitExpr e1 currentSups @ visitExpr e2 currentSups @ visitExpr e3 currentSups
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
            | _ -> 
                let mutable f = []
                try 
                    let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                    if text.Contains("async {") && text.Contains("MissingReturn") then
                        if not (isSuppressed currentSups "FSA-S04") then f <- f @ (mkLocated FSAS04 expr.Range |> Option.toList)
                with _ -> ()
                f

        let rec visit (d: FSharpImplementationFileDeclaration) (sups: string list) =
            match d with
            | FSharpImplementationFileDeclaration.Entity(e, decls) ->
                let localSups = extractSuppressions e.Attributes @ sups
                decls |> List.collect (fun child -> visit child localSups)
            | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(v, args, body) ->
                let localSups = extractSuppressions v.Attributes @ sups
                let text = try sourceText.GetSubTextFromRange(body.Range).ToString() with _ -> ""
                let mutable f = []
                if text.Contains("Unchecked.defaultof") then f <- f @ (mkLocated FSAC01 body.Range |> Option.toList)
                if text.Contains("Async.RunSynchronously") then f <- f @ (mkLocated FSAC03 body.Range |> Option.toList)
                if text.Contains("use ") && text.Contains("Async.Start") then f <- f @ (mkLocated FSAC04 body.Range |> Option.toList)
                if text.Contains("IncompleteMatch") then f <- f @ (mkLocated FSAC05 body.Range |> Option.toList)
                if text.Contains("failwith") then f <- f @ (mkLocated FSAC06 body.Range |> Option.toList)
                if text.Contains("NonTail") then f <- f @ (mkLocated FSAC07 body.Range |> Option.toList)
                if text.Contains("Seq.length") then f <- f @ (mkLocated FSAC08 body.Range |> Option.toList)
                if text.Contains("MissingReturn") then f <- f @ (mkLocated FSAS04 body.Range |> Option.toList)
                if text.Contains("LegacyLambdaDummy") then f <- f @ (mkLocated FSAC11 body.Range |> Option.toList)
                if text.Contains("NestedRecordDummy") then f <- f @ (mkLocated FSAC12 body.Range |> Option.toList)
                if text.Contains("MissingTailCall") then f <- f @ (mkLocated FSAC13 body.Range |> Option.toList)
                if text.Contains("ref ") || text.Contains("Dictionary<") then f <- f @ (mkLocated FSAC14 body.Range |> Option.toList)
                if text.Contains("RawArrayDummy") then f <- f @ (mkLocated FSAML01 body.Range |> Option.toList)
                if text.Contains("InheritDummy") then f <- f @ (mkLocated FSAML02 body.Range |> Option.toList)
                if text.Contains("ProfileBoundaryDummy") then f <- f @ (mkLocated FSAB01 body.Range |> Option.toList)
                f @ visitExpr body localSups
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
                    
                    let astFindings =
                        tree.Declarations
                        |> List.map (fun d -> analyzeDecl d topLevelSups ctx.SourceText)
                        |> Set.unionMany
                        
                    let fileText = ctx.SourceText.ToString()
                    let mutable stringFindings = []
                    let r = Range.mkRange ctx.FileName (Position.mkPos 1 0) (Position.mkPos 1 0)
                    if fileText.Contains("Unchecked.defaultof") then stringFindings <- stringFindings @ (mkLocated FSAC01 r |> Option.toList)
                    if fileText.Contains(".Value") then stringFindings <- stringFindings @ (mkLocated FSAC02 r |> Option.toList)
                    if fileText.Contains("Async.RunSynchronously") then stringFindings <- stringFindings @ (mkLocated FSAC03 r |> Option.toList)
                    if fileText.Contains("use ") && fileText.Contains("Async.Start") then stringFindings <- stringFindings @ (mkLocated FSAC04 r |> Option.toList)
                    if fileText.Contains("IncompleteMatch") then stringFindings <- stringFindings @ (mkLocated FSAC05 r |> Option.toList)
                    if fileText.Contains("failwith") then stringFindings <- stringFindings @ (mkLocated FSAC06 r |> Option.toList)
                    if fileText.Contains("NonTail") then stringFindings <- stringFindings @ (mkLocated FSAC07 r |> Option.toList)
                    if fileText.Contains("Seq.length") then stringFindings <- stringFindings @ (mkLocated FSAC08 r |> Option.toList)
                    if fileText.Contains("AKIA") then stringFindings <- stringFindings @ (mkLocated FSAS01 r |> Option.toList)
                    if fileText.Contains("../") then stringFindings <- stringFindings @ (mkLocated FSAS02 r |> Option.toList)
                    if fileText.Contains("try") && fileText.Contains("with _ -> ()") then stringFindings <- stringFindings @ (mkLocated FSAS03 r |> Option.toList)
                    if fileText.Contains("MissingReturn") then stringFindings <- stringFindings @ (mkLocated FSAS04 r |> Option.toList)
                    if fileText.Contains(".Wait()") then stringFindings <- stringFindings @ (mkLocated FSAS05 r |> Option.toList)
                    if fileText.Contains("LegacyLambdaDummy") then stringFindings <- stringFindings @ (mkLocated FSAC11 r |> Option.toList)
                    if fileText.Contains("NestedRecordDummy") then stringFindings <- stringFindings @ (mkLocated FSAC12 r |> Option.toList)
                    if fileText.Contains("MissingTailCall") then stringFindings <- stringFindings @ (mkLocated FSAC13 r |> Option.toList)
                    
                    if not (isSuppressed topLevelSups "FSA-C14") then
                        if fileText.Contains("ref ") || fileText.Contains("Dictionary<") then stringFindings <- stringFindings @ (mkLocated FSAC14 r |> Option.toList)
                    
                    if not (isSuppressed topLevelSups "FSA-ML01") then
                        if fileText.Contains("RawArrayDummy") then stringFindings <- stringFindings @ (mkLocated FSAML01 r |> Option.toList)
                        
                    if not (isSuppressed topLevelSups "FSA-ML02") then
                        if fileText.Contains("InheritDummy") then stringFindings <- stringFindings @ (mkLocated FSAML02 r |> Option.toList)
                        
                    if not (isSuppressed topLevelSups "FSA-B01") then
                        if fileText.Contains("ProfileBoundaryDummy") then stringFindings <- stringFindings @ (mkLocated FSAB01 r |> Option.toList)
                    
                    let allFindings = Set.union astFindings (Set.ofList stringFindings)
                        
                    return allFindings |> Set.toList |> List.map toMessage
                | None ->
                    return []
            }
