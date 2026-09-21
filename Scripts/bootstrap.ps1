[CmdletBinding()]
param(
    [string]$NodePath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$sheetToolDirectory = Join-Path $repositoryRoot 'Config/SheetTool'

if ([string]::IsNullOrWhiteSpace($NodePath)) {
    $NodePath = $env:FEATHER_NODE_PATH
}
if ([string]::IsNullOrWhiteSpace($NodePath)) {
    $nodeCommand = Get-Command node -ErrorAction SilentlyContinue
    if ($nodeCommand) {
        $NodePath = $nodeCommand.Source
    }
}
if ([string]::IsNullOrWhiteSpace($NodePath)) {
    $bundledNode = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
    if (Test-Path -LiteralPath $bundledNode -PathType Leaf) {
        $NodePath = $bundledNode
    }
}
if ([string]::IsNullOrWhiteSpace($NodePath) -or -not (Test-Path -LiteralPath $NodePath -PathType Leaf)) {
    throw 'Node.js 18 or newer is required. Install Node.js and run this script again.'
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw 'npm is required but was not found on PATH.'
}

Push-Location $sheetToolDirectory
try {
    npm ci
}
finally {
    Pop-Location
}

Write-Host 'FeatherFramework dependencies are ready.'
