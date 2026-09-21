[CmdletBinding()]
param(
    [ValidateSet('Architecture', 'SheetTool', 'Tests', 'Full')]
    [string]$Scope = 'Architecture',
    [ValidateSet('ui', 'ui-lifecycle', 'pools', 'save', 'events', 'timers')]
    [string[]]$Module,
    [ValidateNotNullOrEmpty()]
    [string]$TestFilter,
    [ValidateSet('EditMode', 'PlayMode')]
    [string]$TestPlatform = 'EditMode',
    [string]$UnityPath,
    [string]$NodePath,
    [switch]$SkipUnity,
    [switch]$Plan
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$sheetToolDirectory = Join-Path $repositoryRoot 'Config/SheetTool'
$unityProjectDirectory = Join-Path $repositoryRoot 'Client'
$testResultsDirectory = Join-Path $repositoryRoot 'TestResults'

# Selection is explicit: never infer changed modules from a dirty working tree.
if ($SkipUnity) {
    if ($PSBoundParameters.ContainsKey('Scope') -or $Module -or $PSBoundParameters.ContainsKey('TestFilter')) {
        throw '-SkipUnity is the legacy alias for -Scope SheetTool; do not combine it with a scope or test selection.'
    }
    $Scope = 'SheetTool'
}
elseif (-not $PSBoundParameters.ContainsKey('Scope') -and ($Module -or $PSBoundParameters.ContainsKey('TestFilter'))) {
    $Scope = 'Tests'
}

if ($Module -and $PSBoundParameters.ContainsKey('TestFilter')) {
    throw 'Choose -Module or -TestFilter, not both.'
}
if ($Scope -ne 'Tests' -and ($Module -or $PSBoundParameters.ContainsKey('TestFilter'))) {
    throw '-Module and -TestFilter require -Scope Tests. Full runs cannot be filtered.'
}
if ($Scope -eq 'Tests' -and -not $Module -and [string]::IsNullOrWhiteSpace($TestFilter)) {
    throw 'Select -Module or -TestFilter for targeted tests; use -Scope Full for a full run.'
}
if ($Module -and $TestPlatform -ne 'EditMode') {
    throw 'Module shortcuts select EditMode fixtures (ui-lifecycle enters Play Mode itself). Use -TestFilter for a PlayMode assembly.'
}
if ($Scope -ne 'Tests' -and $PSBoundParameters.ContainsKey('TestPlatform')) {
    throw '-TestPlatform is for targeted tests. Full runs include all current EditMode fixtures, including the play lifecycle fixture.'
}

$moduleFixtures = @{
    'ui' = 'Feather.Tests.UIMgrTests'
    'ui-lifecycle' = 'Feather.Tests.UIPlayLifecycleTests'
    'pools' = 'Feather.Tests.PoolMgrTests'
    'save' = 'Feather.Tests.SaveDataMgrTests'
    'events' = 'Feather.Tests.EventCenterTests'
    'timers' = 'Feather.Tests.TimerMgrTests'
}
$filter = $TestFilter
$selection = 'custom'
if ($Module) {
    $Module = @($Module | Select-Object -Unique)
    # Unity accepts semicolon-separated regexes; anchor fixtures to avoid matching unrelated classes.
    $filter = ($Module | ForEach-Object { '^' + [regex]::Escape($moduleFixtures[$_]) + '\.' }) -join ';'
    $selection = ($Module | ForEach-Object { $_.ToLowerInvariant() }) -join '-'
}
$runUnity = $Scope -in @('Tests', 'Full')
$runSheetTool = $Scope -in @('SheetTool', 'Full')
if ($PSBoundParameters.ContainsKey('UnityPath') -and -not $runUnity) {
    throw '-UnityPath requires an explicit test selection (-Module/-TestFilter) or -Scope Full; it no longer implies a full run.'
}
$resultName = if ($Scope -eq 'Full') { 'editmode' } else { "$($TestPlatform.ToLowerInvariant())-$selection" }
$resultFile = Join-Path $testResultsDirectory "$resultName.xml"
$logFile = Join-Path $testResultsDirectory "$resultName.log"
$unityArguments = @(
    '-batchmode', '-nographics',
    '-projectPath', $unityProjectDirectory,
    # The test runner writes results and exits; -quit would abort it during startup.
    '-runTests', '-testPlatform', $TestPlatform,
    '-testResults', $resultFile, '-logFile', $logFile
)
if ($Scope -eq 'Tests') { $unityArguments += @('-testFilter', $filter) }

if ($Plan) {
    [pscustomobject]@{
        Scope = $Scope
        Architecture = $true
        SheetTool = $runSheetTool
        Unity = $runUnity
        TestPlatform = if ($runUnity) { $TestPlatform } else { $null }
        TestFilter = if ($Scope -eq 'Tests') { $filter } else { $null }
        ResultFile = if ($runUnity) { $resultFile } else { $null }
        UnityArguments = if ($runUnity) { $unityArguments } else { @() }
    }
    return
}

# Reject missing Unity before doing other work; modules never require Node.
if ($runUnity -and ([string]::IsNullOrWhiteSpace($UnityPath) -or
    -not (Test-Path -LiteralPath $UnityPath -PathType Leaf))) {
    throw 'Provide -UnityPath pointing to Unity.exe for Tests/Full. Use -Plan to preview without starting Unity.'
}

& (Join-Path $PSScriptRoot 'check-architecture.ps1')
if (-not $?) { throw 'Architecture verification failed.' }

function Resolve-NodeExecutable {
    if (-not [string]::IsNullOrWhiteSpace($NodePath)) { return $NodePath }
    if (-not [string]::IsNullOrWhiteSpace($env:FEATHER_NODE_PATH)) { return $env:FEATHER_NODE_PATH }
    $nodeCommand = Get-Command node -ErrorAction SilentlyContinue
    if ($nodeCommand) { return $nodeCommand.Source }
    $bundledNode = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
    if (Test-Path -LiteralPath $bundledNode -PathType Leaf) { return $bundledNode }
    return $null
}

if ($runSheetTool) {
    $nodeExecutable = Resolve-NodeExecutable
    if ([string]::IsNullOrWhiteSpace($nodeExecutable) -or -not (Test-Path -LiteralPath $nodeExecutable -PathType Leaf)) {
        throw 'Node.js 18 or newer is required for SheetTool validation.'
    }
    $nodeMajorVersion = [int](& $nodeExecutable -p 'Number(process.versions.node.split(".")[0])')
    if ($nodeMajorVersion -lt 18) { throw "Node.js 18 or newer is required; found major version $nodeMajorVersion." }

    Push-Location $sheetToolDirectory
    try {
        & $nodeExecutable ./cli.js validate
        if ($LASTEXITCODE -ne 0) { throw 'SheetTool validation failed.' }
        & $nodeExecutable --test ./test/*.test.js
        if ($LASTEXITCODE -ne 0) { throw 'SheetTool tests failed.' }
    }
    finally { Pop-Location }
}

if ($runUnity) {
    New-Item -ItemType Directory -Path $testResultsDirectory -Force | Out-Null
    foreach ($outputFile in @($resultFile, $logFile)) {
        if (Test-Path -LiteralPath $outputFile -PathType Leaf) { Remove-Item -LiteralPath $outputFile -Force }
    }
    # Start-Process joins arguments into one Windows command line. Quote each argument,
    # including paths with spaces and filter expressions, using Windows escaping rules.
    $quotedArguments = foreach ($argument in $unityArguments) {
        '"' + ([regex]::Replace([string]$argument, '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"'
    }
    Write-Host "Unity verification: $Scope; platform: $TestPlatform; filter: $(if ($filter) { $filter } else { '(all)' })"
    $unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $quotedArguments -Wait -PassThru -WindowStyle Hidden
    if ($unityProcess.ExitCode -ne 0) {
        throw "Unity verification failed with exit code $($unityProcess.ExitCode). See $logFile"
    }
    if (-not (Test-Path -LiteralPath $resultFile -PathType Leaf)) {
        throw "Unity exited without producing test results. See $logFile"
    }

    [xml]$testResults = Get-Content -LiteralPath $resultFile -Raw
    $testRun = $testResults.'test-run'
    if ($null -eq $testRun -or $testRun.result -ne 'Passed' -or [int]$testRun.failed -ne 0 -or
        [int]$testRun.total -le 0 -or [int]$testRun.passed -le 0 -or
        @($testResults.SelectNodes('//test-case[@result="Passed"]')).Count -eq 0) {
        throw "Unity tests did not pass, or the selection ran no passing tests. See $resultFile and $logFile"
    }
    Write-Host "Unity passed: $($testRun.passed)/$($testRun.total). Results: $resultFile"
}

Write-Host "FeatherFramework verification passed (scope: $Scope)."
