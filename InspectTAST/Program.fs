open System
open System.IO
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols

[<EntryPoint>]
let main argv =
    let checker = FSharpChecker.Create(keepAssemblyContents = true)
    let file = Path.Combine(Directory.GetCurrentDirectory(), "dummy.fs")
    let source = """
module Dummy
let doSomething () =
    let x = Unchecked.defaultof<int>
    x
"""
    let sourceText = SourceText.ofString source
    let options, _ = checker.GetProjectOptionsFromScript(file, sourceText) |> Async.RunSynchronously
    let parse, check = checker.ParseAndCheckFileInProject(file, 0, sourceText, options) |> Async.RunSynchronously
    match check with
    | FSharpCheckFileAnswer.Succeeded(res) ->
        let rec visitExpr (expr: FSharpExpr) =
            let exprTypeStr = expr.ToString()
            printfn "EXPR: %s" exprTypeStr
            match expr with
            | FSharpExprPatterns.Call(obj, func, _, _, args) ->
                printfn "CALL: %s" func.FullName
                args |> List.iter visitExpr
                obj |> Option.iter visitExpr
            | _ ->
                let prop = expr.GetType().GetProperty("ImmediateSubExpressions")
                if not (isNull prop) then
                    let subExprs = prop.GetValue(expr) :?> seq<FSharpExpr>
                    for e in subExprs do visitExpr e

        let rec visitDecl (decl: FSharpImplementationFileDeclaration) =
            match decl with
            | FSharpImplementationFileDeclaration.Entity(e, decls) ->
                decls |> List.iter visitDecl
            | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(v, args, body) ->
                visitExpr body
            | FSharpImplementationFileDeclaration.InitAction(expr) ->
                visitExpr expr

        res.ImplementationFile.Value.Declarations |> List.iter visitDecl
    | _ -> printfn "Check failed"
    0
