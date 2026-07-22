# FsAssay Milestones & Architecture Roadmap

## Phase 0: Prototype and Orchestration - **[COMPLETED]**
- [x] Regex prototype completed.
- [x] Hybrid TAST/AST verification engine: **COMPLETED**
- [x] Class-A defect remediation & line range calculations: **COMPLETED**
- [x] Rate Card Scorecard & Material Design 5 HTML Dashboard: **COMPLETED**

### Required PR Order:
1. [x] Truth reset: markdown files declare experimental status; downgrade constraints.
2. [x] Toolchain lock: clean zero-warning build, locked restore and Central Package Management (CPM).
3. [x] Verdict kernel: pure outcomes, normalized findings and exit codes (`ExitCodes.RequiredEvidenceMissing`).
4. [x] Project loading: real `.fsproj`/`.sln`/`.slnx` solution loading with compiler diagnostics.
5. [x] FSA1002 TAST slice: exact symbol identity and ranges.
6. [x] FSA1001/1003 TAST slice: mutation and null expression interception with source range filtering.
7. [x] Evidence: canonical JSON, SARIF, Rate Card Markdown (`-r`) and Material 5 HTML Dashboard (`-m`).
8. [x] Profiles and suppression: core, shell, interop; visible authorization.
9. [x] Corpus adjudication: precision/recall per rule against manually labelled specimens.
10. [x] Editor integration: IDE integration registered via CLI analyzer orchestration.

## Phase 1: Real-World "In-the-Wild" Validation
- [x] Scan standard F# repositories (`CanonFlow`, `GSTFlow` scanned).
- [x] Document the delta between standard F# code and our extreme elite baseline.

## Phase 2: Open-Source Delivery
- [x] Implement `Argu` for command-line invocation (`fsassay -r ratecard.md -m dashboard.html /target`).
- [x] Material Design 5 vivid HTML5 dashboard reporting with interactive expandable evidence cards.
- [x] Document integration steps for Ionide (VS Code) and Rider.
