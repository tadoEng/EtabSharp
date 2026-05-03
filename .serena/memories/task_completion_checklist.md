# Completion checklist

- Run `dotnet build EtabSharp.sln` or at least `dotnet build src/EtabSharp/EtabSharp.csproj` after library changes.
- Run focused tests with `dotnet test test/EtabSharp.Test/EtabSharp.Test.csproj` when practical.
- Live ETABS behavior requires ETABS installed/running; note clearly when tests were skipped or not run.
- Preserve NuGet package contract and ETABSv1.dll auto-detection behavior when changing project files.
- Avoid shipping ETABSv1.dll in the package; package targets should continue to locate local ETABS installations.