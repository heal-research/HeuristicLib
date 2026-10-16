param([switch]$BuildOnly)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
$outputRoot = Join-Path $repoRoot 'artifacts/operator-role-integrated'
$backendDir = Join-Path $outputRoot 'backend'
$consumerDir = Join-Path $outputRoot 'consumer'
$compilerDir = Join-Path $outputRoot 'compiler'
$logDir = Join-Path $outputRoot 'logs'
$script:commandNumber = 0
New-Item -ItemType Directory -Path $backendDir, $consumerDir, $compilerDir, $logDir -Force | Out-Null

function Invoke-Dotnet([string[]]$Arguments, [string]$ExpectedDiagnostic = '') {
    $output = & dotnet @Arguments 2>&1
    $code = $LASTEXITCODE
    $script:commandNumber++
    $output | Set-Content -LiteralPath (Join-Path $logDir "command-$($script:commandNumber).log")
    if ($ExpectedDiagnostic) {
        $errors = [regex]::Matches(($output -join "`n"), 'error ([A-Z]+\d+):') |
            ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
        if ($code -eq 0 -or @($errors).Count -ne 1 -or $errors -ne $ExpectedDiagnostic) {
            $output | ForEach-Object { Write-Output $_ }
            throw "Expected only $ExpectedDiagnostic; exit=$code; diagnostics=$($errors -join ',')."
        }
        Write-Output "PASS expected compiler diagnostic $ExpectedDiagnostic"
    }
    else {
        $output | Where-Object { "$_" -notmatch ': warning [A-Z]+\d+:' } | ForEach-Object { Write-Output $_ }
        if ($code -ne 0) { throw "dotnet $($Arguments -join ' ') failed with exit $code." }
    }
}

function Add-Element($Document, $Parent, [string]$Name, [string]$Value) {
    $element = $Document.CreateElement($Name)
    $element.InnerText = $Value
    [void]$Parent.AppendChild($element)
}

function Write-ConsumerProject([string]$Directory, [string]$Sources) {
    $document = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><IsPackable>false</IsPackable></PropertyGroup><ItemGroup /></Project>'
    $items = $document.SelectSingleNode('/Project/ItemGroup')
    $compile = $document.CreateElement('Compile')
    $compile.SetAttribute('Include', $Sources)
    [void]$items.AppendChild($compile)
    $reference = $document.CreateElement('ProjectReference')
    $reference.SetAttribute('Include', (Join-Path $backendDir 'PrototypeBackend.csproj'))
    [void]$items.AppendChild($reference)
    $project = Join-Path $Directory 'Consumer.csproj'
    $document.Save($project)
    return $project
}

Push-Location -LiteralPath $repoRoot
try {
    Write-Output 'CHECKPOINTS 1-3: API, ownership, success/lifecycle advice and duration; not Package 0 completion'
    Write-Output "SDK=$((& dotnet --version).Trim()); HEAD=$(& git rev-parse HEAD)"
    Write-Output "Plan baseline=a2643bc1005dc47611699cec603ba0106e4231df"

    # Only these two declarations change in the shipping resolver, in a generated artifact.
    $resolverPath = Join-Path $repoRoot 'src/HeuristicLib/Execution/ResolutionScope.cs'
    $resolver = Get-Content -Raw -LiteralPath $resolverPath
    foreach ($declaration in @('public sealed class ResolutionScope', 'internal sealed class State(State? parent)')) {
        if ([regex]::Matches($resolver, [regex]::Escape($declaration)).Count -ne 1) {
            throw "Shipping resolver declaration drifted: $declaration"
        }
    }
    $resolver.Replace('public sealed class ResolutionScope', 'public sealed partial class ResolutionScope').
        Replace('internal sealed class State(State? parent)', 'internal sealed partial class State(State? parent)') |
        Set-Content -LiteralPath (Join-Path $backendDir 'ResolutionScope.cs')
    Write-Output "Shipping resolver SHA256=$((Get-FileHash -LiteralPath $resolverPath -Algorithm SHA256).Hash)"

    # Mirror production settings, package versions and analyzer references rather than maintaining a second list.
    $sourceProject = Join-Path $repoRoot 'src/HeuristicLib/HeuristicLib.csproj'
    $document = [xml](Get-Content -Raw -LiteralPath $sourceProject)
    foreach ($reference in $document.SelectNodes('//ProjectReference')) {
        $reference.SetAttribute('Include', [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $sourceProject) $reference.Include)))
    }
    $properties = $document.CreateElement('PropertyGroup')
    Add-Element $document $properties 'EnableDefaultCompileItems' 'false'
    Add-Element $document $properties 'IsPackable' 'false'
    [void]$document.Project.AppendChild($properties)
    $items = $document.CreateElement('ItemGroup')
    $compile = $document.CreateElement('Compile')
    $core = (Join-Path $repoRoot 'src/HeuristicLib').Replace('\', '/')
    $compile.SetAttribute('Include', "$core/**/*.cs")
    $compile.SetAttribute('Exclude', "$core/obj/**/*.cs;$core/bin/**/*.cs;$($resolverPath.Replace('\', '/'))")
    [void]$items.AppendChild($compile)
    foreach ($include in @((Join-Path $backendDir 'ResolutionScope.cs'), (Join-Path $PSScriptRoot 'Backend/*.cs'))) {
        $compile = $document.CreateElement('Compile')
        $compile.SetAttribute('Include', $include)
        [void]$items.AppendChild($compile)
    }
    [void]$document.Project.AppendChild($items)
    $backendProject = Join-Path $backendDir 'PrototypeBackend.csproj'
    $document.Save($backendProject)

    $consumerProject = Write-ConsumerProject $consumerDir (Join-Path $PSScriptRoot 'Fixtures/*.cs')
    $compilerProject = Write-ConsumerProject $compilerDir (Join-Path $PSScriptRoot 'CompilerCases.cs')
    Invoke-Dotnet @('restore', $consumerProject, '--verbosity', 'quiet')

    $frameworkJson = & dotnet msbuild $backendProject -target:ResolveFrameworkReferences -getItem:ResolvedFrameworkReference -verbosity:quiet
    if ($LASTEXITCODE -ne 0) { throw 'Cannot determine the selected targeting pack.' }
    $frameworks = (($frameworkJson -join "`n") | ConvertFrom-Json).Items.ResolvedFrameworkReference
    $framework = $frameworks | Where-Object { $_.Identity -eq 'Microsoft.NETCore.App' }
    Write-Output "Reference pack=$($framework.TargetingPackName)/$($framework.TargetingPackVersion)"

    Invoke-Dotnet @('build', $consumerProject, '--configuration', 'Release', '--no-restore', '--verbosity', 'quiet')
    $backend = Join-Path $backendDir 'bin/Release/net10.0/HEAL.HeuristicLib.dll'
    Write-Output "Prototype backend SHA256=$((Get-FileHash -LiteralPath $backend -Algorithm SHA256).Hash)"
    if ($BuildOnly) { return }
    Invoke-Dotnet @('run', '--project', $consumerProject, '--configuration', 'Release', '--no-build', '--no-restore')

    Invoke-Dotnet @('restore', $compilerProject, '--verbosity', 'quiet')
    Invoke-Dotnet @('build', $compilerProject, '--configuration', 'Release', '--no-restore', '--verbosity', 'quiet')
    Invoke-Dotnet @('run', '--project', $compilerProject, '--configuration', 'Release', '--no-build', '--no-restore')
    foreach ($case in @(@('WRONG_CANDIDATE', 'CS0029'), @('WRONG_OPERATION', 'CS0029'),
        @('ALGORITHM_SLOT', 'CS0266'), @('ALGORITHM_NAMED', 'CS1503'), @('WRONG_ADVICE', 'CS0311'))) {
        Write-Output "CASE: $($case[0])"
        Invoke-Dotnet @('build', $compilerProject, '--configuration', 'Release', '--no-restore', '--verbosity', 'quiet',
            "-p:DefineConstants=$($case[0])", '-p:BuildProjectReferences=false') $case[1]
    }
    Write-Output 'PASS checkpoints 1-3 correctness; around policies, metrics, native hosting and size reduction remain unvalidated. Run benchmarks separately.'
}
finally { Pop-Location }
