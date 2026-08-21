# Experiments

Stochastic algorithms should be judged across trials. HeuristicLib experiments turn repetitions and parameter grids into typed configurations with deterministic trial keys.

## Repeat a configuration

```csharp
using HEAL.HeuristicLib.Experiments;

var experiment = algorithm.Repeat(20);
var results = await experiment.CompleteAsync(
    problem,
    RandomNumberGenerator.Create(seed: 999));
```

`results` is an `ImmutableArray` of `(Trial, State)` pairs. Each trial carries its key, the exact algorithm configuration it ran, and the random fork path that produced it; the state is that run's final state. Repetitions use independent runs derived from the experiment's random source.

Summarize the distribution rather than quoting one run:

```csharp
double BestOf(PopulationState<RealVector> state) =>
    state.Population.EvaluatedCandidates
        .MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!
        .ObjectiveVector[0];

var qualities = results.Select(result => BestOf(result.State)).Order().ToArray();

Console.WriteLine($"trials:  {qualities.Length}");
Console.WriteLine($"best:    {qualities[0]:F4}");
Console.WriteLine($"median:  {qualities[qualities.Length / 2]:F4}");
Console.WriteLine($"worst:   {qualities[^1]:F4}");
Console.WriteLine($"reached below 0.01: {qualities.Count(quality => quality < 0.01)} of {qualities.Length}");
```

For a 100 generation genetic algorithm on the Rastrigin function:

```
trials:  20
best:    0.0000
median:  0.0009
worst:   2.6617
```

The spread is the finding. The best trial reaches the optimum and the worst is still at `2.66`, so quoting either number alone would misdescribe this configuration. Retain the seed and trial key so an unusual run can be reproduced exactly.

## Explore a parameter grid

```csharp
var grid = algorithm
    .AsGrid()
    .VaryBy(
        [50, 100, 200],
        (configuration, populationSize) => configuration with { PopulationSize = populationSize })
    .VaryBy(
        [0.1, 0.3],
        (configuration, mutationRate) => configuration with { MutationRate = mutationRate })
    .Repeat(5);

var gridResults = await grid.CompleteAsync(
    problem,
    RandomNumberGenerator.Create(seed: 999));
```

Each `VaryBy` takes the values to try and a function that applies one value to a configuration. Because algorithms are records, `with` produces a modified copy and the original is untouched. Three population sizes times two mutation rates times five repetitions is 30 trials.

Group by the configuration rather than by a formatted name. `Trial.Algorithm` is the exact configuration that ran, so the parameter values stay typed:

```csharp
Console.WriteLine("population  mutation   median      best");

foreach (var group in gridResults
    .GroupBy(result => (result.Trial.Algorithm.PopulationSize, result.Trial.Algorithm.MutationRate))
    .OrderBy(group => group.Key.PopulationSize)
    .ThenBy(group => group.Key.MutationRate))
{
    var values = group.Select(result => BestOf(result.State)).Order().ToArray();

    Console.WriteLine(
        $"{group.Key.PopulationSize,10}{group.Key.MutationRate,10:F1}" +
        $"{values[values.Length / 2],9:F4}{values[0],10:F4}");
}
```

```
population  mutation   median      best
        50       0.1   1.0024    0.0000
        50       0.3   1.9899    1.9899
       100       0.1   0.0019    0.0003
       100       0.3   0.9951    0.0001
       200       0.1   0.0030    0.0007
       200       0.3   0.0000    0.0000
```

Read the median column, not the best column. A population of 50 with a mutation rate of `0.3` never escapes `1.99`, while the same rate at population 200 solves the problem every time. The two settings interact: a high mutation rate needs a population large enough to hold onto good candidates while exploring, and judging either parameter on its own would have missed that.

## Control concurrency

Create an explicit run when you want to process trials as they finish:

```csharp
using HEAL.HeuristicLib.Execution;

var run = experiment.CreateRun(problem, RandomNumberGenerator.Create(seed: 789));
var tasks = run.StartTrials(ExecutionConcurrency.Concurrent(4));

await foreach (var completedTask in Task.WhenEach(tasks))
{
    var (trial, state) = await completedTask;
    Console.WriteLine($"Finished {trial.Key}");
}
```

Limit concurrency when evaluation is CPU heavy or uses a constrained external resource.

## Design a useful comparison

1. Choose a budget that reflects comparable work.
2. Define the seed schedule before looking at results.
3. Repeat every configuration on the same problem instances.
4. Keep all outcomes, not only successful or best runs.
5. Summarize effect size and spread alongside a central value.

Parameter search can overfit benchmark instances. Keep separate instances for final evaluation when conclusions will guide a real deployment.

## Add analysis

Analyzers can collect progress during each trial rather than only final states. Continue with [Observability and analysis](/guide/execution/observability-and-analysis).
