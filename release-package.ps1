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
    [string]$ApiKey = ""
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

# Check if we're in the right directory
if (-not (Test-Path "EtabSharp.sln")) {
    Write-Host "Error: EtabSharp.sln not found. Please run this script from the solution root." -ForegroundColor Red
    exit 1
}

# Get current version from .csproj if not specified
if ([string]::IsNullOrEmpty($Version)) {
    $csprojPath = "src\EtabSharp\EtabSharp.csproj"
    $csprojContent = Get-Content $csprojPath -Raw
    if ($csprojContent -match '<Version>(.*?)</Version>') {
        $Version = $matches[1]
        Write-Host "Using version from .csproj: $Version" -ForegroundColor Yellow
    } else {
        Write-Host "Error: Could not find version in .csproj and no version specified" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Building EtabSharp v$Version..." -ForegroundColor Green
Write-Host ""

# Step 1: Clean
Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Clean failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Clean completed" -ForegroundColor Green
Write-Host ""

# Step 2: Restore
Write-Host "Step 2: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Restore completed" -ForegroundColor Green
Write-Host ""

# Step 3: Run tests (optional)
if (-not $SkipTests) {
    Write-Host "Step 3: Running tests..." -ForegroundColor Yellow
    dotnet test --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed! Use -SkipTests to skip testing." -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ All tests passed" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Step 3: Skipping tests (as requested)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 4: Build
Write-Host "Step 4: Building in Release mode..." -ForegroundColor Yellow
dotnet build src\EtabSharp\EtabSharp.csproj --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build completed" -ForegroundColor Green
Write-Host ""

# Step 5: Pack
Write-Host "Step 5: Creating NuGet package..." -ForegroundColor Yellow
$outputDir = ".\artifacts"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

dotnet pack src\EtabSharp\EtabSharp.csproj `
    --configuration Release `
    --no-build `
    --output $outputDir `
    /p:PackageVersion=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Package created" -ForegroundColor Green
Write-Host ""

# Find the created package
$packageFile = Get-ChildItem "$outputDir\EtabSharp.$Version.nupkg" -ErrorAction SilentlyContinue
if (-not $packageFile) {
    Write-Host "Error: Package file not found at $outputDir\EtabSharp.$Version.nupkg" -ForegroundColor Red
    exit 1
}

$packageSize = [math]::Round($packageFile.Length / 1MB, 2)

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "✓ Package Created Successfully!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package Details:" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor White
Write-Host "  Location: $($packageFile.FullName)" -ForegroundColor White
Write-Host "  Size: $packageSize MB" -ForegroundColor White
Write-Host ""

# Step 6: Verify package contents
Write-Host "Step 6: Verifying package contents..." -ForegroundColor Yellow
dotnet nuget verify $packageFile.FullName
Write-Host ""

# Step 7: Push to NuGet (optional)
if ($Push) {
    if ([string]::IsNullOrEmpty($ApiKey)) {
        Write-Host "Error: -Push specified but no -ApiKey provided" -ForegroundColor Red
        Write-Host "Usage: .\release-package.ps1 -Push -ApiKey YOUR_API_KEY" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "Step 7: Publishing to NuGet.org..." -ForegroundColor Yellow
    Write-Host "Package: $($packageFile.Name)" -ForegroundColor White
    
    $confirm = Read-Host "Are you sure you want to publish to NuGet.org? (yes/no)"
    if ($confirm -eq "yes") {
        dotnet nuget push $packageFile.FullName --api-key $ApiKey --source https://api.nuget.org/v3/index.json
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "✓ Package published successfully!" -ForegroundColor Green
            Write-Host "It may take a few minutes to appear on NuGet.org" -ForegroundColor Yellow
        } else {
            Write-Host ""
            Write-Host "✗ Publish failed!" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "Publish cancelled by user" -ForegroundColor Yellow
    }
} else {
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host "1. Verify package contents:" -ForegroundColor White
    Write-Host "   dotnet nuget verify `"$($packageFile.FullName)`"" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "2. Test package locally:" -ForegroundColor White
    Write-Host "   dotnet nuget add source $outputDir --name LocalPackages" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "3. Publish to NuGet.org:" -ForegroundColor White
    Write-Host "   .\release-package.ps1 -Version $Version -Push -ApiKey YOUR_API_KEY" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or manually:" -ForegroundColor White
    Write-Host "   dotnet nuget push `"$($packageFile.FullName)`" --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""