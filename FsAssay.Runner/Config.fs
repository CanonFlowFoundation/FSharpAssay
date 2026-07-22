namespace FsAssay.Runner

open System.IO
open System.Text.Json

module Config =
    type PolicyConfig = {
        severities: Map<string, string>
        exclude: string[]
        targetGrade: string
        profile: string
    }

    let defaultConfig = {
        severities = Map.empty
        exclude = [| "**/obj/**"; "**/bin/**"; "**/AssemblyAttributes.fs" |]
        targetGrade = "A"
        profile = "core"
    }

    let loadConfig (targetPath: string) =
        let dirPath = if Directory.Exists(targetPath) then targetPath else Path.GetDirectoryName(targetPath)
        let configPath = Path.Combine(dirPath, ".fsassayrc")
        if File.Exists(configPath) then
            try
                let json = File.ReadAllText(configPath)
                let opts = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
                let loaded = JsonSerializer.Deserialize<PolicyConfig>(json, opts)
                Option.ofObj loaded |> Option.defaultValue defaultConfig
            with _ -> defaultConfig
        else defaultConfig

