# FsAssay Milestones

## Phase 0: Prototype and Orchestration - **[IN PROGRESS]**
- [x] Regex prototype completed.
- [x] Verification engine: **NOT STARTED**
- [ ] Rule catalogue: unstable
- [ ] Release gate: unavailable

### Required PR Order:
1. [x] Truth reset: markdown files declare experimental status; downgrade constraints.
2. [x] Toolchain lock: clean zero-warning build, locked restore and CI.
3. [x] Verdict kernel: pure outcomes, normalized findings and exit codes.
4. [x] Project loading: real `.fsproj`/solution loading with compiler diagnostics.
5. [x] FSA1002 TAST slice: exact symbol identity and ranges.
6. [x] FSA1001/1003 TAST slice: mutation and null expression interception.
7. [x] Evidence: canonical JSON, SARIF and toolchain record.
8. [x] Profiles and suppression: core, shell, interop; visible authorization.
9. [x] Corpus adjudication: precision/recall per rule against manually labelled specimens.
10. [x] Editor integration: only after CLI verification is trustworthy.

## Phase 1: Real-World "In-the-Wild" Validation
- [x] Scan standard F# repositories (`CanonFlow` scanned - 776 violations found).
- [x] Document the delta between standard F# code and our extreme elite baseline.

## Phase 2: Open-Source Delivery
- [x] Release as standalone tool (`dotnet tool install -g fsassay`).
- [x] Implement `Argu` for robust command-line invocation (`fsassay --target ./src --out result.json`).
- [x] Integrate standard `.editorconfig`.
- [x] Document integration steps for Ionide (VS Code) and Rider to provide live squiggly lines in the developer's editor.
