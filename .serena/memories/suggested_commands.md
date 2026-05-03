# Suggested commands

From `D:/Work/EtabSharp`:
- `dotnet build EtabSharp.sln`
- `dotnet build src/EtabSharp/EtabSharp.csproj`
- `dotnet test test/EtabSharp.Test/EtabSharp.Test.csproj`
- `dotnet run --project test/EtabsSharp.VisualTest/EtabsSharp.VisualTest.csproj`
- `./release-package.ps1` for release packaging.

Windows/PowerShell utilities:
- `git status --short --branch`
- `git ls-files`
- `Get-ChildItem -Recurse -File -Include *.cs`
- `Select-String -Path (...) -Pattern '...'` for text search when ripgrep is unavailable.