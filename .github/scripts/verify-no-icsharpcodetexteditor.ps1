#!/usr/bin/env pwsh
# Verify that no references to ICSharpCode.TextEditor remain inside LiteDB.Studio.Wpf
param(
    [string]$SearchRoot = "LiteDB.Studio.Wpf"
)
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $SearchRoot)) {
    Write-Output "Directory not found: $SearchRoot"; exit 0
}

$pattern = 'ICSharpCode.TextEditor'
$matches = Select-String -Path (Join-Path $SearchRoot "**/*.*") -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue
if ($matches) {
    Write-Host "Found references to $pattern:" -ForegroundColor Red
    foreach ($m in $matches) {
        Write-Host "  $($m.Path):$($m.LineNumber): $($m.Line.Trim())"
    }
    Write-Host "Verification FAILED: remove all references to $pattern in $SearchRoot" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "Verification passed: no references to $pattern found in $SearchRoot" -ForegroundColor Green
    exit 0
}