# EtabSharp overview

Purpose: strongly typed .NET wrapper around the CSI ETABS API (ETABS v22+), used by sidecar automation projects such as `D:/Work/EtabExtension.CLI`.

Tech stack: C#/.NET, multi-targeted `net8.0;net10.0`, ETABS COM API via `ETABSv1.dll`, Microsoft.Extensions.Logging, NuGet package with build targets for ETABS DLL auto-detection. Includes an MCP server project and xUnit tests/visual tests.

Structure:
- `src/EtabSharp/Core`: `ETABSWrapper`, `ETABSApplication`, `ETABSModel` entry points and lifecycle wrappers.
- `src/EtabSharp/DatabaseTables`: database table display/edit wrappers and table result models.
- `src/EtabSharp/Loads`: load cases, load combinations, load patterns, including name/type helpers.
- `src/EtabSharp/AnalysisResults*`, `Analyzes`, `Properties`, `Elements`, `Groups`, `Design`: typed managers/models over ETABS API domains.
- `test/EtabSharp.Test`: xUnit tests, many live ETABS tests skip unless ETABS is running/installed.
- `test/EtabsSharp.VisualTest`: manual visual test harness.
- `mcp/EtabSharp.Mcp`: MCP tooling over the wrapper.

Important integration surfaces for EtabExtension:
- `ETABSWrapper.CreateNew()` creates hidden/visible Mode B ETABS sessions.
- `ETABSWrapper.Connect()` / `ConnectToProcess()` attach to running Mode A sessions.
- `model.LoadCases.GetNameList()`, `GetAllLoadCases()`, and `GetTypeOAPI()` provide load case metadata.
- `model.LoadCombinations.GetNameList()`, `GetAllCombinations()`, and `GetCaseList()` provide combo metadata.
- `model.LoadPatterns.GetNameList()`, `model.Groups.GetNameList()`, materials/sections/story helpers can support model metadata snapshots.
- `model.DatabaseTables.SetLoadCasesSelectedForDisplay`, `SetLoadCombinationsSelectedForDisplay`, and `GetTableForDisplayArray` drive table extraction.