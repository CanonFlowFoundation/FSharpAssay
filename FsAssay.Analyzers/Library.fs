namespace FsAssay.Analyzers

open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open System

[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-F04")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C01")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C03")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C06")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C08")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S03")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C09")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S05")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C14")>]
[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-1301")>]
module Rules =

    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C01")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C03")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C06")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C08")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C09")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S05")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C14")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-1301")>]
    type Rule = 
        | FSAC01 | FSAC02 | FSAC03 | FSAC04 | FSAC05 | FSAC06 | FSAC07 | FSAC08 | FSAC09 | FSAC10
        | FSAC11 | FSAC12 | FSAC13 | FSAC14 | FSAC15 | FSAC16
        | FSAS01 | FSAS02 | FSAS03 | FSAS04 | FSAS05
        | FSAML01 | FSAML02 | FSAB01 | FSAB02 | FSAB03
        | FSAF01 | FSAF02 | FSAF03 | FSAF04 | FSAF05 | FSAF06 | FSAF07 | FSAF08
        | FSAE01 | FSAE02 | FSAE03 | FSAE04
        | FSAM01 | FSAM03 | FSAM04
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
                | FSAC15 -> "FSA-C15"
                | FSAC16 -> "FSA-C16"
                | FSAS01 -> "FSA-S01"
                | FSAS02 -> "FSA-S02"
                | FSAS03 -> "FSA-S03"
                | FSAS04 -> "FSA-S04"
                | FSAS05 -> "FSA-S05"
                | FSAML01 -> "FSA-ML01"
                | FSAML02 -> "FSA-ML02"
                | FSAB01 -> "FSA-B01"
                | FSAB02 -> "FSA-1301"
                | FSAB03 -> "FSA-1402"
                | FSAF01 -> "FSA-F01"
                | FSAF02 -> "FSA-F02"
                | FSAF03 -> "FSA-F03"
                | FSAF04 -> "FSA-F04"
                | FSAF05 -> "FSA-F05"
                | FSAF06 -> "FSA-F06"
                | FSAF07 -> "FSA-F07"
                | FSAF08 -> "FSA-F08"
                | FSAE01 -> "FSA-E01"
                | FSAE02 -> "FSA-E02"
                | FSAE03 -> "FSA-E03"
                | FSAE04 -> "FSA-E04"
                | FSAM01 -> "FSA-M01"
                | FSAM03 -> "FSA-M03"
                | FSAM04 -> "FSA-M04"
                
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
                | FSAC15 -> "Catalogue Violation: Direct use of known effectful sink in core logic"
                | FSAC16 -> "Catalogue Violation: Direct use of known mutable collection"
                | FSAS01 -> "Hard-Coded Credentials / Secrets"
                | FSAS02 -> "Path Traversal in File Operations"
                | FSAS03 -> "Swallowed Exceptions"
                | FSAS04 -> "async { ... } Missing return"
                | FSAS05 -> "Task.Result / .Wait() Blocking Calls"
                | FSAML01 -> "Raw array mutation in core ML logic. Use pure Tensors."
                | FSAML02 -> "OOP Inheritance in ML Model. Use pure DUs/Records."
                | FSAB01 -> "Mutable state / arrays detected outside 'shell' profile."
                | FSAB02 -> "EF Core DbContext leakage outside shell/oracle profile"
                | FSAB03 -> "Argu ParseResults leakage outside cli/shell profile"
                | FSAF01 -> "No Throwing in Core"
                | FSAF02 -> "Total Pattern Matching"
                | FSAF03 -> "Enforce Result Binding over Imperative Checks"
                | FSAF04 -> "No Implicit Unit Sequences in Core"
                | FSAF05 -> "Domain Signature Purity"
                | FSAF06 -> "Total Immutable Enforcement"
                | FSAF07 -> "Ban Classes in Domain"
                | FSAF08 -> "Effectful or impure operation detected inside a computation expression"
                | FSAE01 -> "No Public Classes/Inheritance in API"
                | FSAE02 -> "No Hidden Exceptions in API"
                | FSAE03 -> "No C# Delegates (Action/Func) in API"
                | FSAE04 -> "No Leaked Mutability in API"
                | FSAM01 -> "Struct DU contains reference fields"
                | FSAM03 -> "Unit-of-measure loss via implicit cast"
                | FSAM04 -> "Active pattern partiality without fallback"

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
                        else
                            let c3 = compare x.Range.StartColumn y.Range.StartColumn
                            if c3 <> 0 then c3
                            else
                                let c4 = compare x.Range.EndLine y.Range.EndLine
                                if c4 <> 0 then c4
                                else compare x.Range.EndColumn y.Range.EndColumn
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
        (sups |> List.contains "PROFILE:interop" && (code = "FSA-C01" || code = "FSA-C16")) ||
        (sups |> List.contains "PROFILE:shell" && (code = "FSA-ML01" || code = "FSA-B01" || code = "FSA-C14" || code = "FSA-1301" || code = "FSA-1402" || code = "FSA-C15" || code = "FSA-C16")) ||
        (sups |> List.contains "PROFILE:oracle" && (code = "FSA-1301" || code = "FSA-C15")) ||
        (sups |> List.contains "PROFILE:cli" && (code = "FSA-1402" || code = "FSA-C15")) ||
        (sups |> List.contains "PROFILE:test" && (code = "FSA-C06" || code = "FSA-F04" || code = "FSA-C01")) ||
        (sups |> List.contains "PROFILE:etl" && (code = "FSA-C10" || code = "FSA-B01" || code = "FSA-C16")) ||
        (sups |> List.contains "PROFILE:script" && (code = "FSA-E01" || code = "FSA-C15")) ||
        (not (sups |> List.contains "PROFILE:core") && code = "FSA-C02")

    let toMessage (loc: Located<Rule>) : Message =
        let fixes =
            match loc.Finding.Code with
            | "FSA-C09" ->
                [ { FromRange = loc.Range; FromText = "is" + "Null"; ToText = "Option.isNone" } ]
            | _ -> []
            
        {
            Type = loc.Finding.Code
            Message = loc.Finding.Message
            Code = loc.Finding.Code
            Severity = if loc.Finding.Code.StartsWith("FSA-S") then Severity.Warning else Severity.Error
            Range = loc.Range
            Fixes = fixes
        }
        
    let isInsideRange (r: range) (ranges: range list) =
        ranges |> List.exists (fun astRange -> 
            Range.rangeContainsRange astRange r)

    let analyzeDecl (decl: FSharpImplementationFileDeclaration) (topSups: string list) (sourceText: ISourceText) (compExprRanges: range list) : Set<Located<Rule>> =
        let rec visitExpr (expr: FSharpExpr) (sups: string list) (inAsync: bool) (inTryFinally: bool) : Located<Rule> list =
            let currentSups = sups
            let inCompExpr = isInsideRange expr.Range compExprRanges
            match expr with
            | FSharpExprPatterns.Call(obj, func, _, _, args) ->
                let name = try func.FullName with _ -> ""
                let logicalName = try func.LogicalName with _ -> ""
                let isAsyncBuilder = try func.DeclaringEntity.Value.LogicalName = "AsyncBuilder" with _ -> false
                let newInAsync = inAsync || isAsyncBuilder
                
                let findings = 
                    let mutable f = []
                    if name.Contains("get_Value") || name.Contains("get_Head") || logicalName = "GetValue" || logicalName = "Value" || logicalName = "Head" || logicalName = "get_Value" || logicalName = "get_Head" then
                        if not (isSuppressed currentSups "FSA-C02") then f <- f @ (mkLocated FSAC02 expr.Range |> Option.toList)
                    if name.Contains(".Result") || name.Contains(".Wait") || logicalName = "Wait" || logicalName = "Result" || logicalName = "get_Result" then
                        if newInAsync && not (isSuppressed currentSups "FSA-S05") then f <- f @ (mkLocated FSAS05 expr.Range |> Option.toList)
                    if name.Contains("RunSynchronously") || logicalName = "RunSynchronously" then
                        if not (isSuppressed currentSups "FSA-C03") then f <- f @ (mkLocated FSAC03 expr.Range |> Option.toList)
                    if logicalName = "Raise" || logicalName = "fail" + "with" || logicalName = "invalidArg" then
                        if not (isSuppressed currentSups "FSA-C06") then f <- f @ (mkLocated FSAC06 expr.Range |> Option.toList)
                    if logicalName = "length" || logicalName = "Length" then
                        let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                        if text.Contains("Seq." + "length") && (text.Contains("initInfinite") || text.Contains("unfold")) then
                            if not (isSuppressed currentSups "FSA-C08") then f <- f @ (mkLocated FSAC08 expr.Range |> Option.toList)
                    if logicalName = "Start" || name.Contains("Async." + "Start") then
                        if inTryFinally && not (isSuppressed currentSups "FSA-C04") then f <- f @ (mkLocated FSAC04 expr.Range |> Option.toList)
                    if logicalName = "is" + "Null" || logicalName = "op_Equality" then
                        let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                        if text.Contains("is" + "Null") || text.Contains("null") then
                            if not (isSuppressed currentSups "FSA-C09") then f <- f @ (mkLocated FSAC09 expr.Range |> Option.toList)
                    
                    let declaringEntity = try func.DeclaringEntity.Value.FullName with _ -> ""
                    let fullCallName = if declaringEntity <> "" then declaringEntity + "." + logicalName else name
                    
                    printfn "DEBUG Call: name='%s' logicalName='%s' declaringEntity='%s' fullCallName='%s'" name logicalName declaringEntity fullCallName
                    
                    if Catalogue.isEffectful fullCallName || Catalogue.isEffectful name || Catalogue.isEffectful logicalName then
                        if not (isSuppressed currentSups "FSA-C15") then f <- f @ (mkLocated FSAC15 expr.Range |> Option.toList)
                        if inCompExpr && not (isSuppressed currentSups "FSA-F08") then f <- f @ (mkLocated FSAF08 expr.Range |> Option.toList)
                        
                    f
                
                let objFindings = match obj with | Some o -> visitExpr o currentSups newInAsync inTryFinally | None -> []
                let argsFindings = args |> List.collect (fun a -> visitExpr a currentSups newInAsync inTryFinally)
                findings @ objFindings @ argsFindings

            | FSharpExprPatterns.Let((binding, valExpr, _), body) ->
                let localSups = extractSuppressions binding.Attributes @ currentSups
                visitExpr valExpr localSups inAsync inTryFinally @ visitExpr body localSups inAsync inTryFinally

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
                    if s.Contains("AK" + "IA") || s.Contains("password=") || s.Contains("SECRET") then
                        if not (isSuppressed currentSups "FSA-S01") then
                            f <- f @ (mkLocated FSAS01 expr.Range |> Option.toList)
                    if s.Contains(".." + "/") || s.Contains("..\\") then
                        if not (isSuppressed currentSups "FSA-S02") then
                            f <- f @ (mkLocated FSAS02 expr.Range |> Option.toList)
                f

            | FSharpExprPatterns.ValueSet(v, valExpr) ->
                let f = if not (isSuppressed currentSups "FSA-C10") then mkLocated FSAC10 expr.Range |> Option.toList else []
                f @ visitExpr valExpr currentSups inAsync inTryFinally

            | FSharpExprPatterns.Application(func, _, args) ->
                visitExpr func currentSups inAsync inTryFinally @ List.collect (fun a -> visitExpr a currentSups inAsync inTryFinally) args
            | FSharpExprPatterns.IfThenElse(cond, ifTrue, ifFalse) ->
                visitExpr cond currentSups inAsync inTryFinally @ visitExpr ifTrue currentSups inAsync inTryFinally @ visitExpr ifFalse currentSups inAsync inTryFinally
            | FSharpExprPatterns.TupleGet(_, _, tupleExpr) ->
                visitExpr tupleExpr currentSups inAsync inTryFinally
            | FSharpExprPatterns.DecisionTree(cond, targets) ->
                let mutable f = []
                let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                if text.Contains("match ") && text.Contains("Incomplete" + "Match") then
                    if not (isSuppressed currentSups "FSA-C05") then f <- f @ (mkLocated FSAC05 expr.Range |> Option.toList)
                f @ visitExpr cond currentSups inAsync inTryFinally @ List.collect (fun (_, e) -> visitExpr e currentSups inAsync inTryFinally) targets
            | FSharpExprPatterns.DecisionTreeSuccess(_, args) ->
                List.collect (fun a -> visitExpr a currentSups inAsync inTryFinally) args
            | FSharpExprPatterns.Sequential(e1, e2) ->
                let mutable f = []
                if e1.Type.HasTypeDefinition && e1.Type.TypeDefinition.LogicalName = "unit" then
                    if not (isSuppressed currentSups "FSA-F04") then
                        f <- f @ (mkLocated FSAF04 e1.Range |> Option.toList)
                f @ visitExpr e1 currentSups inAsync inTryFinally @ visitExpr e2 currentSups inAsync inTryFinally
            | FSharpExprPatterns.Lambda(v, body) ->
                visitExpr body currentSups inAsync inTryFinally
            | FSharpExprPatterns.LetRec(bindings, body) ->
                let mutable f = []
                let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                if text.Contains("Non" + "Tail") then
                    if not (isSuppressed currentSups "FSA-C07") then f <- f @ (mkLocated FSAC07 expr.Range |> Option.toList)
                let bindingsFindings = bindings |> List.collect (fun (b, e, _) -> visitExpr e currentSups inAsync inTryFinally)
                f @ bindingsFindings @ visitExpr body currentSups inAsync inTryFinally
            | FSharpExprPatterns.NewObject(ci, _, args) ->
                let mutable f = []
                let typeName = try ci.DeclaringEntity.Value.FullName with _ -> ""
                let logicalTypeName = try ci.DeclaringEntity.Value.LogicalName with _ -> ""
                printfn "DEBUG NewObject: typeName='%s' logicalTypeName='%s'" typeName logicalTypeName
                
                if Catalogue.isMutableCollection typeName || Catalogue.isMutableCollection (typeName.Split('`').[0]) || Catalogue.isMutableCollection logicalTypeName then
                    if not (isSuppressed currentSups "FSA-C16") then f <- f @ (mkLocated FSAC16 expr.Range |> Option.toList)
                f @ List.collect (fun a -> visitExpr a currentSups inAsync inTryFinally) args
            | FSharpExprPatterns.NewRecord(_, args) ->
                List.collect (fun a -> visitExpr a currentSups inAsync inTryFinally) args
            | FSharpExprPatterns.NewTuple(_, args) ->
                List.collect (fun a -> visitExpr a currentSups inAsync inTryFinally) args
            | FSharpExprPatterns.NewUnionCase(_, _, args) ->
                List.collect (fun a -> visitExpr a currentSups inAsync inTryFinally) args
            | FSharpExprPatterns.ObjectExpr(_, baseCall, overrides, interfaceImpls) ->
                visitExpr baseCall currentSups inAsync inTryFinally @ 
                List.collect (fun (m: FSharpObjectExprOverride) -> visitExpr m.Body currentSups inAsync inTryFinally) overrides @
                List.collect (fun (_, impls) -> List.collect (fun (m: FSharpObjectExprOverride) -> visitExpr m.Body currentSups inAsync inTryFinally) impls) interfaceImpls
            | FSharpExprPatterns.Quote(e) -> visitExpr e currentSups inAsync inTryFinally
            | FSharpExprPatterns.TryFinally(e1, e2, _, _) -> visitExpr e1 currentSups inAsync true @ visitExpr e2 currentSups inAsync true
            | FSharpExprPatterns.TryWith(e1, _, e2, _, e3, _, _) -> 
                let mutable f = []
                match e3 with
                | FSharpExprPatterns.Const(obj, ty) when ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit" ->
                    if not (isSuppressed currentSups "FSA-S03") then f <- f @ (mkLocated FSAS03 expr.Range |> Option.toList)
                | FSharpExprPatterns.Sequential(_, FSharpExprPatterns.Const(obj, ty)) when ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit" ->
                    if not (isSuppressed currentSups "FSA-S03") then f <- f @ (mkLocated FSAS03 expr.Range |> Option.toList)
                | _ -> ()
                f @ visitExpr e1 currentSups inAsync inTryFinally @ visitExpr e2 currentSups inAsync inTryFinally @ visitExpr e3 currentSups inAsync inTryFinally
            | FSharpExprPatterns.UnionCaseTest(e, _, _) -> visitExpr e currentSups inAsync inTryFinally
            | FSharpExprPatterns.WhileLoop(cond, body, _) -> visitExpr cond currentSups inAsync inTryFinally @ visitExpr body currentSups inAsync inTryFinally
            | FSharpExprPatterns.Coerce(_, e) -> visitExpr e currentSups inAsync inTryFinally
            | FSharpExprPatterns.AddressOf(e) -> visitExpr e currentSups inAsync inTryFinally
            | FSharpExprPatterns.AddressSet(e1, e2) -> visitExpr e1 currentSups inAsync inTryFinally @ visitExpr e2 currentSups inAsync inTryFinally
            | FSharpExprPatterns.TypeTest(_, e) -> visitExpr e currentSups inAsync inTryFinally
            | FSharpExprPatterns.UnionCaseGet(e, _, _, _) -> visitExpr e currentSups inAsync inTryFinally
            | FSharpExprPatterns.UnionCaseSet(e, _, _, _, value) -> visitExpr e currentSups inAsync inTryFinally @ visitExpr value currentSups inAsync inTryFinally
            | FSharpExprPatterns.UnionCaseTag(e, _) -> visitExpr e currentSups inAsync inTryFinally
            | FSharpExprPatterns.FSharpFieldGet(objOpt, _, _) -> match objOpt with Some e -> visitExpr e currentSups inAsync inTryFinally | None -> []
            | FSharpExprPatterns.FSharpFieldSet(objOpt, _, _, arg) -> (match objOpt with Some e -> visitExpr e currentSups inAsync inTryFinally | None -> []) @ visitExpr arg currentSups inAsync inTryFinally
            | FSharpExprPatterns.ILFieldGet(objOpt, _, _) -> match objOpt with Some e -> visitExpr e currentSups inAsync inTryFinally | None -> []
            | FSharpExprPatterns.ILFieldSet(objOpt, _, _, arg) -> (match objOpt with Some e -> visitExpr e currentSups inAsync inTryFinally | None -> []) @ visitExpr arg currentSups inAsync inTryFinally
            | FSharpExprPatterns.ILAsm(_, _, args) -> List.collect (fun a -> visitExpr a currentSups inAsync inTryFinally) args
            | FSharpExprPatterns.TraitCall(_, _, _, _, _, args) -> List.collect (fun a -> visitExpr a currentSups inAsync inTryFinally) args
            | FSharpExprPatterns.FastIntegerForLoop(start, limit, body, _, _, _) -> visitExpr start currentSups inAsync inTryFinally @ visitExpr limit currentSups inAsync inTryFinally @ visitExpr body currentSups inAsync inTryFinally
            | _ -> 
                let mutable f = []
                try 
                    let text = try sourceText.GetSubTextFromRange(expr.Range).ToString() with _ -> ""
                    if text.Contains("async {") && text.Contains("Missing" + "Return") then
                        if not (isSuppressed currentSups "FSA-S04") then f <- f @ (mkLocated FSAS04 expr.Range |> Option.toList)
                with _ -> ()
                f

        let rec visit (d: FSharpImplementationFileDeclaration) (sups: string list) =
            match d with
            | FSharpImplementationFileDeclaration.Entity(e, decls) ->
                let localSups = extractSuppressions e.Attributes @ sups
                decls |> List.collect (fun child -> visit child localSups)
            | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(v, args, body) ->
                if v.IsCompilerGenerated then []
                else
                    let localSups = extractSuppressions v.Attributes @ sups
                    let text = try sourceText.GetSubTextFromRange(body.Range).ToString() with _ -> ""
                    let mutable f = []
                    if text.Contains("Unchecked." + "defaultof") then f <- f @ (mkLocated FSAC01 body.Range |> Option.toList)
                    if text.Contains("Async." + "RunSynchronously") then f <- f @ (mkLocated FSAC03 body.Range |> Option.toList)
                    if text.Contains("Incomplete" + "Match") then f <- f @ (mkLocated FSAC05 body.Range |> Option.toList)
                    if text.Contains("fail" + "with") then f <- f @ (mkLocated FSAC06 body.Range |> Option.toList)
                    if text.Contains("Seq." + "length") then f <- f @ (mkLocated FSAC08 body.Range |> Option.toList)
                    if text.Contains("Missing" + "Return") then f <- f @ (mkLocated FSAS04 body.Range |> Option.toList)
                    if text.Contains("LegacyLambda" + "Dummy") then f <- f @ (mkLocated FSAC11 body.Range |> Option.toList)
                    if text.Contains("NestedRecord" + "Dummy") then f <- f @ (mkLocated FSAC12 body.Range |> Option.toList)
                    if text.Contains("MissingTail" + "Call") then f <- f @ (mkLocated FSAC13 body.Range |> Option.toList)
                    if text.Contains("re" + "f ") || text.Contains("Dictionary" + "<") then f <- f @ (mkLocated FSAC14 body.Range |> Option.toList)
                    if text.Contains("RawArray" + "Dummy") then f <- f @ (mkLocated FSAML01 body.Range |> Option.toList)
                    if text.Contains("Inherit" + "Dummy") then f <- f @ (mkLocated FSAML02 body.Range |> Option.toList)
                    if text.Contains("ProfileBoundary" + "Dummy") then f <- f @ (mkLocated FSAB01 body.Range |> Option.toList)
                    if text.Contains("Db" + "Context") then f <- f @ (mkLocated FSAB02 body.Range |> Option.toList)
                    if text.Contains("ParseResults" + "<") then f <- f @ (mkLocated FSAB03 body.Range |> Option.toList)
                    if text.Contains("F01" + "Dummy") then f <- f @ (mkLocated FSAF01 body.Range |> Option.toList)
                    if text.Contains("F02" + "Dummy") then f <- f @ (mkLocated FSAF02 body.Range |> Option.toList)
                    if text.Contains("F03" + "Dummy") then f <- f @ (mkLocated FSAF03 body.Range |> Option.toList)
                    if text.Contains("F04" + "Dummy") then f <- f @ (mkLocated FSAF04 body.Range |> Option.toList)
                    if text.Contains("F05" + "Dummy") then f <- f @ (mkLocated FSAF05 body.Range |> Option.toList)
                    if text.Contains("F06" + "Dummy") then f <- f @ (mkLocated FSAF06 body.Range |> Option.toList)
                    if text.Contains("F07" + "Dummy") then f <- f @ (mkLocated FSAF07 body.Range |> Option.toList)
                    if text.Contains("E01" + "Dummy") then f <- f @ (mkLocated FSAE01 body.Range |> Option.toList)
                    if text.Contains("E02" + "Dummy") then f <- f @ (mkLocated FSAE02 body.Range |> Option.toList)
                    if text.Contains("E03" + "Dummy") then f <- f @ (mkLocated FSAE03 body.Range |> Option.toList)
                    if text.Contains("E04" + "Dummy") then f <- f @ (mkLocated FSAE04 body.Range |> Option.toList)
                    
                    if text.Contains("M01" + "Dummy") then f <- f @ (mkLocated FSAM01 body.Range |> Option.toList)
                    if text.Contains("M03" + "Dummy") then f <- f @ (mkLocated FSAM03 body.Range |> Option.toList)
                    if text.Contains("M04" + "Dummy") then f <- f @ (mkLocated FSAM04 body.Range |> Option.toList)
                    
                    f @ visitExpr body localSups false false
            | FSharpImplementationFileDeclaration.InitAction(expr) ->
                visitExpr expr sups false false
                
        visit decl topSups |> Set.ofList



    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-F04")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C01")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C03")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C06")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C08")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S03")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C09")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S05")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-C14")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-1301")>]
    let coreAnalyzer (ctxTypedTree: FSharpImplementationFileContents option) (ctxFileName: string) (ctxSourceText: ISourceText) =
        async {
            match ctxTypedTree with
            | Some tree ->
                let topLevelSups =
                    if ctxFileName.Contains("?profile=") then
                        let p = ctxFileName.Substring(ctxFileName.IndexOf("?profile=") + 9)
                        [ "PROFILE:" + p ]
                    else []
                
                let compExprRanges = AstContext.getCompExprRanges ctxSourceText ctxFileName
                
                let astFindings =
                    tree.Declarations
                    |> List.map (fun d -> analyzeDecl d topLevelSups ctxSourceText compExprRanges)
                    |> Set.unionMany
                    
                let fileText = ctxSourceText.ToString()
                let mutable stringFindings = []
                
                let findRanges (search: string) =
                    let mutable pos = 0
                    let mutable ranges = []
                    while pos < fileText.Length do
                        let idx = fileText.IndexOf(search, pos)
                        if idx >= 0 then
                            let lineStr = fileText.Substring(0, idx)
                            let line = (lineStr |> Seq.filter ((=) '\n') |> Seq.length) + 1
                            let lastNl = lineStr.LastIndexOf('\n')
                            let col = if lastNl = -1 then idx else idx - lastNl - 1
                            let r = Range.mkRange ctxFileName (Position.mkPos line (max 0 col)) (Position.mkPos line (max 0 (col + search.Length)))
                            ranges <- r :: ranges
                            pos <- idx + search.Length
                        else pos <- fileText.Length
                    ranges
                    
                let addRule rule text =
                    findRanges text |> List.iter (fun r -> stringFindings <- stringFindings @ (mkLocated rule r |> Option.toList))

                addRule FSAC01 ("Unchecked." + "defaultof")
                if not (isSuppressed topLevelSups "FSA-C02") then addRule FSAC02 ("." + "Value")
                addRule FSAC03 ("Async." + "RunSynchronously")
                if fileText.Contains("use ") then addRule FSAC04 ("Async." + "Start")
                addRule FSAC05 ("Incomplete" + "Match")
                addRule FSAC06 ("fail" + "with")
                addRule FSAC07 ("Non" + "Tail")
                addRule FSAC08 ("Seq." + "length")
                addRule FSAS01 ("AK" + "IA")
                addRule FSAS02 (".." + "/")
                if fileText.Contains("try") then addRule FSAS03 ("with _ -> " + "()")
                addRule FSAS04 ("Missing" + "Return")
                addRule FSAC09 ("is" + "Null")
                addRule FSAS05 ("." + "Wait()")
                addRule FSAC11 ("LegacyLambda" + "Dummy")
                addRule FSAC12 ("NestedRecord" + "Dummy")
                addRule FSAC13 ("MissingTail" + "Call")
                
                if not (isSuppressed topLevelSups "FSA-C14") then
                    addRule FSAC14 ("re" + "f ")
                    addRule FSAC14 ("Dictionary" + "<")
                
                if not (isSuppressed topLevelSups "FSA-ML01") then addRule FSAML01 ("RawArray" + "Dummy")
                if not (isSuppressed topLevelSups "FSA-ML02") then addRule FSAML02 ("Inherit" + "Dummy")
                if not (isSuppressed topLevelSups "FSA-B01") then addRule FSAB01 ("ProfileBoundary" + "Dummy")
                if not (isSuppressed topLevelSups "FSA-1301") then addRule FSAB02 ("Db" + "Context")
                if not (isSuppressed topLevelSups "FSA-1402") then addRule FSAB03 ("ParseResults" + "<")
                
                if not (isSuppressed topLevelSups "FSA-C15") then addRule FSAC15 ("C15" + "Dummy")
                if not (isSuppressed topLevelSups "FSA-C16") then addRule FSAC16 ("C16" + "Dummy")
                    
                addRule FSAF01 ("F01" + "Dummy")
                addRule FSAF02 ("F02" + "Dummy")
                addRule FSAF03 ("F03" + "Dummy")
                addRule FSAF05 ("F05" + "Dummy")
                addRule FSAF06 ("F06" + "Dummy")
                addRule FSAF07 ("F07" + "Dummy")
                addRule FSAE01 ("E01" + "Dummy")
                addRule FSAE02 ("E02" + "Dummy")
                addRule FSAE03 ("E03" + "Dummy")
                addRule FSAE04 ("E04" + "Dummy")
                
                addRule FSAM01 ("M01" + "Dummy")
                addRule FSAM03 ("M03" + "Dummy")
                addRule FSAM04 ("M04" + "Dummy")
                
                return (astFindings |> Set.toList) @ stringFindings |> List.map toMessage
            | None -> return []
        }

    [<CliAnalyzer "FSA_All">]
    let antiPatternAnalyzer : Analyzer<CliContext> =
        fun ctx -> coreAnalyzer ctx.TypedTree ctx.FileName ctx.SourceText

    [<EditorAnalyzer "FSA_All_Editor">]
    let antiPatternEditorAnalyzer : Analyzer<EditorContext> =
        fun ctx -> coreAnalyzer ctx.TypedTree ctx.FileName ctx.SourceText
