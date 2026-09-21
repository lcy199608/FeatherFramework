[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$catalogPath = Join-Path $repositoryRoot 'Config/framework-modules.json'
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$errors = [System.Collections.Generic.List[string]]::new()

$requiredIds = @('assets', 'events', 'save', 'config', 'timers', 'scenes', 'pools', 'ui', 'audio')
$ids = @($catalog.modules | ForEach-Object { $_.id })
foreach ($requiredId in $requiredIds) {
    if ($ids -notcontains $requiredId) {
        $errors.Add("Capability catalog is missing core module '$requiredId'.")
    }
}
if (($ids | Select-Object -Unique).Count -ne $ids.Count) {
    $errors.Add('Capability catalog contains duplicate module ids.')
}

$sourceFiles = foreach ($relativeRoot in $catalog.rules.gameCodeRoots) {
    $absoluteRoot = Join-Path $repositoryRoot $relativeRoot
    if (Test-Path -LiteralPath $absoluteRoot -PathType Container) {
        Get-ChildItem -LiteralPath $absoluteRoot -Recurse -File -Filter '*.cs'
    }
}

$sourceFiles = @($sourceFiles | Where-Object {
    $include = $_.FullName -notmatch '[\\/]Editor[\\/]'
    $directory = $_.Directory
    while ($include -and $directory -and $directory.FullName.StartsWith($repositoryRoot)) {
        $assembly = Get-ChildItem -LiteralPath $directory.FullName -Filter '*.asmdef' -File | Select-Object -First 1
        if ($assembly) {
            $definition = Get-Content -LiteralPath $assembly.FullName -Raw | ConvertFrom-Json
            if ($definition.optionalUnityReferences -contains 'TestAssemblies') { $include = $false }
            break
        }
        $directory = $directory.Parent
    }
    $include
})

$forbidden = [ordered]@{
    'Addressables\.' = 'Use Framework.Services.Assets so loading and ownership stay centralized.'
    'Resources\.(Load|LoadAsync)' = 'Use Framework.Services.Assets; Resources bypasses the project asset policy.'
    '\bES3\.' = 'Use Framework.Services.Save so encryption, compatibility and error handling are preserved.'
    '\b(SceneManager|UnityEngine\.SceneManagement\.SceneManager)\.' = 'Use Framework.Services.Scenes so transitions and progress events remain integrated.'
    '\bnew\s+(ResMgr|EventCenter|SaveDataMgr|ConfigMgr|TimerMgr|SceneMgr|PoolMgr|LanguageMgr|RedDotSystem)\b' = 'Framework services are composed once by FrameworkHost; consume Framework.Services instead.'
    '\b(?!BindingFlags\b)[A-Za-z0-9_]+\.(Instance|instance)\b' = 'Do not add global singleton access in game code; use Framework.Services or explicit dependency injection.'
}

foreach ($file in $sourceFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    # Preserve line positions while ignoring comments, ordinary/verbatim strings and character literals.
    $literalPattern = '(?s)//[^\r\n]*|/\*.*?\*/|@"(?:""|[^"])*"|\$?"(?:\\.|[^"\\])*"|''(?:\\.|[^''\\])*'''
    $content = [regex]::Replace($content, $literalPattern, [System.Text.RegularExpressions.MatchEvaluator]{
        param($match)
        [regex]::Replace($match.Value, '[^\r\n]', ' ')
    })
    $relative = $file.FullName.Substring($repositoryRoot.Length + 1)
    foreach ($pattern in $forbidden.Keys) {
        foreach ($match in [regex]::Matches($content, $pattern)) {
            $line = 1 + ([regex]::Matches($content.Substring(0, $match.Index), '\n')).Count
            $errors.Add("${relative}:${line}: $($forbidden[$pattern])")
        }
    }

    # Manager/Mgr suffixes do not prove that a business class duplicates infrastructure.
    # Responsibility is reviewed against the capability catalog; enforce concrete bypasses above.
}

if ($errors.Count -gt 0) {
    Write-Error ("Architecture check failed:`n - " + ($errors -join "`n - "))
}

Write-Host "Architecture check passed ($($catalog.modules.Count) registered capabilities, $(@($sourceFiles).Count) game source files scanned)."
