param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "artifacts\publish\$Runtime"
$installerProject = Join-Path $root "Installer\PowerConsumptionInstaller.wixproj"
$appProject = Join-Path $root "TimestampCalculator.csproj"

Write-Host "Publishing application to $publishDir"
dotnet publish $appProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Building MSI installer"
dotnet build $installerProject `
    -c $Configuration `
    -p:PublishDir=$publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build for the MSI installer failed with exit code $LASTEXITCODE."
}

Write-Host "MSI build completed."
