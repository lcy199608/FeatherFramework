[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Query
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$catalogPath = Join-Path $repositoryRoot 'Config/framework-modules.json'
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$terms = $Query.ToLowerInvariant() -split '[\s,，/]+' | Where-Object { $_ }

$matches = foreach ($module in $catalog.modules) {
    $searchable = (@($module.id, $module.entry, $module.implementation, $module.purpose) + @($module.keywords)) -join ' '
    $searchable = $searchable.ToLowerInvariant()
    $score = @($terms | Where-Object { $searchable.Contains($_) }).Count
    if ($score -gt 0) {
        [PSCustomObject]@{
            Score = $score
            Module = $module.id
            Status = $module.status
            Entry = $module.entry
            Purpose = $module.purpose
        }
    }
}

$matches = @($matches | Sort-Object @{ Expression = 'Score'; Descending = $true }, Module)
if ($matches.Count -eq 0) {
    Write-Host "No existing capability matched '$Query'. Review Config/framework-modules.json before proposing a new module."
    exit 2
}

$matches | Format-Table Module, Status, Entry, Purpose -AutoSize
