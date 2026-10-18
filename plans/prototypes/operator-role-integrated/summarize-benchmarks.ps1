param([Parameter(Mandatory)][ValidateSet('Count', 'Duration', 'Stack', 'All')][string]$Category)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
$resultsRoot = Join-Path $repoRoot "artifacts/operator-role-integrated/checkpoint-3/$Category"
$reports = @(Get-ChildItem -LiteralPath (Join-Path $resultsRoot 'results') -Filter '*-report-full.json' -File)
if ($reports.Count -ne 1) { throw 'Expected exactly one full benchmark JSON report.' }
$report = Get-Content -Raw -LiteralPath $reports[0].FullName | ConvertFrom-Json
$provenance = Get-Content -Raw -LiteralPath (Join-Path $resultsRoot 'provenance.json') | ConvertFrom-Json
$gates = @()
$profiles = if ($Category -eq 'All') { @('Count', 'Duration', 'Stack') } else { @($Category) }
if (@($report.Benchmarks).Count -ne 2 * $profiles.Count) { throw 'Unexpected benchmark case count.' }
foreach ($profile in $profiles) {
    $direct = @($report.Benchmarks | Where-Object Method -eq "Direct$profile")
    $shared = @($report.Benchmarks | Where-Object Method -eq "Shared$profile")
    if ($direct.Count -ne 1 -or $shared.Count -ne 1 -or $null -eq $direct[0].Statistics -or $null -eq $shared[0].Statistics) {
        throw "Missing measurements for $profile."
    }
    $direct = $direct[0]
    $shared = $shared[0]
    foreach ($measurement in @($direct, $shared)) {
        $launches = @($measurement.Measurements | Where-Object { $_.IterationMode -eq 'Workload' -and $_.IterationStage -eq 'Result' } |
            Select-Object -ExpandProperty LaunchIndex -Unique)
        if ($launches.Count -ne $provenance.LaunchCount) { throw 'Missing independent process results.' }
    }
    $lower = $shared.Statistics.ConfidenceInterval.Lower - $direct.Statistics.ConfidenceInterval.Upper
    $upper = $shared.Statistics.ConfidenceInterval.Upper - $direct.Statistics.ConfidenceInterval.Lower
    $budget = switch ($profile) { 'Count' { 5.0 }; 'Duration' { 10.0 }; 'Stack' { $null } }
    $allocationPass = $direct.Memory.BytesAllocatedPerOperation -eq 0 -and $shared.Memory.BytesAllocatedPerOperation -eq 0
    $status = if (-not $allocationPass) { 'Failed allocations' } elseif ($null -eq $budget -or $upper -le $budget) { 'Passed' }
        elseif ($lower -gt $budget) { 'Failed latency' } else { 'Uncertain latency' }
    $gates += [pscustomobject]@{
        Profile = $profile; DirectMeanNs = $direct.Statistics.Mean; SharedMeanNs = $shared.Statistics.Mean
        DirectMedianNs = $direct.Statistics.Median; SharedMedianNs = $shared.Statistics.Median
        MeanDeltaNs = $shared.Statistics.Mean - $direct.Statistics.Mean
        ConservativeDeltaLowerNs = $lower; ConservativeDeltaUpperNs = $upper; LatencyBudgetNs = $budget
        DirectBytesPerCall = $direct.Memory.BytesAllocatedPerOperation; SharedBytesPerCall = $shared.Memory.BytesAllocatedPerOperation
        Status = $status
    }
    Write-Output "$profile`: $status; direct=$($direct.Statistics.Mean) ns; shared=$($shared.Statistics.Mean) ns; delta interval=[$lower,$upper] ns; bytes=$($shared.Memory.BytesAllocatedPerOperation)"
}
if ($gates.Count -eq 0) { throw 'No benchmark pairs were found.' }
$summary = [ordered]@{
    Provenance = $provenance; Environment = $report.HostEnvironmentInfo
    RawReportSHA256 = (Get-FileHash -LiteralPath $reports[0].FullName -Algorithm SHA256).Hash
    Gates = $gates
    Benchmarks = @($report.Benchmarks | ForEach-Object {
        [pscustomobject]@{ Method = $_.Method; Statistics = $_.Statistics; Memory = $_.Memory
            WorkloadResults = @($_.Measurements | Where-Object { $_.IterationMode -eq 'Workload' -and $_.IterationStage -eq 'Result' }) }
    })
}
$summary | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $resultsRoot "$($Category.ToLowerInvariant())-summary.json")
if (@($gates | Where-Object Status -ne 'Passed').Count -ne 0) { throw 'A benchmark gate failed or remains uncertain.' }
