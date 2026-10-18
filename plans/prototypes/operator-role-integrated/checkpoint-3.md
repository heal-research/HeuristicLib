# Checkpoint 3: counting cost, lifecycle hooks and duration

Date: 2026-10-16. Scope: cheap synchronous counting/duration paths in the isolated prototype. Production migration remains unstarted; this checkpoint does not complete Package 0.

## Findings

The cheap success-counting comparison passed before lifecycle implementation began. Local evidence records that earlier backend separately. Counting is also checked on the final lifecycle-enabled backend. Detailed measurements and host environment information are excluded from version control.

`ICallHooks<TToken>` now supplies `Enter`, `Success`, `Fault` and `Finally`. Each invocation creates a local `CallFrame` with its own token and failure ledger. The four existing built-in operation bindings and the external scalar binding implement the lifecycle profile alongside success-only hooks. The runtime execution interfaces retain their named methods, and the shared binding base forwards the child's control provider.

The adapter enters before invoking the child. If entry fails, no child or cleanup runs. After successful entry, cleanup runs exactly once. Success-callback failure does not invoke the fault callback. A lone failure retains its exception identity and original stack; multiple failures are aggregated in child, success/fault callback, cleanup order. Existing child aggregates remain intact. Completed state updates are not rolled back.

`DurationAdvice` uses an actual `long` timestamp token, an explicitly supplied `TimeProvider` and the existing atomic `DurationAccumulator`. It records elapsed time during cleanup, including failed and canceled work. It does not substitute a unit-token success hook for duration measurement. The success-only counter uses the existing atomic `CountAccumulator`.

## Correctness evidence

The combined run passed 198 runtime assertions in 22 groups: the previous 61 and 74 assertions plus 63 lifecycle/duration assertions. The positive compiler fixture and all five expected compiler failures still pass. Its detailed snapshot is retained locally.

The new assertions cover the child success/failure/cancellation matrix crossed with callback and cleanup failure, failed entry, original throw-site stacks, preservation of an existing aggregate, invocation-local tokens during nested calls, and success work counts. The full failure matrix exercises mutation; the other existing built-in and external adapters also have functional execution coverage.

Further fixtures cover lifecycle preparation across operation/state views, live controls, deterministic elapsed time for successful/failed/canceled calls, timestamp failures during entry and cleanup, and whether observer work lies inside the timing boundary according to advice order. The earlier ownership, ordering, contextual child and weak-lifetime fixtures still run.

## Performance evidence

The local BenchmarkDotNet 0.15.8 harness runs Release builds with three independent process launches per case, six warmup iterations and twelve measured iterations per process, targeting 100 ms per iteration. Returned results are consumed by the harness; counting also performs observable atomic updates. Default BenchmarkDotNet outlier handling remains in use. Local summaries include the statistics, every workload-result measurement with its process index, allocation diagnostics, environment, backend/benchmark hashes and raw-report hash.

Direct and shared paths use the same allocation-free identity leaf, inputs, RNG, atomic accumulators and `TimeProvider.System`. Direct duration uses the ordinary timestamp/`try`/`finally` wrapper. The three-layer comparison uses inner counting, duration and outer counting in the same order on both paths. Construction and resolution occur outside timed invocations.

The automated gate computes a conservative delta interval by subtracting the direct mean-confidence interval from the shared interval. Counting must have an upper delta bound at most 5 ns/call; duration at most 10 ns/call. Both direct and shared cheap paths must measure 0 B/call. The stack has an allocation gate; the plan specifies no separate stack latency threshold. Missing cases/processes, stale reports, failed measurements and uncertain latency fail the runner.

The local counting and duration comparisons passed the respective 5 ns and 10 ns delta budgets, with 0 B/call measured for both direct and shared paths. The three-layer comparison also measured 0 B/call on both paths. Detailed timings, distributions, hashes and host information remain in local summaries. These results apply to the measured paths and are not universal overhead guarantees.

## Reproduce

```powershell
pwsh -NoProfile -File ./plans/prototypes/operator-role-integrated/run.ps1
pwsh -NoProfile -File ./plans/prototypes/operator-role-integrated/run-benchmarks.ps1 -Category Count
pwsh -NoProfile -File ./plans/prototypes/operator-role-integrated/run-benchmarks.ps1 -Category Duration
pwsh -NoProfile -File ./plans/prototypes/operator-role-integrated/run-benchmarks.ps1 -Category Stack
```

Run these commands sequentially. Benchmark builds are generated under ignored `artifacts/operator-role-integrated/benchmarks` and reference the isolated backend. Package versions in production projects are not changed. Benchmark logs, full reports and summaries stay under ignored `artifacts/operator-role-integrated/checkpoint-3/<category>`. Rerunning benchmarks refreshes those local summaries. Historical evidence under the prototype's `evidence/` directory and `results*.txt` snapshots is also ignored.

Targeted C# whitespace and changed Markdown/JSON formatting are checked separately. Backend analyzer warnings remain in the command logs; this is not an analyzer-clean or full-solution-validation claim. Production sources and existing staged content are preserved.

## Remaining gates and next checkpoint

The numerical 32-candidate mutation/evaluation overhead limit of 2% has not been evaluated. Neither have around policies, retry, weighted choice/routing, candidate metrics, all nine operation families, preparation/rebinding cost, native hosting, real `EvolutionStrategy` adaptation or net source/API reduction. The full public protocol remains provisional and broad migration stays gated.

Next, add the around-policy profile with an external generic topology and explicit retry semantics, then measure its cheap path and extend numerical batch comparisons. Preserve the measured success/lifecycle paths and their failure semantics while doing so.
