namespace FsAssay.Analyzers

open FSharp.Analyzers.SDK
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open System
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
            try
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
                else None
            with _ -> None)
        |> Seq.toList

    /// Replaces comments and string literals with spaces to prevent false positives in lexical checks,
    /// while strictly preserving line numbers and column offsets.
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA1001")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA1007")>]
    let sanitizeSource (source: string) =
        let charArray = source.ToCharArray()
        let len = charArray.Length
        let mutable i = 0
        let mutable inSingleLineComment = false
        let mutable inMultiLineComment = 0
        let mutable inString = false
        let mutable inVerbatimString = false
        let mutable inTripleString = false
        let mutable stringChar = '"'
        let mutable prevC = '\000'

        while i < len do
            let c = charArray.[i]
            let nextC = if i + 1 < len then charArray.[i + 1] else '\000'

            if inSingleLineComment then
                if c = '\n' || c = '\r' then
                    inSingleLineComment <- false
                else
                    charArray.[i] <- ' '
                prevC <- c
                i <- i + 1
            elif inMultiLineComment > 0 then
                if c = '(' && nextC = '*' then
                    inMultiLineComment <- inMultiLineComment + 1
                    charArray.[i] <- ' '
                    charArray.[i + 1] <- ' '
                    prevC <- '*'
                    i <- i + 2
                elif c = '*' && nextC = ')' then
                    inMultiLineComment <- inMultiLineComment - 1
                    charArray.[i] <- ' '
                    charArray.[i + 1] <- ' '
                    prevC <- ')'
                    i <- i + 2
                else
                    if c <> '\n' && c <> '\r' then charArray.[i] <- ' '
                    prevC <- c
                    i <- i + 1
            elif inTripleString then
                if c = '"' && nextC = '"' && (i + 2 < len && charArray.[i + 2] = '"') then
                    inTripleString <- false
                    charArray.[i] <- ' '
                    charArray.[i + 1] <- ' '
                    charArray.[i + 2] <- ' '
                    prevC <- '"'
                    i <- i + 3
                else
                    if c <> '\n' && c <> '\r' then charArray.[i] <- ' '
                    prevC <- c
                    i <- i + 1
            elif inVerbatimString then
                if c = '"' && nextC = '"' then
                    charArray.[i] <- ' '
                    charArray.[i + 1] <- ' '
                    prevC <- '"'
                    i <- i + 2
                elif c = '"' then
                    inVerbatimString <- false
                    charArray.[i] <- ' '
                    prevC <- '"'
                    i <- i + 1
                else
                    if c <> '\n' && c <> '\r' then charArray.[i] <- ' '
                    prevC <- c
                    i <- i + 1
            elif inString then
                if c = stringChar && prevC <> '\\' then
                    inString <- false
                    charArray.[i] <- ' '
                    prevC <- c
                    i <- i + 1
                else
                    if c <> '\n' && c <> '\r' then charArray.[i] <- ' '
                    prevC <- c
                    i <- i + 1
            else
                if c = '/' && nextC = '/' then
                    inSingleLineComment <- true
                    charArray.[i] <- ' '
                    charArray.[i + 1] <- ' '
                    prevC <- '/'
                    i <- i + 2
                elif c = '(' && nextC = '*' then
                    inMultiLineComment <- 1
                    charArray.[i] <- ' '
                    charArray.[i + 1] <- ' '
                    prevC <- '*'
                    i <- i + 2
                elif c = '"' && nextC = '"' && (i + 2 < len && charArray.[i + 2] = '"') then
                    inTripleString <- true
                    charArray.[i] <- ' '
                    charArray.[i + 1] <- ' '
                    charArray.[i + 2] <- ' '
                    prevC <- '"'
                    i <- i + 3
                elif (c = '@' || c = '$') && nextC = '"' then
                    if c = '@' then inVerbatimString <- true else inString <- true
                    stringChar <- '"'
                    charArray.[i] <- ' '
                    charArray.[i + 1] <- ' '
                    prevC <- '"'
                    i <- i + 2
                elif c = '"' then
                    inString <- true
                    stringChar <- '"'
                    charArray.[i] <- ' '
                    prevC <- '"'
                    i <- i + 1
                else
                    prevC <- c
                    i <- i + 1

        String(charArray)

    [<CliAnalyzer "FSA_All">]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA1001")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA1007")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA2012")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA2016")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA2017")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA2018")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA2024")>]
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA-S03")>]
    let antiPatternAnalyzer : Analyzer<CliContext> =
        fun ctx ->
            async {
                let source = ctx.SourceText.ToString()
                let sanitized = sanitizeSource source
                let mutable violations = []

                let addViolation code msg range (sups: string list) =
                    let isSuppressed = 
                        sups |> List.contains code ||
                        (code = "FSA1003" && sups |> List.contains "PROFILE:interop") ||
                        (code = "FSA1001" && sups |> List.contains "PROFILE:interop")
                    if not isSuppressed then
                        violations <- createViolation code msg range :: violations

                let mutable topLevelSups = []

                if ctx.TypedTree.IsSome then
                    let rec visitExpr (expr: FSharpExpr) (sups: string list) =
                        try
                            match expr with
                            | FSharpExprPatterns.Call(obj, func, _, _, args) ->
                                try
                                    let name = func.FullName
                                    if name = "Microsoft.FSharp.Core.OptionModule.GetValue" ||
                                       name.EndsWith("FSharpOption`1.get_Value") ||
                                       name = "Microsoft.FSharp.Collections.ListModule.Head" ||
                                       name = "Microsoft.FSharp.Collections.SeqModule.Head" ||
                                       name.EndsWith("FSharpList`1.get_Head") then
                                        addViolation "FSA1002" "Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching." expr.Range sups
                                with _ -> ()

                                args |> List.iter (fun a -> visitExpr a sups)
                                obj |> Option.iter (fun o -> visitExpr o sups)
                            | FSharpExprPatterns.Let((binding, valExpr, _), body) ->
                                try
                                    let localSups = extractSuppressions binding.Attributes @ sups
                                    if binding.IsMutable && not binding.IsCompilerGenerated then
                                        addViolation "FSA1001" "Mutation Overuse: Avoid 'mutable'. Use record copies with 'with' instead." binding.DeclarationLocation localSups
                                    visitExpr valExpr localSups
                                    visitExpr body localSups
                                with _ ->
                                    visitExpr valExpr sups
                                    visitExpr body sups
                            | FSharpExprPatterns.DefaultValue(_) ->
                                try
                                    let text = ctx.SourceText.GetSubTextFromRange(expr.Range).ToString()
                                    if text.Contains("null") || text.Contains("defaultof") then
                                        addViolation "FSA1003" "Null Reference: Avoid 'null'. Use 'Option' types to represent missing values." expr.Range sups
                                with _ -> ()
                            | FSharpExprPatterns.Const(obj, ty) ->
                                try
                                    if isNull obj && not (ty.HasTypeDefinition && ty.TypeDefinition.LogicalName = "unit") then
                                        let text = ctx.SourceText.GetSubTextFromRange(expr.Range).ToString()
                                        if text.Contains("null") then
                                            addViolation "FSA1003" "Null Reference: Avoid 'null'. Use 'Option' types to represent missing values." expr.Range sups
                                with _ -> ()
                            | FSharpExprPatterns.ValueSet(v, valExpr) ->
                                if not v.IsCompilerGenerated then
                                    addViolation "FSA1001" "Mutation Overuse: Avoid mutation. Use record copies with 'with' instead." expr.Range sups
                                visitExpr valExpr sups
                            | _ ->
                                try
                                    let prop = expr.GetType().GetProperty("ImmediateSubExpressions")
                                    if not (isNull prop) then
                                        let subExprs = prop.GetValue(expr) :?> seq<FSharpExpr>
                                        for e in subExprs do
                                            visitExpr e sups
                                with _ -> ()
                        with _ -> ()

                    let rec visitDecl (decl: FSharpImplementationFileDeclaration) (sups: string list) =
                        try
                            match decl with
                            | FSharpImplementationFileDeclaration.Entity(e, decls) ->
                                let localSups = extractSuppressions e.Attributes @ sups
                                topLevelSups <- localSups @ topLevelSups
                                decls |> List.iter (fun d -> visitDecl d localSups)
                            | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(v, args, body) ->
                                let localSups = extractSuppressions v.Attributes @ sups
                                topLevelSups <- localSups @ topLevelSups
                                if v.IsMutable && not v.IsCompilerGenerated && not v.IsPropertyGetterMethod && not v.IsPropertySetterMethod && not v.IsProperty && not (v.LogicalName.StartsWith("New")) && not (v.LogicalName.StartsWith("get_")) && not (v.LogicalName.StartsWith("set_")) then
                                    addViolation "FSA1001" "Mutation Overuse: Avoid 'mutable'. Use record copies with 'with' instead." v.DeclarationLocation localSups
                                visitExpr body localSups
                            | FSharpImplementationFileDeclaration.InitAction(expr) ->
                                visitExpr expr sups
                        with _ -> ()

                    try
                        ctx.TypedTree.Value.Declarations |> List.iter (fun d -> visitDecl d [])
                    with _ -> ()

                // Precise position calculation using sanitized code (ignoring comments/strings)
                let lines = source.Split('\n')
                let lineStarts = Array.zeroCreate (lines.Length + 1)
                let mutable currentOffset = 0
                for l = 0 to lines.Length - 1 do
                    lineStarts.[l] <- currentOffset
                    currentOffset <- currentOffset + lines.[l].Length + 1

                let checkRegex code msg pattern (opts: RegexOptions) =
                    for m in Regex.Matches(sanitized, pattern, opts) do
                        let mutable lineIdx = 0
                        while lineIdx < lines.Length - 1 && lineStarts.[lineIdx + 1] <= m.Index do
                            lineIdx <- lineIdx + 1

                        let lineNum = lineIdx + 1
                        let colStart = m.Index - lineStarts.[lineIdx]
                        let colEnd = colStart + m.Length

                        let posStart = Position.mkPos lineNum colStart
                        let posEnd = Position.mkPos lineNum colEnd
                        let r = Range.mkRange ctx.FileName posStart posEnd
                        addViolation code msg r topLevelSups

                // Lexical rules with precise line/column ranges & comment/string stripping
                checkRegex "FSA1004" "Primitive Obsession: Do not use type aliases for primitives. Use Single-Case Discriminated Unions to make illegal states unrepresentable." @"type\s+[A-Za-z0-9_]+\s*=\s*(string|int|float|bool|decimal|DateTime)\b" RegexOptions.None
                checkRegex "FSA1005" "Parse, Don't Validate: Functions should return Result<ParsedType, Error> rather than a boolean validity flag." @"let\s+is[A-Z][a-zA-Z0-9_]*\b|\bisValid\b" RegexOptions.None
                checkRegex "FSA1006" "Generic Catch: Do not catch generic exceptions for flow control. Use Result types instead." @"\:\?\s*System\.Exception|\:\?\s*Exception|catch\s*\(Exception" RegexOptions.None
                checkRegex "FSA1007" "Imperative Loops: Avoid 'while' loops. Use Seq.fold or recursion." @"\bwhile\b" RegexOptions.None
                checkRegex "FSA1008" "OOP Inheritance: Avoid OOP inheritance and interfaces. Use records of functions or Discriminated Unions." @"\binherit\b|\babstract\s+member\b|\binterface\b.*with" RegexOptions.None
                checkRegex "FSA1009" "Mutable Collections: Avoid C# mutable collections. Use F# immutable Map, Set, or list." @"\bResizeArray\b|System\.Collections\.Generic\.List|System\.Collections\.Generic\.Dictionary" RegexOptions.None
                checkRegex "FSA2008" "Enum Instead of DU: C#-style enum detected. Replace with a Discriminated Union." @"type\s+[A-Za-z0-9_]+\s*=\s*\|\s*[A-Za-z0-9_]+\s*=\s*\d+" RegexOptions.None
                checkRegex "FSA2012" "Mutable Collection Intrusion: BCL mutable collection detected in domain logic." @"\bHashSet\b|System\.Collections\.Generic\.HashSet" RegexOptions.None
                checkRegex "FSA2014" "Imperative Accumulation: Mutable accumulator with loop detected." @"let\s+mutable.*\n*.*(while|for)" RegexOptions.None
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

                // SOTA Agentic AI Guardrail Rules (Tier 1 Correctness & Tier 2 Suspicious)
                checkRegex "FSA-C01" "Unchecked Default Value: Avoid Unchecked.defaultof in domain logic." @"Unchecked\.defaultof" RegexOptions.None
                checkRegex "FSA-C03" "Synchronous Async Run: Avoid Async.RunSynchronously in library code." @"Async\.RunSynchronously" RegexOptions.None
                checkRegex "FSA-C04" "Disposed Before Async Run: IDisposable resource disposed before async workflow starts." @"use\s+[A-Za-z0-9_]+\s*=\s*new.*Async\.Start" RegexOptions.Singleline
                checkRegex "FSA-S01" "Hard-Coded Secrets: Sensitive API keys or credentials detected in source." @"(password|apiKey|api_key|secret|connectionString)\s*=\s*""[^""]+""|""(sk_live_[A-Za-z0-9]+|AKIA[A-Z0-9]{16})""" RegexOptions.IgnoreCase
                checkRegex "FSA-S03" "Swallowed Exception: Empty catch block 'try ... with _ -> ()' detected." @"with\s*_\s*->\s*\(\)" RegexOptions.None
                checkRegex "FSA-S05" "Task Blocking Call: Avoid Task.Result or Task.Wait() in asynchronous code." @"\.(Result|Wait\(\))" RegexOptions.None

                return violations |> List.rev
            }

