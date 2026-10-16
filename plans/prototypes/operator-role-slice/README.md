# Operator role slice

Executable design prototype supporting [the rework plan](../../operator-role-parameterization-rework.md). The shipping library and solution are unchanged.

From the repository root:

```powershell
dotnet build src/HeuristicLib/HeuristicLib.csproj --configuration Release --no-restore
./plans/prototypes/operator-role-slice/run.ps1
```

The runner compiles against the actual Release HLib assembly using the SDK selected by `global.json`. It uses the installed .NET 10 reference pack and existing package cache; no package restore is performed. It prints HEAD and a SHA256 of the backend assembly. Build the backend first when its sources change.

Sources live here as ordinary reviewable C#. Compiler outputs and generated dependency/runtime manifests live under ignored `artifacts/operator-role-slice/bin`. The runner never changes the solution or Git index. `-CoreOnly` compiles only the prototype library for an inner development loop. `-ConstructionOnly` runs the fluent construction follow-up; `-SkipPerformance` runs all functional and compiler checks without repeating the illustrative timing probes.

The retained [results.txt](results.txt) is the initial validated snapshot, including performance measurements before the fluent API follow-up. [fluent-results.txt](fluent-results.txt) records the follow-up's construction checks and rerun of the existing functional/compiler probes with `-SkipPerformance`. Neither report updates automatically. Running the script prints a new result. To retain another local output, pipe it to `Tee-Object` with a destination under `artifacts/operator-role-slice/`.

- `Core.cs`, `SynchronousRoles.cs`, `Authoring.cs`, `Operators.cs`, `Algorithms.cs`, `Inspection.cs` and `FluentConfiguration.cs` form the main prototype library.
- `ConstructionCases.cs` checks complete fluent configuration for seven representative families, factory options/defaults, exact inferred return types, immutable copies, child references, collection snapshots and actual seeded executions from a separate consumer assembly.
- `Scenarios.cs` tests behavior, production comparisons, controls, contracts, algorithms, hosting, analysis and streaming ownership.
- `ExternalConsumer.cs` is compiled separately, proving consumer-defined roles and generic meta operators need no built-in role registry.
- `CompilerCases.cs` has a positive inference example and eight deliberately invalid variants whose diagnostic codes are checked by the runner.
- `StateFamilies.cs` is an isolated alternative foundation with hidden run-state type parameters. It is not a second production API and is not yet merged into the main slice.
- `Performance.cs` contains illustrative warmed local probes. No performance threshold or permanent benchmark project is introduced.

`HostBridge` and `ContractProjection` adapt the proposed configurations to existing hosting/compatibility APIs only for this experiment. They are not proposed shipping compatibility shims. See the plan for remaining migration decisions and validation limits.
