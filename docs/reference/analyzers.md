# Analyzers and traces

The analysis building blocks of the core package. [Analysis of algorithm runs](/guide/execution/observability-and-analysis) shows how to place them and read their results.

## Observation sources

A source names a boundary to observe. Creating one installs nothing.

| Source                 | Observes                                            | Observation                                              |
| ---------------------- | --------------------------------------------------- | -------------------------------------------------------- |
| `operator.Invocations` | Each successful call of the operator                | The operation's record, such as `MutationObservation<T>` |
| `algorithm.Iterations` | Each state the algorithm yields, after interceptors | `AlgorithmObservation<T, TState>`                        |
| `algorithm.Calls`      | The start and outcome of each executor call         | `AlgorithmCallObservation<T, TState>`                    |

Every operation offers `Invocations`, including operations you [define](/extend/defining-operations).

## Analyzers

Pass an analyzer to `CreateRun` and read its result with `run.Result(analyzer)`.

| Analyzer                                                                            | Records                                                            | Result                           |
| ----------------------------------------------------------------------------------- | ------------------------------------------------------------------ | -------------------------------- |
| `Analyzer.BestSoFar<T>()`                                                           | The running best candidate at the innermost evaluators             | `BestSoFarCurve<T>`              |
| `Analyzer.BestSoFar(evaluator)`                                                     | The running best candidate at one evaluator                        | `BestSoFarCurve<T>`              |
| `algorithm.Iterations.TracePopulationQuality()`                                     | Best, median and worst objective vector of each yielded population | `Trace<BestMedianWorst>`         |
| `algorithm.Iterations.TracePopulationCandidates()`                                  | Best, median and worst candidate of each yielded population        | `Trace<BestMedianWorstEntry<T>>` |
| `source.Trace(value)` or `Analyzer.Trace(sources, value)`                           | One value per observation                                          | `Trace<TValue>`                  |
| `source.Aggregate(create, update)` or `Analyzer.Aggregate(sources, create, update)` | One state updated by every observation                             | The state you create             |

Each trace and best-so-far analyzer takes optional `clocks` and `retention` arguments. A best-so-far curve point carries its `Evaluations`, `Value` and `Candidate`. A trace offers `SampleCount`, `Latest`, `Snapshot()` and `By(clock)`.

## Clocks

A clock gives each recorded entry its time. Pass clocks in the `clocks` argument of a trace.

| Clock            | Reads                                           |
| ---------------- | ----------------------------------------------- |
| `counter.Clock`  | The count of a `Counter`                        |
| `duration.Clock` | The time of a `Duration`                        |
| `Clock.Elapsed`  | The time the run spends advancing its algorithm |

## Counting and timing

Counters and durations record nothing until a placement updates them. Pass the placement to `CreateRun` or `Substitute`, after the analyzers that read it.

| Placement                            | Updates                                      |
| ------------------------------------ | -------------------------------------------- |
| `source.Counting(counter)`           | Counts the observations of a source          |
| `source.CountingCandidates(counter)` | Counts the candidates of an operator's calls |
| `source.Timing(duration)`            | Adds the time of an operator's calls         |
| `algorithm.Timed(duration)`          | Adds the time of an algorithm's calls        |
| `source.Observing(callback)`         | Passes each observation to a callback        |

## Retention

Retention decides which computed values a trace keeps.

| Retention                           | Keeps                                                                   |
| ----------------------------------- | ----------------------------------------------------------------------- |
| `TraceRetention.EveryObservation()` | Every value, the default for traces                                     |
| `TraceRetention.OnChange()`         | A value that differs from the previous one, the default for best-so-far |
| `TraceRetention.EveryNth(n)`        | Every n-th value                                                        |
| `TraceRetention.LatestOnly()`       | Only the latest value                                                   |
