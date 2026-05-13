param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$Version = "0.1.0-beta"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "DesktopPortal\DesktopPortal.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$packageName = "DesktopPortal-$Version-$Runtime"
$publishDir = Join-Path $artifactsRoot $packageName
$zipPath = Join-Path $artifactsRoot "$packageName.zip"

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:Version=$Version `
    -p:FileVersion=0.1.0.0 `
    -p:InformationalVersion=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host "Published: $publishDir"
Write-Host "Package:   $zipPath"
