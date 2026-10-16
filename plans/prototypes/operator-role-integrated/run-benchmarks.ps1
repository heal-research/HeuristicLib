param([ValidateSet('Count', 'Duration', 'Stack', 'All')][string]$Category = 'Count')
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
$benchmarkRoot = Join-Path $repoRoot 'artifacts/operator-role-integrated/benchmarks'
$resultsRoot = Join-Path $repoRoot "artifacts/operator-role-integrated/checkpoint-3/$Category"
New-Item -ItemType Directory -Path $benchmarkRoot, $resultsRoot -Force | Out-Null

& (Join-Path $PSScriptRoot 'run.ps1') -BuildOnly

$document = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><IsPackable>false</IsPackable></PropertyGroup><ItemGroup /></Project>'
$items = $document.SelectSingleNode('/Project/ItemGroup')
foreach ($item in @(@('Compile', (Join-Path $PSScriptRoot 'Benchmarks/*.cs')),
    @('ProjectReference', (Join-Path $repoRoot 'artifacts/operator-role-integrated/backend/PrototypeBackend.csproj')))) {
    $element = $document.CreateElement($item[0])
    $element.SetAttribute('Include', $item[1])
    [void]$items.AppendChild($element)
}
$package = $document.CreateElement('PackageReference')
$package.SetAttribute('Include', 'BenchmarkDotNet')
$package.SetAttribute('Version', '0.15.8')
[void]$items.AppendChild($package)
$project = Join-Path $benchmarkRoot 'InspectionBenchmarks.csproj'
$document.Save($project)

Push-Location -LiteralPath $benchmarkRoot
try {
    & dotnet restore $project --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Benchmark restore failed.' }
    & dotnet build $project --configuration Release --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Benchmark build failed.' }
    Write-Output "Benchmark assembly SHA256=$((Get-FileHash -LiteralPath (Join-Path $benchmarkRoot 'bin/Release/net10.0/InspectionBenchmarks.dll') -Algorithm SHA256).Hash)"
    [ordered]@{
        Category = $Category; RecordedAtUtc = [DateTime]::UtcNow.ToString('O'); HEAD = (& git rev-parse HEAD)
        SDK = (& dotnet --version); BenchmarkDotNet = '0.15.8'; LaunchCount = 3; WarmupCount = 6; IterationCount = 12; IterationTimeMs = 100
        BackendSHA256 = (Get-FileHash -LiteralPath (Join-Path $repoRoot 'artifacts/operator-role-integrated/backend/bin/Release/net10.0/HEAL.HeuristicLib.dll') -Algorithm SHA256).Hash
        BenchmarkSHA256 = (Get-FileHash -LiteralPath (Join-Path $benchmarkRoot 'bin/Release/net10.0/InspectionBenchmarks.dll') -Algorithm SHA256).Hash
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $resultsRoot 'provenance.json')
    $filter = if ($Category -eq 'All') { '*' } else { "*$Category" }
    $startedUtc = [DateTime]::UtcNow
    & dotnet run --project $project --configuration Release --no-build --no-restore -- --filter $filter --artifacts $resultsRoot |
        Tee-Object -FilePath (Join-Path $resultsRoot 'console.txt') |
        Where-Object { $_ -match '^// (Benchmark|Execute|Remaining|Found|.*Results)|^\|' }
    if ($LASTEXITCODE -ne 0) { throw 'Benchmark process failed.' }
    $reports = @(Get-ChildItem -LiteralPath (Join-Path $resultsRoot 'results') -Filter '*-report-full.json' -File)
    if ($reports.Count -ne 1) { throw 'Expected exactly one full benchmark JSON report.' }
    if ($reports[0].LastWriteTimeUtc -lt $startedUtc) { throw 'The benchmark report is stale.' }
    $report = Get-Content -Raw -LiteralPath $reports[0].FullName | ConvertFrom-Json
    if (@($report.Benchmarks).Count -lt 2 -or @($report.Benchmarks | Where-Object { $null -eq $_.Statistics }).Count -ne 0) {
        throw 'Benchmark report contains missing or failed measurements.'
    }
    Write-Output "Benchmark report=$($reports[0].FullName)"
    & (Join-Path $PSScriptRoot 'summarize-benchmarks.ps1') -Category $Category
}
finally { Pop-Location }
