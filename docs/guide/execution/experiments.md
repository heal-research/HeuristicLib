# Experiments

A parameter grid (`ParameterGrid`) defines variations of an algorithm prototype. Each combination of dimension values produces an algorithm configuration called a **parameter candidate**. Create an experiment from one or more grids to run their parameter candidates on the supplied problems. The experiment controls repetitions and random stream sharing. Each trial owns an independent algorithm run.

## Repeat a configuration

Import `HEAL.HeuristicLib.Experiments` for the experiment extensions.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ExperimentDocumentationSpecs.cs#experiment-repeat{csharp}

`RunToCompletion` and `RunToCompletionAsync` return an `ImmutableArray` of `(Trial, State)` pairs, with the final state from each run. Each trial exposes the problem instance and its zero-based `ProblemPosition`, `ParameterCandidateIndex` and `Repetition`. For a single repeated configuration on one problem, only `Repetition` varies. Record the seed, problem order, experiment settings and trial positions to reproduce a trial.

Summarize the distribution across repetitions.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ExperimentDocumentationSpecs.cs#experiment-summary{csharp}

The examples on this page run a genetic algorithm on the four-dimensional Rastrigin function with a population of 100, 100 generations, a blend crossover with `Alpha = 0.7` and a Gaussian mutator with rate and strength `0.2` and `0.15`. Twenty repetitions with seed `999` print:

```text
trials:  20
best:    0.0000
median:  0.0010
worst:   0.9965
reached below 0.01: 13 of 20
```

The spread is the finding. The best trial reaches the optimum to the displayed precision, while seven of twenty stop near `0.995`, the nearest local minimum, so quoting either end alone would misdescribe this configuration. Retain the seed and trial positions so you can reproduce an unusual run.

## Explore a parameter grid

Start with `AsParameterGrid()` and add dimensions with `VaryBy`. Pass a typed parameter descriptor to vary a declared property with compile-time type checking. Pass a name such as `"MutationRate"` to resolve a top-level parameter on the prototype, including inherited parameters. The name must identify exactly one parameter, and the values must fit its type without numeric or other conversions. Pass a typed lambda to configure several related settings together. Every form requires at least one value and throws `ArgumentException` for an empty value list.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ExperimentDocumentationSpecs.cs#experiment-grid{csharp}

The last dimension varies fastest. Dimensions are applied in the order they were added, so the lambda can derive the elite count from the population size chosen earlier. Here three populations, two mutation rates and five repetitions produce 30 trials. `trial.Settings` holds descriptor and value pairs in declaration order for the descriptor and name dimensions. Each descriptor appears once at its first declaration and reads its value from the resulting algorithm configuration. Lambda dimensions contribute no labels.

Group by typed settings and compare the distribution within each group. `BestOf` reads the best quality in a final population as in the repetition example.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ExperimentDocumentationSpecs.cs#experiment-grid-summary{csharp}

With the same seed, the grid prints:

```text
population  mutation   median      best
        50       0.1   0.9953    0.0060
        50       0.3   1.9899    0.9950
       100       0.1   0.9955    0.0013
       100       0.3   0.9950    0.0000
       200       0.1   0.0000    0.0000
       200       0.3   0.0000    0.0000
```

<script setup>
import grid from "../../.vitepress/figures/experiments-grid";
</script>

<ExperimentGrid v-bind="grid">

Every trial of the grid. The heatmap shows the median of each configuration and the dot plot its five trials. With a population of 200 both rates reach the optimum in most trials.

</ExperimentGrid>

Read the median column, not the best column, to judge typical performance. The best column describes a single trial: with a population of 100 and mutation rate `0.3`, one trial reaches the optimum while the median stops at `0.995`. The two settings interact. With a population of 50, raising the mutation rate to `0.3` doubles the median, while with a population of 200 both rates reach the displayed optimum. A high mutation rate needs a population large enough to hold onto good candidates while exploring, and judging either parameter on its own would miss that.

## Combine grids

A grid with no dimensions yields its prototype as its only parameter candidate. `Experiment.Create(grid)` sets up one repetition per parameter candidate and problem by default. To combine grids and set the repetition count, use `Experiment.Create([gridA, gridB], repetitions: 5)`. The experiment numbers parameter candidates continuously across the supplied grids. `Experiment.Create` requires at least one grid.

For configurations generated by a script, give each configuration its own grid with `Experiment.Create([.. configurations.Select(configuration => configuration.AsParameterGrid())])`. Equal parameter candidates at different indexes still produce separate trials. Grouping by parameter values pools those trials.

To inspect a grid, read its `Prototype` and `Dimensions`. Each dimension pairs a parameter with its values in declaration order, and a lambda dimension reports a null parameter. A parameter varied twice appears as two dimensions, while `trial.Settings` lists it once. An experiment lists its grids in supplied order through `Grids`.

## Compare problems and control random streams

Pass a problem list to `CreateRun`, for example `experiment.CreateRun([rastrigin10, rastrigin20], random, [best])`. For different problem types with the same candidate and search space, hold the problems as an `IReadOnlyList<IProblem<TCandidate, TSearchSpace>>`. Trials are ordered by problem, parameter candidate and repetition. `run.Trials.GroupBy(trial => (trial.ProblemPosition, trial.ParameterCandidateIndex))` groups repetitions for each parameter candidate and problem.

By default, all parameter candidates start from the same random stream for a given problem and repetition. Each trial still receives a separate generator, so its draws cannot advance another trial's generator. Choose independent starting streams with `experiment with { ShareRandomStreamsAcrossParameterCandidates = false }`. In that mode the fork also includes the parameter candidate index. With the same seed and settings, sequential and concurrent execution assign the same streams to the same trial positions.

## Control concurrency

Create an explicit run when you want to process trials as they finish. Import `HEAL.HeuristicLib.Execution` for `ExecutionConcurrency`.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ExperimentDocumentationSpecs.cs#experiment-concurrency{csharp}

`ExecutionConcurrency` states how much independent work may overlap, such as the trials of an experiment or the candidates of an operator batch. `Sequential()` finishes each item in input order before it starts the next. `Concurrent()` allows all items to overlap, and `Concurrent(max)` caps how many are active at once without guaranteeing any order. None of them reserves or creates dedicated threads. They only limit overlap, and the runtime decides which threads run the work. Limit concurrency when evaluation is CPU heavy or uses a constrained external resource.

## Run lifecycle

An experiment run is returned in `Ready` with one independent algorithm run per trial and starts once. Start the experiment to schedule all trials. Starting a trial run directly prevents scheduling through the experiment.

`CreateRun` decodes the parameter candidates and applies the supplied substitutions once per parameter candidate. It validates the resulting configurations on each problem and checks that each trial's run can hold the supplied analyzers. A decoding failure throws `InvalidOperationException` with the parameter candidate index. A substitution, validation or analysis check failure identifies the problem, parameter candidate and repetition in `ExperimentTrialException`. When several trials share the failed operation, the exception identifies the first affected trial. Pass `validate: false` to `CreateRun`, `RunStreaming`, `RunTrials`, `RunToCompletionAsync` or `RunToCompletion` to skip configuration validation. Executors are constructed when execution starts. During execution, a failed trial leaves the other trials available to finish.

An experiment moves from `Running` to `Completed`, `Failed` or `Canceled`. The experiment reserves its trials and resolves their executors before streaming starts. Each trial receives its cancellation token when its worker starts the iterator. Disposing the stream before completion stops scheduling, signals that token, waits for active trials to clean up and leaves the experiment `Stopped`. With `StartTrials`, cancellation settles active and queued trial tasks even if a trial throws during cleanup. These states are terminal. Create a new experiment run to execute again. Pause and resume apply to individual algorithm runs.

## Design a useful comparison

1. Choose a budget that reflects comparable work.
2. Define the seed schedule before looking at results.
3. Repeat every configuration on the same problem instances.
4. Keep all outcomes, including unsuccessful runs and runs with ordinary results.
5. Summarize effect size and spread alongside a central value.

Parameter search can overfit benchmark instances. Keep separate instances for final evaluation when conclusions will guide a real deployment.

## Add analysis

The analyzers of [Analysis of algorithm runs](/guide/execution/observability-and-analysis) work in an experiment. Pass them in the substitutions list of `experiment.CreateRun(problem, random, [analyzer])`. They apply after variation, so a default-selecting analyzer observes whichever evaluator each parameter candidate configures. `trial.Algorithm` is the configuration produced by the grid, `trial.Run.Algorithm` adds the substitutions, and `trial.Run.Result(analyzer)` reads the trial's own result.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-analyzed-grid{csharp}

The example repeats each parameter candidate five times and groups the trials by `ParameterCandidateIndex`. For each trial it reads the best objective found within the same budget of 16 evaluations, because a larger population spends more evaluations per generation and would otherwise be compared on more work. It then reports the median and the range over the repetitions, so a difference between parameter candidates can be judged against the spread between seeds.

The analyzer observes whole evaluated batches, so choose a budget that every compared configuration reaches at a batch boundary. Here each algorithm runs four generations, one evaluated batch each: a population of 4 reaches 16 evaluations after four batches and a population of 8 after two. With a budget of 12, the population of 4 would be read after three full batches while the population of 8 would be read after only one, since its second batch ends at 16. The point read at the budget is the last improvement retained up to it, so it may come from an earlier batch when the later ones found nothing better. The budget is small here so the example runs quickly. Choose one that matches the work you want to compare.

Grid variation creates new configurations, so a source named on the prototype matches only a parameter candidate that retains its target. A source named on the prototype root, such as `algorithm.Iterations`, matches the original in `algorithm.Repeat(n)`. A source named on the prototype or on a child the grid replaces fails placement when its target is absent. Use default selections on grids or name a child shared by the parameter candidates.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-experiment-setup{csharp}

Analysis already attached to a configuration remains available when a parameter candidate keeps the observing wrapper.
