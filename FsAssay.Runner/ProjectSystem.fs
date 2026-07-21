namespace FsAssay.Runner

open System.IO
open Ionide.ProjInfo
open FSharp.Compiler.CodeAnalysis

module ProjectSystem =

    let loadProjects (paths: string list) =
        let toolsPath = Init.init (DirectoryInfo(Directory.GetCurrentDirectory())) None
        let loader = WorkspaceLoader.Create(toolsPath, [])
        let parsed = loader.LoadProjects paths
        
        parsed 
        |> Seq.map (fun p -> FCS.mapToFSharpProjectOptions p parsed)
        |> Seq.toList

    let loadSolution (path: string) =
        let toolsPath = Init.init (DirectoryInfo(Directory.GetCurrentDirectory())) None
        let loader = WorkspaceLoader.Create(toolsPath, [])
        let parsed = loader.LoadSln path
        
        parsed 
        |> Seq.map (fun p -> FCS.mapToFSharpProjectOptions p parsed)
        |> Seq.toList

    let getTargetProjects (path: string) =
        if path.EndsWith(".sln") then loadSolution path
        elif path.EndsWith(".fsproj") then loadProjects [path]
        else 
            let projs = Directory.GetFiles(path, "*.fsproj", SearchOption.AllDirectories)
            if projs.Length = 0 then []
            else loadProjects (projs |> Array.toList)
