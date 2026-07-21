namespace FsAssay.Runner

open System
open System.IO
open System.Text.Json

module Config =
    type PolicyConfig = {
        severities: System.Collections.Generic.Dictionary<string, string>
        exclude: string[]
        targetGrade: string
    }

    let defaultConfig = {
        severities = System.Collections.Generic.Dictionary<string, string>()
        exclude = [| "**/obj/**"; "**/bin/**"; "**/AssemblyAttributes.fs" |]
        targetGrade = "A"
    }

    let loadConfig (path: string) =
        let configPath = if Directory.Exists(path) then Path.Combine(path, ".fsassayrc") else Path.Combine(Path.GetDirectoryName(path), ".fsassayrc")
        if File.Exists(configPath) then
            try
                let json = File.ReadAllText(configPath)
                let opts = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
                let loaded = JsonSerializer.Deserialize<PolicyConfig>(json, opts)
                if Object.ReferenceEquals(loaded, null) then defaultConfig else loaded
            with _ -> defaultConfig
        else defaultConfig
