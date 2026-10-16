param([switch]$CoreOnly, [switch]$ConstructionOnly, [switch]$SkipPerformance)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
$outputRoot = Join-Path $repoRoot 'artifacts/operator-role-slice'
$outDir = Join-Path $outputRoot 'bin'
$libraryDir = Join-Path $repoRoot 'src/HeuristicLib/bin/Release/net10.0'
$library = Join-Path $libraryDir 'HEAL.HeuristicLib.dll'
if (-not (Test-Path -LiteralPath $library)) { throw 'First build src/HeuristicLib/HeuristicLib.csproj in Release.' }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
Push-Location -LiteralPath $repoRoot
try {
    $sdkVersion = (& dotnet --version).Trim()
    $dotnetRoot = Split-Path -Parent (Get-Command dotnet).Source
    $compiler = Join-Path $dotnetRoot "sdk/$sdkVersion/Roslyn/bincore/csc.dll"
    $pack = Get-ChildItem -LiteralPath (Join-Path $dotnetRoot 'packs/Microsoft.NETCore.App.Ref') -Directory |
        Where-Object { $_.Name -match '^10\.0\.\d+$' } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    $references = Get-ChildItem -LiteralPath (Join-Path $pack.FullName 'ref/net10.0') -Filter '*.dll' |
        ForEach-Object { '/reference:"' + $_.FullName + '"' }
    $references += '/reference:"' + $library + '"'
    Copy-Item -LiteralPath $library -Destination (Join-Path $outDir 'HEAL.HeuristicLib.dll')
    Write-Output "SDK=$sdkVersion; reference pack=$($pack.Name); HEAD=$(& git rev-parse HEAD)"
    Write-Output "HLib SHA256=$((Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash)"
    function Compile-Probe {
        param([string]$Name, [string[]]$Sources, [switch]$Library, [string[]]$ExtraReferences = @(), [string]$Define = 'CORE', [string]$ExpectedDiagnostic = '')
        $target = if ($Library) { 'library' } else { 'exe' }
        $lines = @('/nologo', "/target:$target", '/langversion:14.0', '/nullable:enable', '/optimize+', "/define:$Define", ('/out:"' + (Join-Path $outDir "$Name.dll") + '"'))
        $lines += $references
        $lines += $ExtraReferences | ForEach-Object { '/reference:"' + $_ + '"' }
        $lines += $Sources | ForEach-Object { '"' + (Join-Path $PSScriptRoot $_) + '"' }
        $responseFile = Join-Path $outDir "$Name.rsp"
        Set-Content -LiteralPath $responseFile -Value $lines
        Write-Output "CASE: $Name / $Define"
        $diagnostics = & dotnet $compiler "@$responseFile" 2>&1
        $compileExit = $LASTEXITCODE
        $diagnostics | ForEach-Object { Write-Output $_ }
        if ($ExpectedDiagnostic) {
            if ($compileExit -eq 0 -or ($diagnostics -join "`n") -notmatch $ExpectedDiagnostic) { throw "Expected $ExpectedDiagnostic; compiler exit=$compileExit." }
        }
        elseif ($compileExit -ne 0) { throw "$Name compilation failed." }
        if (-not $Library -and -not $ExpectedDiagnostic) {
            Set-Content -LiteralPath (Join-Path $outDir "$Name.runtimeconfig.json") -Value '{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}'
        }
    }
    function Run-Probe([string]$Name) {
        & dotnet exec --depsfile (Join-Path $outDir 'RoleSlice.deps.json') --additionalprobingpath (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages') (Join-Path $outDir "$Name.dll")
        if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit $LASTEXITCODE." }
    }
    Compile-Probe -Name 'RoleSlice' -Library -Sources @('Usings.cs', 'Core.cs', 'SynchronousRoles.cs', 'Authoring.cs', 'Operators.cs', 'Algorithms.cs', 'Inspection.cs', 'FluentConfiguration.cs')
    $dependencies = Get-Content -Raw -LiteralPath (Join-Path $libraryDir 'HEAL.HeuristicLib.deps.json') | ConvertFrom-Json -AsHashtable
    $dependencies.targets[$dependencies.runtimeTarget.name]['RoleSlice/0.0.0'] = @{ runtime = @{ 'RoleSlice.dll' = @{} } }
    $dependencies.libraries['RoleSlice/0.0.0'] = @{ type = 'project'; serviceable = $false; sha512 = '' }
    $dependencies | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $outDir 'RoleSlice.deps.json')
    if ($CoreOnly) { return }
    $prototypeReferences = @((Join-Path $outDir 'RoleSlice.dll'))
    Compile-Probe -Name 'ConstructionCases' -Sources @('Usings.cs', 'ConstructionCases.cs') -ExtraReferences $prototypeReferences
    Run-Probe 'ConstructionCases'
    if ($ConstructionOnly) { return }
    Compile-Probe -Name 'Scenarios' -Sources @('Usings.cs', 'Scenarios.cs') -ExtraReferences $prototypeReferences
    Run-Probe 'Scenarios'
    Compile-Probe -Name 'ExternalConsumer' -Sources @('Usings.cs', 'ExternalConsumer.cs') -ExtraReferences $prototypeReferences
    Run-Probe 'ExternalConsumer'
    foreach ($case in @(@('WRONG_CANDIDATE', 'CS0029'), @('WRONG_ROLE', 'CS0029'), @('STREAMING_RETRY', 'CS0311'), @('WRONG_STATE', 'CS0029'), @('WRONG_OBSERVER', 'CS0311'), @('UNSUPPORTED_METRIC', 'CS0311'), @('WRONG_FLUENT_ROLE', 'CS1503'), @('UNSUPPORTED_CONCURRENCY', 'CS0311'))) {
        Compile-Probe -Name 'CompilerCases' -Sources @('Usings.cs', 'CompilerCases.cs') -ExtraReferences $prototypeReferences -Define $case[0] -ExpectedDiagnostic $case[1]
    }
    Compile-Probe -Name 'CompilerCases' -Sources @('Usings.cs', 'CompilerCases.cs') -ExtraReferences $prototypeReferences
    Run-Probe 'CompilerCases'
    Compile-Probe -Name 'StateFamilies' -Sources @('Usings.cs', 'StateFamilies.cs') -ExtraReferences $prototypeReferences
    Run-Probe 'StateFamilies'
    if (-not $SkipPerformance) {
        Compile-Probe -Name 'Performance' -Sources @('Usings.cs', 'Performance.cs') -ExtraReferences $prototypeReferences
        Run-Probe 'Performance'
    }
}
finally { Pop-Location }
