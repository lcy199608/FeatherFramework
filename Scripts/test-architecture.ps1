[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('feather-architecture-' + [guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path "$fixtureRoot/Scripts", "$fixtureRoot/Config", "$fixtureRoot/Game" -Force | Out-Null
    Copy-Item -LiteralPath "$PSScriptRoot/check-architecture.ps1" -Destination "$fixtureRoot/Scripts/check-architecture.ps1"
    $catalog = Get-Content -LiteralPath "$repositoryRoot/Config/framework-modules.json" -Raw | ConvertFrom-Json
    $catalog.rules.gameCodeRoots = @('Game')
    $catalog | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath "$fixtureRoot/Config/framework-modules.json" -Encoding UTF8
    $cases = @(
        @{ Source = '// Resources.Load("item"); ES3.Save();'; Reject = $false },
        @{ Source = 'class Notes { string text = "SceneManager.LoadScene(0)"; }'; Reject = $false },
        @{ Source = '/* Resources.Load("item") */ class Notes { void Start() { ES3.Save("item", 1); } }'; Reject = $true },
        @{ Source = 'class InventoryManager {} class QuestMgr {}'; Reject = $false },
        @{ Source = 'class InventoryManager { void Load() { Resources.Load("item"); } }'; Reject = $true },
        @{ Source = 'class QuestMgr { void Start() { QuestMgr.Instance.Run(); } }'; Reject = $true },
        @{ Source = 'class InventoryManager { void Start() { new PoolMgr(null, null); } }'; Reject = $true }
    )
    foreach ($case in $cases) {
        Set-Content -LiteralPath "$fixtureRoot/Game/Example.cs" -Value $case.Source -Encoding UTF8
        $rejected = $false
        try { & "$fixtureRoot/Scripts/check-architecture.ps1" }
        catch {
            if ($_.Exception.Message -notlike 'Architecture check failed:*') { throw }
            $rejected = $true
        }
        if ($rejected -ne $case.Reject) { throw "Unexpected architecture result for: $($case.Source)" }
    }
    Write-Host "Architecture rule tests passed ($($cases.Count)/$($cases.Count))."
}
finally {
    # Delete only the uniquely named fixture created by this run under the temp directory.
    $resolvedFixture = [System.IO.Path]::GetFullPath($fixtureRoot)
    $tempPrefix = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (!$resolvedFixture.StartsWith($tempPrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path -Leaf $resolvedFixture) -notmatch '^feather-architecture-[0-9a-f]{32}$') {
        throw 'Refusing to remove an unexpected fixture path.'
    }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
