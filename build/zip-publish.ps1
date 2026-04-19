# Zips the contents of the publish folder into BreakReminder_{version}.zip.
# Reads <Version> from BreakReminder.csproj. Excludes existing .zip/.7z archives.

$ErrorActionPreference = 'Stop'

$csproj = Join-Path $PSScriptRoot '..\BreakReminder.csproj' | Resolve-Path
$content = Get-Content $csproj -Raw

if ($content -notmatch '<Version>(\d+\.\d+\.\d+)</Version>') {
    throw "No <Version>X.Y.Z</Version> tag found in $csproj"
}
$version = $Matches[1]

$publishDir = Join-Path $PSScriptRoot '..\publish' | Resolve-Path
$zipPath = Join-Path $publishDir "BreakReminder_$version.zip"

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

$files = Get-ChildItem $publishDir -File | Where-Object { $_.Extension -notin '.zip', '.7z' }
if ($files.Count -eq 0) { throw "No files to zip in $publishDir" }

Compress-Archive -Path $files.FullName -DestinationPath $zipPath -CompressionLevel Optimal

$size = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "Zipped to $zipPath ($size MB)"
