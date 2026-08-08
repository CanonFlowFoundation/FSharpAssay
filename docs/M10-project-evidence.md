# M10 project-loading evidence

FsAssay records project loading as evidence rather than treating discovery as
proof of analysis. Legacy `.sln` and modern `.slnx` targets are discovered,
then each F# project is classified as `loaded`, `unsupported`, or `load-failed`.

The current qualified project shape is an SDK-style F# project with a
`net10.0` target. Multi-target projects are eligible when they include that
target; projects without it remain explicitly `unsupported`. A loader or
metadata error is `load-failed` and is never converted into a directory scan or
a successful receipt.

JSON output uses schema `1.0.0` and includes project counts, per-project reasons,
source-file counts, compiler-incomplete files, findings, `outcome`,
`policyAvailable`, and `authoritative`. A policy is not inferred from the
consumer repository, so policyless runs are observations with
`authoritative: false`. Any unsupported project, load failure, skipped file, or
compiler-incomplete file keeps the outcome non-authoritative (`Inconclusive`, or
`ToolFailure` for a loader failure).

The M10 fixture covers a legacy `.sln` containing one loaded `net10.0` project
and one unsupported project, plus a malformed project proving that load failure
is explicit evidence and never silent fallback success.
