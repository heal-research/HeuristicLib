# Execution factory cost results, 2026-10-04

All authorized measurements completed successfully. Already-bound operator calls remain allocation-free and show no regression in the reversed-order check. Declaration, resolution and fresh-domain construction costs increase substantially in relative terms. Retained memory is bounded in the measured activation workloads, with a larger fixed footprint for Cycle reuse. **The user accepted the performance tradeoff for long-running optimization after reviewing absolute costs and the workload estimate below. Resolver optimization is not a migration prerequisite.** M3 lifecycle reconciliation and final foundation acceptance remain open independently.

## Evidence and environment

- Baseline: `edcf81d1c898c5e6b2ec0586b877783ba3b8c43a`; migrated: `85c1b24f66417d31f8217f265491a10d642cfda5`. No production source changed during measurement. Both Release harness builds succeeded.
- Identical harness SHA256 across all eight measured stages: `6AEAEC0CCBCF903B4766F0F25A3FB1C6108A5C69EEB07BABFCA3A3BCD4B0EDB3`.
- Intel Core i5-12600KF, 10 physical cores / 16 logical processors; Windows build 26200, x64; SDK 10.0.401; .NET 10.0.12; concurrent workstation GC. High performance power mode was active before and after execution.
- BenchmarkDotNet 0.15.8, default job with normal warmup/pilot/adaptive measurement, `--affinity 4 --allStats --stopOnFirstError`. Timed child processes use logical processor 2. Heap probes are untimed and use the host's normal affinity. The [official console options](https://benchmarkdotnet.org/articles/guides/console-args.html) describe the job controls; actual measurements and emitted configuration are preserved below.
- Sequential order: baseline full, migrated full, alternating baseline/migrated retention at 1,000 and 10,000 activations, migrated operation repeat, baseline operation repeat. No build/test work overlapped measurements, and no unrelated applications were closed or modified. Preflight observed about 0.77 busy core equivalents, including the sampling process; this is a local interactive-machine comparison, not a dedicated CI regression gate.
- Execution ran approximately 22:15–23:05 Europe/Vienna. All ten stages returned exit code zero: two builds, two full runs, four retention runs and two operation repeats. There are 57 baseline and 64 migrated full cases, six repeated operation cases and 60 retained-heap samples. Main-case statistics contain 12–100 retained measurements after BenchmarkDotNet's outlier handling. Several activation/algorithm cases reached high sample counts; preserve their variation rather than treating small differences as proven improvements.

The local [complete cost CSV](../artifacts/execution-factory/c4-2026-10-04/execution-factory-cost-results-2026-10-04.csv) contains every migrated case, its matched baseline where applicable, means, standard deviations, sample counts, reported confidence bounds, allocations and operation-repeat results. The local [retention CSV](../artifacts/execution-factory/c4-2026-10-04/execution-factory-retention-results-2026-10-04.csv) preserves all 60 samples and signed heap deltas. These datasets are ignored local artifacts rather than repository assets.

Raw full JSON, individual measurements, console reports, metadata, SDK/CPU/power snapshots, exact argument arrays and the orchestration/analysis scripts are preserved under [the local evidence directory](../artifacts/execution-factory/c4-2026-10-04/). Its `run-events.jsonl` records every stage's working directory, command arguments, timestamps and exit code. The temporary [harness](../artifacts/execution-factory/c4-2026-10-04/harness/README.md) and its build products were moved there after review; recorded commands retain their original measurement-time paths. The report is the durable repository record. Local artifacts are ignored by Git and are not distributed with a checkout. No Dry or harness-smoke results enter this comparison.

## Already-bound operation calls

Identity mutation returns its input list. Wrappers perform ordinary call-counting through a shared accumulator. Resolution, factories, RNG creation and scope lookup are outside the measured operation method. Values are nanoseconds per call; `±` denotes sample standard deviation, not the error of the mean.

| Counting wrappers | Baseline main | Migrated main | Baseline reversed-order repeat | Migrated reversed-order repeat | Allocated bytes/call, both revisions |
| --- | ---: | ---: | ---: | ---: | ---: |
| 0 | 0.844 ± 0.041 | 0.812 ± 0.034 | 0.947 ± 0.054 | 0.794 ± 0.087 | 0 |
| 1 | 3.268 ± 0.105 | 3.151 ± 0.056 | 3.636 ± 0.148 | 3.110 ± 0.106 | 0 |
| 4 | 17.141 ± 0.210 | 16.825 ± 0.156 | 19.322 ± 0.325 | 18.507 ± 0.236 | 0 |

The measured migration does not add a steady-state regression on this path. Between-run drift is visible, especially in baseline repeats; the sub-nanosecond leaf is particularly sensitive to harness overhead. These results establish an allocation-free path and no observed slowdown here, not a universal speedup claim.

## Declaration and resolution costs

Declaration snapshots include root scope creation and registration allocation, without preparing or invoking an operator.

| Registrations | Baseline µs | Migrated µs | Migrated/baseline | Baseline allocated B | Migrated allocated B |
| --- | ---: | ---: | ---: | ---: | ---: |
| 0 | 0.050 | 0.089 | 1.78× | 568 | 1,096 |
| 1 | 0.118 | 1.033 | 8.72× | 968 | 1,880 |
| 4 | 0.146 | 3.112 | 21.33× | 1,088 | 3,840 |
| 16 | 0.271 | 11.575 | 42.64× | 1,808 | 10,376 |

Across stateless/stateful/two-child composite graphs and 0/1/4 inherited wrappers:

| Resolution method | Migrated/baseline timing range | Allocation finding |
| --- | ---: | --- |
| First resolution in a fresh root | 3.15–9.15× | Baseline 728–4,088 B; migrated 1,880–10,312 B per invocation |
| Cached resolution | 2.21–7.97× | 0 B in both revisions |
| Fresh child scope with no added declarations | 5.87–14.17× | Baseline 216 B; migrated 1,040–1,600 B |
| Fresh child scope plus one direct observer | 5.23–9.26× | Baseline 2,288–3,512 B; migrated 3,128–5,784 B |

For scale, an unwrapped stateless leaf's first resolution rises from 95.54 ns / 728 B to 318.54 ns / 1,880 B. Its warm resolution rises from 4.55 ns to 24.53 ns, and a fresh child without declarations from 20.57 ns to 120.76 ns. Warm resolution is not part of the already-bound operator call above.

These are fresh-scope construction measurements on a warmed CLR, not process cold-start measurements. Direct observation of the stateful leaf intentionally differs: the baseline recreates its state, whereas the migration shares ancestor-prepared state. Those three rows are marked separately in the CSV. Ranges summarize matched parameter rows; they do not imply that all graph/wrapper combinations cost the same.

## Algorithm and activation workloads

The complete HillClimber/GA workloads use inexpensive integer candidates. Construction plus the short HillClimber run rises from 3.338 to 4.196 µs (+26%, allocations 6,032→8,024 B); the short GA run rises from 3.635 to 5.371 µs (+48%, 9,040→12,704 B). For the longer 16-step/generation workloads, construction-plus-run means are about unchanged (0.98× HillClimber, 1.02× GA), while allocation still increases by 1,992 B and 3,664 B respectively. Existing-execution invocation allocations are identical for all four combinations, and their measured means show no regression; the full CSV records variation.

Meta-algorithm activation means and allocations:

| Mode and boundary | Baseline µs | Migrated µs | Ratio | Baseline allocated B | Migrated allocated B |
| --- | ---: | ---: | ---: | ---: | ---: |
| Cycle reset, construct + activate | 6.785 | 11.655 | 1.72× | 23,040 | 31,848 |
| Cycle reset, existing execution | 7.481 | 11.064 | 1.48× | 22,056 | 29,800 |
| Cycle reuse, construct + activate | 6.573 | 9.081 | 1.38× | 16,664 | 21,832 |
| Cycle reuse, existing execution | 4.942 | 4.881 | 0.99× | 9,032 | 9,032 |
| Pipeline, construct + activate | 5.601 | 7.239 | 1.29× | 13,208 | 18,232 |
| Pipeline, existing execution | 5.129 | 6.835 | 1.33× | 12,328 | 16,200 |

The distinction matters: an existing Pipeline or reset-mode Cycle still constructs fresh child domains during invocation. Reusing a Cycle execution and its retained child domains amortizes that preparation cost. These small workloads expose construction overhead that longer optimization runs can hide.

## Retained memory

Median managed heap deltas with the owner alive, in bytes, across five samples for each setting:

| Mode | Baseline, 1,000 activations | Baseline, 10,000 | Migrated, 1,000 | Migrated, 10,000 |
| --- | ---: | ---: | ---: | ---: |
| Cycle reset | 1,096 | 3,576 | 2,112 | 2,720 |
| Cycle reuse | 3,536 | 4,096 | 10,896 | 11,456 |
| Pipeline | 1,512 | 1,512 | 2,616 | 2,616 |

All 60 samples reported collection of both the released root scope and execution. Cycle reuse retains about 7.2 KiB more fixed managed heap at 10,000 activations; Pipeline retains about 1.1 KiB more. The tenfold activation increase does not produce proportional retained growth in these workloads. Reset-mode heap deltas vary due to runtime/GC noise: baseline 10,000 samples span 1,656–11,504 B and migrated samples 2,672–5,560 B. After release, reuse/Pipeline medians are 48 B in both revisions; reset-mode residuals are noisy. Use the raw signed deltas and ranges, not an exact object-size attribution or a general leak-free claim.

Per-activation allocation at 10,000 activations averages approximately 22.26→29.94 kB for Cycle reset, 9.03→9.03 kB for Cycle reuse and 12.33→16.19 kB for Pipeline (decimal kB = 1,000 B). The raw CSV records bytes to avoid unit ambiguity. These include construction, candidate/search-state production, RNG and async traversal, not just resolver objects.

This probe has no changing observation contexts or dynamic-problem subscriptions. It does not close the M3 questions about epoch handlers, deterministic cache disposal or DynamicRacing failure-path contender disposal.

## New-capability and mechanism breakdown

Descendant-only observation of an ancestor-selected composite costs 3.635 µs / 3,976 B for the additional binding. It has no equivalent baseline measurement: the baseline reuses the composite and omits the descendant observer. The smoke checks establish that distinction; missing behavior cannot be counted as a performance advantage.

Factory preparation alone is 15.1 ns / 64 B for the stateless leaf, 23.2 ns / 144 B for the stateful leaf and 15.1 ns / 96 B for the composite. Direct binding of an already-prepared factory with a fresh standalone construction scope is about 92–95 ns / 1,064 B for leaves and 662 ns / 2,816 B for the composite. That diagnostic bypasses a composite preparation owner and is not interchangeable with contextual rebinding. The factory API's small preparation cost does not by itself explain the much larger declaration/resolver regressions.

## Performance acceptance and workload estimate

Decision, 2026-10-04: accept the increased resolution and activation costs for the intended long-running optimization workloads. This supersedes the initial recommendation to profile/optimize the resolver before continuing. Preserve the typed-factory design and its successful steady-state operation path; resolver optimization is optional follow-up rather than a migration prerequisite.

The current GeneticAlgorithm resolves its operators when its execution is constructed. Generations call the already-bound executions directly, so resolution/factory construction does not recur per candidate or generation. For scale, 5,000 generations with 500 candidates represent approximately 2.5 million evaluations:

| Assumed sequential evaluation cost per candidate | Evaluation work alone | Estimated share of 100 µs extra setup | Estimated share of 5 ms extra repeated activation |
| --- | ---: | ---: | ---: |
| 1 µs | 2.5 s | 0.004% | 0.2% |
| 10 µs | 25 s | 0.0004% | 0.02% |
| 100 µs | 250 s | 0.00004% | 0.002% |
| 1 ms | 2,500 s (about 42 min) | 0.000004% | 0.0002% |

This is an arithmetic estimate, not a new benchmark or a claim about typical GP evaluation time. It assumes sequential evaluation with roughly one population's worth of evaluations per generation; elitism, caching, termination and parallelism change the actual work. Selection, crossover, mutation, candidate construction and other algorithm work are additional. The 100-microsecond extra setup allowance is illustrative, not a measured upper bound; the measured short GA construction-plus-run difference is approximately 1.7 microseconds.

The repeated-activation estimate is separate from ordinary GA execution. The existing Pipeline benchmark invokes two children and adds approximately 1.7 microseconds; reset-mode Cycle invokes four children and adds approximately 3.6 microseconds. Dividing those workload differences gives roughly one additional microsecond per child activation for these small graphs, not an isolated activation-cost measurement. If a comparable fresh child were activated once per generation, 5,000 activations would add approximately five milliseconds. Cycle reuse showed no observed slowdown in the measured existing-execution workload.

Repeated very short algorithms, larger execution graphs or heavy observation registration may still make construction material. Roughly one extra microsecond is 1% of a 100-microsecond task and 0.1% of a one-millisecond task. Fresh activation also repeats the additional allocations; ordinary GA setup pays its extra allocation once. The acceptance is scoped to the intended workloads, not a universal guarantee for all callers.

C4 performance acceptance is closed. M3 lifecycle/resource-ownership reconciliation and final foundation review remain open; retention samples do not establish cleanup of dynamic subscriptions or deterministic cache disposal. No production repair, optimization or redesign is implemented by this report, and no further benchmark was run for the estimate.

Commit the plan updates and this report as the durable decision/evidence record. The one-off harness, detailed CSVs, raw output and build products need not become permanent repository infrastructure. Source/index/commit operations remain under the user's control; existing staged review content was preserved.
