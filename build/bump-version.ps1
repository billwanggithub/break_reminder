# Bumps the <Version> patch number in BreakReminder.csproj.
# e.g. 0.1.3 -> 0.1.4. Writes the result back and prints the new version.

$ErrorActionPreference = 'Stop'

$csproj = Join-Path $PSScriptRoot '..\BreakReminder.csproj' | Resolve-Path
$content = Get-Content $csproj -Raw

if ($content -notmatch '<Version>(\d+)\.(\d+)\.(\d+)</Version>') {
    throw "No <Version>X.Y.Z</Version> tag found in $csproj"
}

$major = [int]$Matches[1]
$minor = [int]$Matches[2]
$patch = [int]$Matches[3] + 1
$newVersion = "$major.$minor.$patch"

$updated = [regex]::Replace($content, '<Version>\d+\.\d+\.\d+</Version>', "<Version>$newVersion</Version>", 1)
Set-Content -Path $csproj -Value $updated -NoNewline

Write-Host "Bumped version to $newVersion"
