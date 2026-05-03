# release-package.ps1
# Script to build and package EtabSharp for NuGet release

param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "",

    [Parameter(Mandatory=$false)]
    [switch]$SkipTests = $false,

    [Parameter(Mandatory=$false)]
    [switch]$Push = $false,

    [Parameter(Mandatory=$false)]
    [string]$ApiKey = "",

    # Install the packed .nupkg into a local NuGet source folder so sibling
    # projects (e.g. EtabExtension.CLI) can consume it without publishing.
    [Parameter(Mandatory=$false)]
    [string]$LocalSource = "",

    # Clear the NuGet HTTP and package cache before restoring.
    # Always true when -LocalSource is used to prevent stale .targets files.
    [Parameter(Mandatory=$false)]
    [switch]$ClearCache = $false
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "EtabSharp NuGet Package Release Script" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to solution root
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

if (-not (Test-Path "EtabSharp.sln")) {
    Write-Host "Error: EtabSharp.sln not found. Run this script from the solution root." -ForegroundColor Red
    exit 1
}

# ── Resolve version ────────────────────────────────────────────────────────────
if ([string]::IsNullOrEmpty($Version)) {
    $csprojPath = "src\EtabSharp\EtabSharp.csproj"
    $csprojContent = Get-Content $csprojPath -Raw
    if ($csprojContent -match '<Version>(.*?)</Version>') {
        $Version = $matches[1]
        Write-Host "Using version from .csproj: $Version" -ForegroundColor Yellow
    } else {
        Write-Host "Error: Could not find version in .csproj and no -Version supplied." -ForegroundColor Red
        exit 1
    }
}

Write-Host "Building EtabSharp v$Version..." -ForegroundColor Green
Write-Host ""

# ── Step 1: Clean ──────────────────────────────────────────────────────────────
Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release
if ($LASTEXITCODE -ne 0) { Write-Host "Clean failed!" -ForegroundColor Red; exit 1 }
Write-Host "✓ Clean completed" -ForegroundColor Green
Write-Host ""

# ── Step 2: Clear NuGet cache (always when using local source) ────────────────
if ($ClearCache -or -not [string]::IsNullOrEmpty($LocalSource)) {
    Write-Host "Step 2: Clearing NuGet caches..." -ForegroundColor Yellow
    # Clear the global packages cache so the old .targets file is not reused
    $nugetCache = "$env:USERPROFILE\.nuget\packages\etabsharp"
    if (Test-Path $nugetCache) {
        Remove-Item $nugetCache -Recurse -Force
        Write-Host "  ✓ Removed cached EtabSharp packages from: $nugetCache" -ForegroundColor Green
    } else {
        Write-Host "  ℹ No cached EtabSharp packages found." -ForegroundColor Gray
    }
    Write-Host "✓ Cache cleared" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Step 2: Skipping cache clear (pass -ClearCache to force)" -ForegroundColor Gray
    Write-Host ""
}

# ── Step 3: Restore ───────────────────────────────────────────────────────────
Write-Host "Step 3: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) { Write-Host "Restore failed!" -ForegroundColor Red; exit 1 }
Write-Host "✓ Restore completed" -ForegroundColor Green
Write-Host ""

# ── Step 4: Tests ─────────────────────────────────────────────────────────────
if (-not $SkipTests) {
    Write-Host "Step 4: Running tests..." -ForegroundColor Yellow
    dotnet test --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed! Use -SkipTests to skip." -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ All tests passed" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Step 4: Skipping tests (as requested)" -ForegroundColor Yellow
    Write-Host ""
}

# ── Step 5: Build ─────────────────────────────────────────────────────────────
Write-Host "Step 5: Building in Release mode..." -ForegroundColor Yellow
dotnet build src\EtabSharp\EtabSharp.csproj --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed!" -ForegroundColor Red; exit 1 }
Write-Host "✓ Build completed" -ForegroundColor Green
Write-Host ""

# ── Step 6: Pack ──────────────────────────────────────────────────────────────
Write-Host "Step 6: Creating NuGet package..." -ForegroundColor Yellow
$outputDir = ".\artifacts"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

dotnet pack src\EtabSharp\EtabSharp.csproj `
    --configuration Release `
    --no-build `
    --output $outputDir `
    /p:PackageVersion=$Version

if ($LASTEXITCODE -ne 0) { Write-Host "Pack failed!" -ForegroundColor Red; exit 1 }
Write-Host "✓ Package created" -ForegroundColor Green
Write-Host ""

# Locate the produced package
$packageFile = Get-ChildItem "$outputDir\EtabSharp.$Version.nupkg" -ErrorAction SilentlyContinue
if (-not $packageFile) {
    Write-Host "Error: Package not found at $outputDir\EtabSharp.$Version.nupkg" -ForegroundColor Red
    exit 1
}

$packageSize = [math]::Round($packageFile.Length / 1MB, 2)

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "✓ Package Created Successfully!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package Details:" -ForegroundColor Cyan
Write-Host "  Version:   $Version" -ForegroundColor White
Write-Host "  Location:  $($packageFile.FullName)" -ForegroundColor White
Write-Host "  Size:      $packageSize MB" -ForegroundColor White
Write-Host "  Frameworks: .NET 8.0, .NET 10.0" -ForegroundColor White
Write-Host ""

# ── Step 7: Verify ────────────────────────────────────────────────────────────
Write-Host "Step 7: Verifying package contents..." -ForegroundColor Yellow
dotnet nuget verify $packageFile.FullName
Write-Host ""

# ── Step 8: Copy to local NuGet source (optional) ────────────────────────────
if (-not [string]::IsNullOrEmpty($LocalSource)) {
    Write-Host "Step 8: Installing into local NuGet source..." -ForegroundColor Yellow

    if (-not (Test-Path $LocalSource)) {
        New-Item -ItemType Directory -Path $LocalSource -Force | Out-Null
        Write-Host "  Created local source folder: $LocalSource" -ForegroundColor Gray
    }

    Copy-Item $packageFile.FullName $LocalSource -Force
    Write-Host "  ✓ Copied to: $LocalSource" -ForegroundColor Green

    # Register the source if not already present
    $existingSources = dotnet nuget list source 2>&1
    if ($existingSources -notmatch [regex]::Escape($LocalSource)) {
        dotnet nuget add source $LocalSource --name "EtabSharpLocal"
        Write-Host "  ✓ Registered as NuGet source 'EtabSharpLocal'" -ForegroundColor Green
    } else {
        Write-Host "  ℹ NuGet source already registered." -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "  Next: rebuild sidecar to pick up the new package:" -ForegroundColor Yellow
    Write-Host "    cd ..\EtabExtension.CLI" -ForegroundColor Cyan
    Write-Host "    .\build-sidecar.ps1" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "Step 8: Skipping local install (pass -LocalSource <path> to install locally)" -ForegroundColor Gray
    Write-Host ""
}

# ── Step 9: Push to NuGet.org (optional) ─────────────────────────────────────
if ($Push) {
    if ([string]::IsNullOrEmpty($ApiKey)) {
        Write-Host "Error: -Push specified but no -ApiKey provided." -ForegroundColor Red
        Write-Host "Usage: .\release-package.ps1 -Push -ApiKey YOUR_API_KEY" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Step 9: Publishing to NuGet.org..." -ForegroundColor Yellow
    Write-Host "Package: $($packageFile.Name)" -ForegroundColor White

    $confirm = Read-Host "Are you sure you want to publish to NuGet.org? (yes/no)"
    if ($confirm -eq "yes") {
        dotnet nuget push $packageFile.FullName --api-key $ApiKey --source https://api.nuget.org/v3/index.json
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "✓ Package published successfully!" -ForegroundColor Green
            Write-Host "It may take a few minutes to appear on NuGet.org." -ForegroundColor Yellow
        } else {
            Write-Host "✗ Publish failed!" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "Publish cancelled." -ForegroundColor Yellow
    }
} else {
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host "Next Steps" -ForegroundColor Yellow
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Test locally (recommended before publishing):" -ForegroundColor White
    Write-Host "  .\release-package.ps1 -LocalSource C:\LocalNuGet -ClearCache" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Then update EtabExtension.CLI to reference 0.3.5-beta and rebuild:" -ForegroundColor White
    Write-Host "  cd ..\EtabExtension.CLI" -ForegroundColor Cyan
    Write-Host "  .\build-sidecar.ps1" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Publish to NuGet.org when ready:" -ForegroundColor White
    Write-Host "  .\release-package.ps1 -Push -ApiKey YOUR_API_KEY" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""