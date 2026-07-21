# FsAssay Milestones

## Milestone 1: Foundation (v0.1) - **[COMPLETED]**
- [x] Establish the project architecture.
- [x] Pin the F# compiler services (`FSharp.Compiler.Service`) and the `FSharp.Analyzers.SDK` to deterministic versions (v0.37.2).
- [x] Create the initial TDD test suite using `Expecto`.

## Milestone 2: Elite F# Rule Engine - **[COMPLETED]**
- [x] Distill the "Elite F#" concepts from Domain-Driven Design (Make illegal states unrepresentable, Parse don't validate).
- [x] Implement the `FSA_All` analyzer to catch C#-ish anti-patterns.
  - [x] **FSA1001**: Mutation Overuse (`mutable`)
  - [x] **FSA1002**: Partial Access (`Option.get`, `.Value`)
  - [x] **FSA1003**: Null Reference
  - [x] **FSA1004**: Primitive Obsession (Type aliases for primitives)
  - [x] **FSA1005**: Boolean Validation (`isValid` -> `bool`)
  - [x] **FSA1006**: Generic Catch (`:? Exception`)
  - [x] **FSA1007**: Imperative Loops (`while`)
- [x] Verify 100% test coverage using the TDD pipeline.

## Milestone 3: Real-World "In-the-Wild" Validation - **[NEXT]**
- [ ] Select a target open-source F# repository from GitHub.
- [ ] Run `fs-assay` against the codebase to discover hidden C#-isms.
- [ ] Document real-world false positives and refine the analyzer.
- [ ] Upgrade the analyzer from source text scanning to Untyped AST (`ParseTree`) traversal for maximum robustness against inline functions.

## Milestone 4: Distribution & IDE Integration
- [ ] Package `FsAssay.Analyzers` as a publishable NuGet package.
- [ ] Configure GitHub Actions for continuous integration (CI/CD).
- [ ] Document integration steps for Ionide (VS Code) and Rider to provide live squiggly lines in the developer's editor.
