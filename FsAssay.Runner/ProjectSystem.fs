namespace FsAssay.Runner

open System
open System.IO
open System.Xml.Linq
open System.Text.RegularExpressions
open Ionide.ProjInfo
open FSharp.Compiler.CodeAnalysis

module ProjectSystem =

    type ProjectLoadDisposition =
        | Loaded
        | Unsupported
        | LoadFailed

    type ProjectEvidence = {
        path: string
        disposition: ProjectLoadDisposition
        reason: string
        targetFrameworks: string list
        sourceFileCount: int
    }

    type ProjectLoadEvidence = {
        options: FSharp.Compiler.CodeAnalysis.FSharpProjectOptions list
        projects: ProjectEvidence list
    }

    let private normalizePath (basePath: string) (relativePath: string) =
        let portableRelativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar)
        let candidate =
            if Path.IsPathRooted(portableRelativePath) then portableRelativePath
            else Path.Combine(basePath, portableRelativePath)
        Path.GetFullPath(candidate)

    let private solutionProjects (path: string) =
        let directory = Path.GetDirectoryName(Path.GetFullPath(path))
        if path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) then
            try
                XDocument.Load(path).Descendants(XName.Get("Project"))
                |> Seq.choose (fun node ->
                    match node.Attribute(XName.Get("Path")) with
                    | null -> None
                    | attribute -> Some (normalizePath directory attribute.Value))
                |> Seq.filter (fun project -> project.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase))
                |> Seq.distinct
                |> Seq.sort
                |> Seq.toList
            with _ -> []
        else
            let pattern = Regex("Project\\([^)]*\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.fsproj)\"", RegexOptions.IgnoreCase)
            File.ReadAllLines(path)
            |> Seq.choose (fun line ->
                let matchResult = pattern.Match(line)
                if matchResult.Success then Some (normalizePath directory matchResult.Groups.[1].Value)
                else None)
            |> Seq.distinct
            |> Seq.sort
            |> Seq.toList

    let discoverProjectPaths (path: string) =
        if path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) then
            solutionProjects path
        elif path.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase) then
            [Path.GetFullPath(path)]
        elif Directory.Exists(path) then
            Directory.GetFiles(path, "*.fsproj", SearchOption.AllDirectories)
            |> Array.sort
            |> Array.toList
        else
            []

    let private projectMetadata (path: string) =
        try
            let document = XDocument.Load(path)
            let values (name: string) =
                document.Descendants(XName.Get(name))
                |> Seq.collect (fun element -> element.Value.Split([|';'; ','|], StringSplitOptions.RemoveEmptyEntries))
                |> Seq.map (fun value -> value.Trim())
                |> Seq.filter (String.IsNullOrWhiteSpace >> not)
                |> Seq.distinct
                |> Seq.toList
            values "TargetFrameworks" @ values "TargetFramework"
            |> List.distinct
        with _ -> []

    let private supportsM10Shape (frameworks: string list) =
        frameworks |> List.exists (fun (framework: string) -> framework.StartsWith("net10.0", StringComparison.OrdinalIgnoreCase))

    let loadProjects (paths: string list) =
        let toolsPath = None |> Init.init (Directory.GetCurrentDirectory() |> DirectoryInfo) // EXPECT: FSA2022
        let loader = WorkspaceLoader.Create(toolsPath, [])
        let parsed = loader.LoadProjects paths
        
        parsed 
        |> Seq.map (fun p -> FCS.mapToFSharpProjectOptions p parsed)
        |> Seq.toList // EXPECT: FSA-P03

    let loadSolution (path: string) =
        let toolsPath = None |> Init.init (Directory.GetCurrentDirectory() |> DirectoryInfo) // EXPECT: FSA2022
        let loader = WorkspaceLoader.Create(toolsPath, [])
        let parsed = loader.LoadSln path
        
        parsed 
        |> Seq.map (fun p -> FCS.mapToFSharpProjectOptions p parsed)
        |> Seq.toList // EXPECT: FSA-P03

    let loadWithEvidence (path: string) =
        let projectPaths = discoverProjectPaths path
        let mutable loadedOptions = []
        let projects =
            projectPaths
            |> List.map (fun projectPath ->
                let frameworks = projectMetadata projectPath
                if List.isEmpty frameworks then
                    { path = projectPath
                      disposition = LoadFailed
                      reason = "project metadata could not be read or has no target framework"
                      targetFrameworks = frameworks
                      sourceFileCount = 0 }
                elif not (supportsM10Shape frameworks) then
                    { path = projectPath
                      disposition = Unsupported
                      reason = "target framework is outside the qualified net10.0 project shape"
                      targetFrameworks = frameworks
                      sourceFileCount = 0 }
                else
                    try
                        let options = loadProjects [projectPath]
                        match options |> List.tryFind (fun option -> option.SourceFiles.Length > 0) with
                        | Some option ->
                            loadedOptions <- option :: loadedOptions
                            { path = projectPath
                              disposition = Loaded
                              reason = "loaded by Ionide/FCS with source files"
                              targetFrameworks = frameworks
                              sourceFileCount = option.SourceFiles.Length }
                        | None ->
                            { path = projectPath
                              disposition = LoadFailed
                              reason = "project loader returned no F# source files"
                              targetFrameworks = frameworks
                              sourceFileCount = 0 }
                    with error ->
                        { path = projectPath
                          disposition = LoadFailed
                          reason = "project loader exception: " + error.Message
                          targetFrameworks = frameworks
                          sourceFileCount = 0 })
        { options = loadedOptions |> List.rev
          projects = projects }

    let getTargetProjects (path: string) =
        match path with
        | _ when path.EndsWith(".sln") || path.EndsWith(".slnx") -> loadSolution path
        | _ when path.EndsWith(".fsproj") -> loadProjects [path]
        | _ when File.Exists(path) -> [] // EXPECT: FSA2022
        | _ -> 
            let projs = Directory.GetFiles(path, "*.fsproj", SearchOption.AllDirectories) // EXPECT: FSA2022
            if projs.Length = 0 then []
            else projs |> Array.toList |> loadProjects
