# Performance regression benchmarking

## Summary

This plan defines permanent performance regression testing for representative HeuristicLib operators and workflows. The suite will use BenchmarkDotNet for measurement and a small Spectre.Console.Cli application for benchmark discovery, selection, revision comparison and reporting.

Performance benchmarks are not unit tests. Ordinary tests continue to establish behavior and deterministic invariants. The benchmark suite measures runtime distributions and managed allocations under controlled conditions. It runs independently from `dotnet test` and can later provide a failing CI check on a stable dedicated machine.

This document is the accepted decision required by § 7.2 of the [developer guidelines](../docs/contributing/developer-guidelines.md). It describes a future implementation. It does not add that implementation.

## Motivation

HeuristicLib contains small operators that may run millions of times as well as complete algorithms whose cost emerges from the composition of many operators. A change can preserve behavior while adding allocation, indirection or repeated work to a hot path. Functional tests cannot detect that class of regression.

Elapsed time assertions inside xUnit would not solve the problem. Their results depend on machine load, debugger attachment, process startup, runtime tiering, power management and background services. A fixed time limit also cannot distinguish a real slowdown from one noisy observation.

BenchmarkDotNet addresses the mechanics of measurement. It builds optimized code, performs warmup, selects suitable invocation counts, evaluates overhead, collects repeated measurements and runs benchmarks in isolated processes. The suite still needs HeuristicLib specific policy for workload selection, revision comparison and failure decisions.

## Goals

1. Measure representative HeuristicLib operators and workflows.
2. Detect substantial runtime regressions between Git revisions.
3. Track managed allocations where they matter.
4. Support focused local investigation and repeatable automated execution.
5. Make benchmarks easy to select from a terminal or IDE.
6. Keep ordinary solution tests fast and deterministic.
7. Produce machine readable artifacts and concise human reports.

## Non goals

1. Do not turn unit tests into microbenchmarks.
2. Do not fail builds because one noisy measurement crossed a wall clock limit.
3. Do not promise comparable absolute results across different machines.
4. Do not run the full suite during every ordinary `dotnet test`.
5. Do not benchmark every public method.
6. Do not replace profiling or scenario tests.

## Measurement architecture

Add a console project at `benchmark/HeuristicLib.Benchmarks` and include it in `HEAL.HeuristicLib.slnx`. The project targets the repository's primary .NET target and references the core library. It references the experimental library only for benchmark cases that require it.

The project references:

* BenchmarkDotNet as the measurement engine
* Spectre.Console.Cli for command parsing and interactive selection

The project sets `IsTestProject=false`. It does not reference `Microsoft.NET.Test.Sdk` or `BenchmarkDotNet.TestAdapter`.

BenchmarkDotNet owns the measured process. The command line application selects workloads and configures runs but does not add a timing loop around library calls. BenchmarkDotNet remains responsible for warmup, runtime tiering, repeated launches, overhead evaluation, garbage collection statistics and process isolation.

### VSTest decision

The first implementation will not use the BenchmarkDotNet VSTest adapter. The adapter offers discovery and execution through Visual Studio and Rider, but it also makes benchmarks visible to solution wide test discovery. BenchmarkDotNet warns that the VSTest host and IDE can affect measurements. VSTest's pass or fail result indicates validation or execution success rather than a measured performance threshold.

HeuristicLib requires `dotnet test` to remain a correctness test command. IDE users will run the console project through launch profiles instead of Test Explorer. This preserves BenchmarkDotNet's normal command line execution path and prevents accidental benchmark runs during ordinary validation.

## Command line interface

The console application provides these commands:

| Command | Behavior |
| --- | --- |
| `list` | Lists benchmark cases, categories, profiles and declared parameters without running measurements. |
| `run` | Runs selected benchmarks for the current candidate. |
| `compare` | Measures a base Git revision and the current candidate, then evaluates regressions. |
| `interactive` | Opens Spectre.Console menus for choosing categories, filters, parameters and profiles. |
| `bdn` | Passes advanced arguments directly to BenchmarkDotNet. |

No arguments display help. They do not start an interactive session and do not run the complete suite.

Both `run` and `compare` require one of these selectors:

* `--all`
* One or more `--category <name>` options
* One or more `--filter <pattern>` options

Common options are:

* `--profile dry|quick|full`
* Repeatable `--category <name>`
* Repeatable `--filter <pattern>`
* Repeatable `--parameter <name=value>`
* `--artifacts <path>`
* `--export json|markdown|csv`

The comparison command also accepts:

* `--base-ref <git-ref>`, which defaults to `dev`
* `--threshold <percentage>`, which defaults to 10
* `--fail-on-inconclusive`
* An output path for the comparison report

Parameter arguments select only values declared by `[Params]` or `[ParamsSource]`. The CLI rejects unknown parameter names and undeclared values before starting BenchmarkDotNet. It prints the allowed values in the error message.

The `interactive` command builds the same selection model used by `run` and `compare`. Menus are a convenience layer rather than a separate execution path. Before starting a run, the application prints the equivalent noninteractive command so the selection can be repeated in CI or shared with another developer.

The `bdn` command is an escape hatch for BenchmarkDotNet options that the HeuristicLib CLI does not expose directly. Its arguments go to `BenchmarkSwitcher`. The project documents that raw passthrough bypasses some HeuristicLib safeguards and is intended for investigation rather than automated regression decisions.

## Measurement profiles

The suite defines three named profiles:

### `dry`

The `dry` profile verifies discovery, setup and execution with minimal iteration counts. It is suitable for validating a new benchmark or its parameters. Its results are never used for regression decisions.

### `quick`

The `quick` profile uses BenchmarkDotNet's short run settings. It supports local investigation and diagnosis during a pull request. A quick result can identify where to investigate but does not provide an authoritative gate.

### `full`

The `full` profile uses the normal statistically meaningful configuration. It is the only profile used for automated regression gating.

All profiles:

* Build and execute Release code
* Run without a debugger
* Use BenchmarkDotNet process isolation
* Record runtime and managed allocation information
* Record the exact runtime, operating system, processor and BenchmarkDotNet configuration

A complete `full` run should remain near 30 to 60 minutes per revision. Category and filter selection must make focused runs much shorter. If growth pushes the suite beyond that range, split slow or specialized workloads into explicit opt in categories rather than weakening every measurement.

## Categories and initial coverage

Categories are stable selection names. Filters provide more specific control within a category.

* `OperatorInfrastructure`
* `VectorGenotypes`
* `SelectionAndReplacement`
* `MultiObjective`
* `SymbolicExpressionGenotype`
* `SymbolicExpressionExecution`
* `AutomaticDifferentiation`
* `NumericParameterFitting`
* `AlgorithmRuns`
* `SymbolicRegression` as an umbrella category

The first implementation prefers a small set of representative benchmarks over broad shallow coverage.

### Operator infrastructure

Measure operator invocation, wrapping, dispatching and instrumentation overhead. Include paths where framework cost may dominate a lightweight operation. Compare direct operation cost with configured composition only when both measurements answer a concrete design question.

### Vector genotypes

Measure candidate creation, crossover and mutation for small, medium and large vector candidates. Cover representative real, integer, Boolean and permutation operations. Parameter sizes should reveal both fixed overhead and scaling behavior.

### Selection and replacement

Measure `TournamentSelector`, `RandomSelector`, `LinearRankSelector`, `ProportionalSelector`, Pareto crowding selection and mating constraints. Use population sizes that represent both ordinary local use and larger algorithm runs. Include replacement work where sorting, copying or objective comparison can dominate a generation.

### Multiobjective operations

Measure objective vector comparison, nondominated sorting and crowding work used by NSGA II. Parameterize population size and objective count. Keep candidate evaluation outside the measured boundary when the benchmark targets sorting or selection.

### Symbolic expression genotype

Measure immutable expression construction, tree editing, subtree crossover and mutation. Use expression shapes and sizes representative of evolved populations. Include cases with structural sharing so later changes cannot hide copying costs behind trivial trees.

### Symbolic expression execution

Measure expression compilation and repeated interpretation across different expression and data sizes. Separate one time compilation cost from repeated execution cost. Consume numeric results and keep data creation outside the measured operation.

### Automatic differentiation and numeric parameter fitting

Measure automatic differentiation evaluation, gradient calculation and reusable buffer paths. Measure numeric parameter fitting with representative expression sizes and observation counts. Retain the distinction between one fit and lower level repeated numerical operations.

### Algorithm runs

Measure short deterministic runs of `GeneticAlgorithm`, `EvolutionStrategy` and `NSGA2`. These cases detect interaction costs that isolated operator benchmarks miss. They use fixed seeds, bounded iteration counts and inexpensive deterministic problems so the measured result remains repeatable.

### Benchmark construction rules

Each benchmark:

* Uses deterministic input generation and explicit random seeds
* Prepares candidates, populations and data outside the measured method
* Consumes results so BenchmarkDotNet cannot remove measured work
* Parameterizes input sizes when scaling behavior matters
* Avoids file access, logging and unrelated setup inside the measured operation
* Includes a short comment when the measured boundary is not obvious

Benchmark cases should use public APIs when the measured behavior is public. An internal hot path may be measured through `InternalsVisibleTo` only when public composition would add unrelated work and the internal boundary is itself a deliberate performance concern.

## Revision comparison

The `compare` command measures the selected cases once for the base revision and once for the current candidate. Both runs use identical categories, filters, parameters, profiles and runtime settings. Base source is materialized in an isolated temporary directory so candidate files remain unchanged.

Results match on:

* Benchmark type and method
* Category
* Parameter names and values
* Runtime and job identity

The report includes:

* Base and candidate mean
* Base and candidate median
* Ratio and percentage change
* Error and standard deviation
* Sample counts
* Allocated bytes per operation
* Statistical comparison result
* Final status of improvement, unchanged, regression or inconclusive

### Regression decision

A runtime result is a confirmed regression only when all of these conditions hold:

1. The candidate exceeds the base by the practical threshold, which defaults to 10 percent.
2. A one sided statistical comparison supports a slowdown at the configured significance level.
3. Both results contain enough valid measurements.
4. Environment and benchmark identities are compatible.

The comparison uses the Mann Whitney test as supporting evidence because measurement samples should not be assumed to follow a normal distribution. The practical threshold prevents tiny but statistically detectable differences from failing a run.

Allocation changes appear in the report. Initial automated gating focuses on runtime. Exact zero allocation invariants that are deterministic remain scenario tests rather than depend on statistical benchmarking. Allocation gating may be added after the suite has enough history to define a practical absolute and relative policy.

### Exit codes and incompatible results

* Exit code `0` means no confirmed regression.
* Exit code `1` means at least one confirmed regression exceeded policy.
* Exit code `2` means configuration, build or benchmark execution failed.
* Inconclusive results produce warnings and exit code `0` unless `--fail-on-inconclusive` is set.

If a benchmark exists on only one revision or its parameters changed, the report marks it incompatible rather than comparing unrelated cases. If the base revision lacks compatible benchmark infrastructure, the command stops with an explanatory error. Historical comparison begins once the suite exists on both revisions.

The comparison always attempts to clean its temporary files after success or failure. A cleanup failure is reported and does not hide the benchmark outcome.

## Environmental controls and interpretation

Meaningful comparisons require the same machine under stable conditions:

* Use a fixed runtime and SDK.
* Keep the machine on a performance power profile.
* Avoid debugger attachment and competing CPU intensive processes.
* Avoid comparing results across different processor models.
* Record operating system and runtime updates.
* Confirm a borderline result with another run before treating it as a regression.
* Investigate thermal throttling, frequency scaling and background activity when many unrelated cases move together.

BenchmarkDotNet reduces common measurement errors but cannot make an unstable machine reliable. Absolute numbers from different machines belong in separate histories. The comparison report should reject known environment mismatches and display other differences prominently.

## CI integration

Adopt automated execution in stages:

1. Add the project to the main solution so restore and Release build verify benchmark code.
2. Confirm solution wide `dotnet test` never discovers or executes benchmarks.
3. Run focused benchmarks manually while cases and thresholds mature.
4. Add a manually triggered or scheduled job on a stable dedicated runner.
5. Store BenchmarkDotNet output and the comparison report as CI artifacts.
6. Make confirmed regressions fail that job after repeated runs establish that its environment is stable.
7. Consider making selected categories required for relevant pull requests. Do not make the full suite an unconditional hosted runner check.

The CI job calls the same CLI used locally. It does not contain another measurement implementation. Shared hosted runners may compile the benchmark project but do not provide authoritative regression results.

The dedicated runner should record its identity and relevant software versions with every result. Maintenance that changes the SDK, runtime, operating system or hardware starts a new baseline period rather than silently joining incompatible histories.

## IDE usage

Provide launch profiles for:

* Interactive selection
* Listing available benchmarks
* A representative quick category run

Contributor documentation explains how Rider and Visual Studio users start the console project and edit command arguments. Benchmarks do not appear in Test Explorer. IDE launches used for measurement must not attach a debugger.

## Relationship to existing tests

Functional assertions remain in the current unit and scenario projects. Retain the deterministic automatic differentiation allocation scenario as a correctness invariant. Do not replace it with a statistical benchmark.

Before adding a benchmark for an operator, rely on its existing functional tests for output correctness. Add missing functional coverage to the appropriate test project during implementation rather than asserting correctness inside timed benchmark methods.

Benchmarks may perform lightweight validation during global setup to prevent meaningless measurements. They should fail setup when test data or configuration is invalid. Validation does not belong inside the measured method.

## Implementation phases

1. Add the console project, BenchmarkDotNet configuration and Spectre.Console.Cli command structure.
2. Implement discovery, category filtering, profiles and artifact paths.
3. Add a small representative benchmark from several categories.
4. Add interactive selection and IDE launch profiles.
5. Implement base and candidate orchestration.
6. Implement result matching, statistical comparison, reports and exit codes.
7. Expand coverage to the agreed initial category set.
8. Add contributor documentation and VitePress navigation.
9. Validate on a stable machine before adding a failing CI policy.
10. Add the dedicated CI job only after local comparison is reliable.

Each phase should leave the project buildable. Comparison and CI work begins only after the benchmark cases and their command line selection are stable enough to produce matching identities.

## Acceptance criteria

The implementation is complete when:

* The main solution restores and builds the benchmark project in Release.
* `dotnet test` executes no benchmarks.
* `list` reports every benchmark, category and parameter.
* `run` rejects an accidental unfiltered full suite invocation.
* `dry` validates representative benchmarks from each implemented category.
* Interactive selection produces the same command configuration as noninteractive arguments.
* Identical selections produce compatible base and candidate result sets.
* A controlled slowdown beyond 10 percent produces exit code `1`.
* An improvement produces exit code `0`.
* Noisy or incompatible measurements are reported as inconclusive.
* Candidate source files remain untouched during comparison.
* Temporary comparison files are cleaned after success and failure.
* Reports contain enough environment data to reproduce the run.
* Full suite runtime stays within the documented target.
* Contributor documentation explains local, IDE and CI use.

## Research and examples

The design combines established benchmark methodology with repository specific constraints.

* [BenchmarkDotNet documentation](https://benchmarkdotnet.org/) describes process isolation, jobs, diagnostics, filtering, parameterization and exports. It supports the decision to use BenchmarkDotNet as the measurement engine rather than write timing loops.
* [BenchmarkDotNet VSTest integration](https://benchmarkdotnet.org/articles/features/vstest.html) describes IDE discovery and warns that the VSTest host or IDE may affect measurements. It supports the decision to keep benchmarks outside ordinary test discovery.
* [dotnet/performance](https://github.com/dotnet/performance) is a large .NET benchmark suite with filtered execution, standalone processes and result artifacts. It demonstrates that benchmark selection and automation can remain separate from unit tests.
* [Statistically Rigorous Java Performance Evaluation](https://doi.org/10.1145/1297027.1297033) by Georges, Buytaert and Eeckhout explains why warmup, repeated execution and statistical treatment matter for managed runtimes.
* [Rigorous Benchmarking in Reasonable Time](https://doi.org/10.1145/2464157.2464160) by Kalibera and Jones addresses experimental design, sources of variance and the cost of obtaining useful confidence.
* [Criterion.rs](https://bheisler.github.io/criterion.rs/book/) demonstrates saved baselines, filtering and statistically informed comparison in another library ecosystem.
* [Google Benchmark](https://google.github.io/benchmark/user_guide.html) demonstrates benchmark registration, filtering, repetitions and machine readable output in a mature native library ecosystem.

These sources inform measurement practice. HeuristicLib specific decisions include the category model, CLI commands, default 10 percent practical threshold, separation from VSTest and staged use of a dedicated CI machine.
