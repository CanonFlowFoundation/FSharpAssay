module M10ProjectLoadingTests

open Expecto
open System
open System.IO
open System.Text.Json
open FsAssay.Runner

let private withTempDirectory action =
    let directory = Path.Combine(Path.GetTempPath(), "fsassay-m10-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(directory) |> ignore
    try action directory
    finally
        if Directory.Exists(directory) then Directory.Delete(directory, true)

let private writeProject directory name framework =
    let projectDirectory = Path.Combine(directory, name)
    Directory.CreateDirectory(projectDirectory) |> ignore
    File.WriteAllText(
        Path.Combine(projectDirectory, name + ".fsproj"),
        sprintf """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>%s</TargetFramework></PropertyGroup><ItemGroup><Compile Include="Library.fs" /></ItemGroup></Project>""" framework)
    File.WriteAllText(Path.Combine(projectDirectory, "Library.fs"), "module " + name + "\nlet value = 1\n")
    Path.Combine(projectDirectory, name + ".fsproj")

let tests =
    testList "M10 project-loading evidence" [
        testCase "legacy sln discovers F# projects and distinguishes supported shape" <| fun _ ->
            withTempDirectory (fun directory ->
                let loaded = writeProject directory "Loaded" "net10.0"
                let unsupported = writeProject directory "Unsupported" "netstandard2.0"
                let solution = Path.Combine(directory, "Fixture.sln")
                let solutionText = sprintf """Microsoft Visual Studio Solution File, Format Version 12.00
Project("{00000000-0000-0000-0000-000000000000}") = "Loaded", "Loaded/Loaded.fsproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
Project("{00000000-0000-0000-0000-000000000000}") = "Unsupported", "Unsupported/Unsupported.fsproj", "{22222222-2222-2222-2222-222222222222}"
EndProject
"""
                File.WriteAllText(solution, solutionText)
                let evidence = ProjectSystem.loadWithEvidence solution
                Expect.equal (ProjectSystem.discoverProjectPaths solution |> List.length) 2 "legacy .sln must discover both F# projects"
                Expect.equal evidence.projects.Length 2 "evidence must account for every discovered project"
                Expect.equal (evidence.projects |> List.filter (fun p -> p.disposition = ProjectSystem.Loaded) |> List.length) 1 "one net10.0 project should load"
                Expect.equal (evidence.projects |> List.filter (fun p -> p.disposition = ProjectSystem.Unsupported) |> List.length) 1 "netstandard-only project must be explicit unsupported evidence"
                Expect.isGreaterThan evidence.options.Length 0 "a supported fixture must produce real project options"
                Expect.isTrue (evidence.projects |> List.exists (fun p -> p.path = loaded && p.sourceFileCount > 0)) "loaded project must carry source evidence"
                Expect.isTrue (evidence.projects |> List.exists (fun p -> p.path = unsupported && p.reason.Contains("target framework"))) "unsupported reason must be actionable"
                let receiptPath = Path.Combine(directory, "receipt.json")
                Output.writeEvidenceJson evidence [] [] "Inconclusive" false false receiptPath
                use receipt = JsonDocument.Parse(File.ReadAllText(receiptPath))
                Expect.equal (receipt.RootElement.GetProperty("projectsDiscovered").GetInt32()) 2 "receipt must preserve discovered count"
                Expect.equal (receipt.RootElement.GetProperty("projectsLoaded").GetInt32()) 1 "receipt must preserve loaded count"
                Expect.equal (receipt.RootElement.GetProperty("projectsUnsupported").GetInt32()) 1 "receipt must preserve unsupported count"
                Expect.isFalse (receipt.RootElement.GetProperty("authoritative").GetBoolean()) "incomplete evidence cannot be authoritative")

        testCase "project loading failure is evidence, never fallback success" <| fun _ ->
            withTempDirectory (fun directory ->
                let malformed = Path.Combine(directory, "Broken.fsproj")
                File.WriteAllText(malformed, "<Project>")
                let evidence = ProjectSystem.loadWithEvidence malformed
                Expect.equal evidence.options.Length 0 "malformed project must not produce fallback options"
                Expect.equal evidence.projects.Length 1 "malformed project must remain in the receipt"
                match evidence.projects.Head.disposition with
                | ProjectSystem.LoadFailed -> Expect.stringContains evidence.projects.Head.reason "metadata" "load failure reason must be explicit"
                | other -> failtestf "expected LoadFailed, got %A" other)
    ]
