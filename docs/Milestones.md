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
10. [ ] Editor integration: only after CLI verification is trustworthy.

## Phase 1: Real-World "In-the-Wild" Validation - **[NOT STARTED]**
- [ ] Run `fs-assay` against a large codebase corpus to discover hidden C#-isms using proper TAST symbols.
- [ ] Document real-world false positives and refine the analyzer.

## Phase 2: Distribution & IDE Integration - **[NOT STARTED]**
- [ ] Package `FsAssay.Analyzers` as a publishable NuGet package.
- [ ] Configure GitHub Actions for continuous integration (CI/CD).
- [ ] Document integration steps for Ionide (VS Code) and Rider to provide live squiggly lines in the developer's editor.
