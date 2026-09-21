# Tests the verification wrapper only. Never launches Unity.
$ErrorActionPreference = 'Stop'
$verify = Join-Path $PSScriptRoot 'verify.ps1'
$verificationState = @{ Checks = 0; ReportMode = 'passed'; CapturedArguments = @(); ExitCode = 0 }
function Assert-Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $verificationState.Checks++
}
function Assert-Rejected([scriptblock]$Action, [string]$Message) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    Assert-Check $rejected $Message
}

$default = & $verify -Plan
Assert-Check ($default.Scope -eq 'Architecture' -and -not $default.Unity -and -not $default.SheetTool) 'Default must stay lightweight.'
$old = & $verify -SkipUnity -Plan
Assert-Check ($old.Scope -eq 'SheetTool' -and $old.SheetTool -and -not $old.Unity) 'SkipUnity alias changed.'
$sheet = & $verify -Scope SheetTool -Plan
Assert-Check ($sheet.SheetTool -and -not $sheet.Unity) 'SheetTool must not launch Unity.'
$full = & $verify -Scope Full -Plan
Assert-Check ($full.Unity -and $full.SheetTool -and -not $full.TestFilter) 'Full must be unfiltered.'
Assert-Check ($full.UnityArguments -notcontains '-quit') 'Test runner must not receive -quit.'
$ui = & $verify -Module ui -Plan
Assert-Check ($ui.Scope -eq 'Tests' -and -not $ui.SheetTool) 'Module selection must skip SheetTool.'
Assert-Check ('Feather.Tests.UIMgrTests.Test' -match $ui.TestFilter) 'UI shortcut misses its fixture.'
Assert-Check ('Feather.Tests.UIPlayLifecycleTests.Test' -notmatch $ui.TestFilter) 'UI shortcut must not enter Play Mode.'
Assert-Check ('Other.Feather.Tests.UIMgrTests.Test' -notmatch $ui.TestFilter) 'Fixture filter must be anchored.'
$combined = & $verify -Module ui,ui-lifecycle,ui -Plan
Assert-Check ($combined.TestFilter.Split(';').Count -eq 2) 'Duplicate modules should be removed.'
foreach ($moduleName in @('ui-lifecycle', 'pools', 'save', 'events', 'timers')) {
    $modulePlan = & $verify -Module $moduleName -Plan
    Assert-Check ($modulePlan.Unity -and -not $modulePlan.SheetTool) "Module $moduleName routed incorrectly."
    Assert-Check ($modulePlan.ResultFile -ne $full.ResultFile) 'Targeted results must not overwrite full results.'
}
$custom = & $verify -TestFilter 'Game.Tests.CombatTests' -TestPlatform PlayMode -Plan
Assert-Check ($custom.TestPlatform -eq 'PlayMode' -and $custom.TestFilter -eq 'Game.Tests.CombatTests') 'Custom selection lost.'
Assert-Rejected { & $verify -Scope Tests -Plan } 'Unfiltered targeted scope must fail.'
Assert-Rejected { & $verify -Scope Full -Module ui -Plan } 'Full cannot secretly run a subset.'
Assert-Rejected { & $verify -Module ui -TestFilter Example -Plan } 'Ambiguous filter must fail.'
Assert-Rejected { & $verify -SkipUnity -Module ui -Plan } 'SkipUnity cannot disable selected module tests.'
Assert-Rejected { & $verify -Scope Full -TestPlatform PlayMode -Plan } 'Full cannot silently omit EditMode.'
Assert-Rejected { & $verify -Module ui -TestPlatform PlayMode -Plan } 'Editor fixture cannot use a PlayMode-only filter.'
Assert-Rejected { & $verify -TestFilter ' ' -Plan } 'Blank filters must fail.'
Assert-Rejected { & $verify -Module unknown -Plan } 'Unknown modules must fail.'
Assert-Rejected { & $verify -UnityPath 'Unity.exe' -Plan } 'Old full command must fail explicitly rather than silently skip Unity.'
Assert-Rejected { & $verify -Module ui -UnityPath 'Z:/missing/Unity.exe' } 'Missing Unity must fail before running tests.'

# Isolate output and mock the process boundary to test report handling and path quoting.
$resultsRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'TestResults'
New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null
$fixtureRoot = Join-Path $resultsRoot ('verify script ' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path (Join-Path $fixtureRoot 'Scripts') | Out-Null
Copy-Item -LiteralPath $verify -Destination (Join-Path $fixtureRoot 'Scripts/verify.ps1')
Set-Content -LiteralPath (Join-Path $fixtureRoot 'Scripts/check-architecture.ps1') -Value '# Isolated architecture stub.'
$fakeUnity = Join-Path $fixtureRoot 'fake Unity.txt'
Set-Content -LiteralPath $fakeUnity -Value 'Not executable: launcher is mocked.'
$fixtureVerify = Join-Path $fixtureRoot 'Scripts/verify.ps1'
$verificationState.ReportMode = 'passed'
$verificationState.CapturedArguments = @()
function Start-Process {
    param($FilePath, $ArgumentList, [switch]$Wait, [switch]$PassThru, $WindowStyle)
    Assert-Check ($FilePath -eq $fakeUnity -and $WindowStyle -eq 'Hidden') 'Unexpected launch target or visible window.'
    $verificationState.CapturedArguments = $ArgumentList
    $plain = @($ArgumentList | ForEach-Object { $_.Substring(1, $_.Length - 2) })
    $output = $plain[[array]::IndexOf($plain, '-testResults') + 1]
    Assert-Check (-not (Test-Path -LiteralPath $output)) 'Stale report was not removed.'
    $report = switch ($verificationState.ReportMode) {
        'passed' { '<test-run result="Passed" total="1" passed="1" failed="0"><test-case result="Passed"/></test-run>' }
        'empty' { '<test-run result="Passed" total="0" passed="0" failed="0"/>' }
        'skipped' { '<test-run result="Passed" total="1" passed="0" failed="0"><test-case result="Skipped"/></test-run>' }
        'failed' { '<test-run result="Failed" total="1" passed="0" failed="1"><test-case result="Failed"/></test-run>' }
        'malformed' { '<broken' }
        'missing' { $null }
    }
    if ($null -ne $report) { Set-Content -LiteralPath $output -Value $report }
    [pscustomobject]@{ ExitCode = $verificationState.ExitCode }
}
try {
    & $fixtureVerify -Module ui -UnityPath $fakeUnity
    Assert-Check (@($verificationState.CapturedArguments | Where-Object { -not ($_.StartsWith('"') -and $_.EndsWith('"')) }).Count -eq 0) 'Each Windows argument must be quoted.'
    foreach ($mode in @('empty', 'skipped', 'failed', 'malformed', 'missing')) {
        $verificationState.ReportMode = $mode
        Assert-Rejected { & $fixtureVerify -Module ui -UnityPath $fakeUnity } "Invalid report accepted: $mode."
    }
    $verificationState.ReportMode = 'passed'
    $verificationState.ExitCode = 2
    Assert-Rejected { & $fixtureVerify -Module ui -UnityPath $fakeUnity } 'A passing report cannot override a process failure.'
}
finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $allowedRoot = [IO.Path]::GetFullPath($resultsRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedFixture.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to clean a fixture outside TestResults.'
    }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
Write-Host "Verification script checks passed: $($verificationState.Checks) (Unity was not launched)."

