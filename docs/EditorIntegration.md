# Editor Integration Guide

FSharpAssay uses `FSharp.Analyzers.SDK` to execute its elite functional-only rules against the F# Typed Syntax Tree (TAST). Since this SDK is an ecosystem standard, you can integrate FSharpAssay directly into your IDE to get live, real-time squiggles for rules like `FSA1001` (Mutation Overuse) and `FSA1003` (Null Reference).

## Ionide (VS Code)

Ionide natively supports `FSharp.Analyzers.SDK`.

1. **Build the Analyzer**: First, build `FsAssay.Analyzers`.
   ```bash
   dotnet build FsAssay.Analyzers -c Release
   ```
2. **Configure Settings**: Open or create `.vscode/settings.json` in your repository and point the analyzer path to the built DLL's directory.
   ```json
   {
       "FSharp.enableAnalyzers": true,
       "FSharp.analyzersPath": [
           "./FsAssay.Analyzers/bin/Release/net10.0"
       ]
   }
   ```
3. **Restart Ionide**: Reload your VS Code window. The elite F# anti-patterns will now highlight natively as errors in your editor.

## JetBrains Rider

Rider also ships with built-in support for `FSharp.Analyzers.SDK`.

1. **Enable Analyzers**: 
   Navigate to `Preferences | Editor | Inspection Settings | F# | Analyzers`.
2. **Enable SDK Analyzers**: Ensure "Enable F# Analyzers" is checked.
3. **Add Path**: Add the path to the compiled `FsAssay.Analyzers.dll` directory in the analyzer path list.
4. **Restart**: Restart Rider. Violations will surface natively within the inspection panel and as inline squiggles.

## Suppressions

Both IDEs respect the `FsAssay` suppression mechanisms built into the TAST slice:
- `[<System.Diagnostics.CodeAnalysis.SuppressMessage("FsAssay", "FSA1001")>]`
- `[<Profile("interop")>]` (Automatically suppresses `FSA1001` and `FSA1003` in the applied scope).
