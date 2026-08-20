# Search states

A search state is a snapshot yielded by an algorithm. It lets application code observe progress without reaching into mutable algorithm internals.

## Population states

Population based algorithms yield `PopulationState<TCandidate>`. Its population contains evaluated candidates, so you can inspect both solutions and objective vectors:

```csharp
var generation = 0;

await foreach (var state in algorithm.Stream(problem, random))
{
    generation++;
    if (generation % 10 != 0) continue;

    var best = state.Population.EvaluatedCandidates
        .MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!;

    Console.WriteLine($"Generation {generation,2}: {best.ObjectiveVector[0]:F4}");
}
```

```
Generation 10: 2.3984
Generation 20: 2.3984
Generation 30: 2.3984
Generation 40: 2.3984
Generation 50: 1.1636
```

The counter in this example belongs to the consumer. The state contains search data while the stream position tells you how many states have been observed.

The flat stretch between generations 10 and 40 is worth noticing. The best objective does not move for thirty generations and then improves, which is normal for a population based search: the population is still changing while its best member is not. Watching only the best value hides that, which is what the analyzers in [Observability and analysis](/guide/execution/observability-and-analysis) are for.

## Single candidate states

Local search algorithms can yield a state centered on one evaluated candidate rather than a population. Consume the concrete state type produced by the selected algorithm and inspect its candidate and objective vector.

## Use search states

Consume the state stream to:

- print progress in a console or UI
- collect convergence data
- inspect diversity or objective tradeoffs
- implement an external stopping decision
- preserve a final result independently from the algorithm configuration

Do not modify candidates held by an evaluated state. Variation should create new candidates that will receive new evaluations.

## Final state

`CompleteAsync` consumes the stream and returns its last state. It is convenient when only the result matters. Use `Stream` when each intermediate state has value.

Search states are not automatically durable checkpoints. If an application needs resume support, persist all configuration, problem data, random state and algorithm specific information required to recreate a valid run.
