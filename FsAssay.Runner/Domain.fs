namespace FsAssay.Runner

open FSharp.Analyzers.SDK

type SkipReason =
    | NoTast
    | CompilerErrors
    | UnrelatedFile

type RuleFailure =
    | AnalyzerException of string

type RuleEvaluation =
    | Completed of Message list
    | Skipped of SkipReason
    | Failed of RuleFailure

type AssayVerdict =
    | Pass
    | Fail
    | Inconclusive
    | ToolFailure

module ExitCodes =
    let Success = 0
    let BlockingFinding = 1
    let RequiredEvidenceMissing = 2
    let ToolFailure = 3
    let InvalidInvocation = 64
