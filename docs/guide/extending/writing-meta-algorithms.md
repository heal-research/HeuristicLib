# Writing meta-algorithms

::: info Advanced extension
This page assumes you have read [Write an algorithm](/guide/extending/writing-algorithms) and [Configuration vs execution instances](/contributing/architecture/execution-instances).
:::

A meta-algorithm coordinates child algorithms rather than operators. It is still an algorithm: it produces search states, it can be run directly, and it can itself be a child of another meta-algorithm. `CycleAlgorithm` and `PipelineAlgorithm` are the built-in examples.

## The same two parts

A meta-algorithm splits into a configuration record and an execution instance, exactly as an ordinary algorithm does. Child algorithms are configuration objects held by the record. Their execution instances belong to the meta-algorithm's instance.

## Resolve child algorithms through the registry

::: warning Never call CreateExecutionInstance on a child algorithm
Always obtain a child algorithm's execution instance with `registry.Resolve(childAlgorithm)`. Calling `childAlgorithm.CreateExecutionInstance(registry)` yourself compiles, runs, and produces correct search states — and silently breaks observation.

`ExecutionInstanceRegistry.Resolve` is what consults the replacements an [analyzer](/guide/execution/observability-and-analysis) installed for the run. Bypassing it means any analyzer anchored on that child algorithm, or on an operator inside it, records nothing at all. There is no error and no warning; the result list is simply empty.

The `HLib0001` analyzer does **not** catch this. It only inspects calls made inside a `CreateExecutionInstance` method, and a meta-algorithm that stores the registry and resolves its children lazily during the run is outside that window.
:::

The same rule applies to operators. Resolve every child once, while creating the instance, and pass the resolved instances to it.

## A two-stage meta-algorithm

This meta-algorithm runs one algorithm to completion, then hands its final search state to a second algorithm as the initial state. Both children share the candidate, search space, problem and search state types, so the state passes straight through.

```csharp
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

public sealed record TwoStageAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Algorithm<TwoStageAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>,
        TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> First { get; init; }

    public required IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Second { get; init; }

    public override IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(instanceRegistry.Resolve(First), instanceRegistry.Resolve(Second));

    private sealed class Instance(
        IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> first,
        IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> second)
        : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(
            TProblem problem,
            IRandomNumberGenerator random,
            TSearchState? initialState = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var state = initialState;

            await foreach (var nextState in first.RunStreamingAsync(problem, random.Fork(0), state, ct))
            {
                state = nextState;
                yield return nextState;
            }

            await foreach (var nextState in second.RunStreamingAsync(problem, random.Fork(1), state, ct))
            {
                yield return nextState;
            }
        }
    }
}
```

Three details carry the design:

- **Both children are resolved in `CreateExecutionInstance`**, not during the run. That is where the registry is available and where replacements are applied.
- **The last state of the first stage becomes the initial state of the second.** Nothing converts between them, because both stages are declared over the same `TSearchState`.
- **Each stage gets its own random stream** through `random.Fork(index)`. Forking by a stable index keeps the stages independent and the run reproducible.

Use it by naming the two stages:

```csharp
var explore = geneticAlgorithm with { MutationRate = 0.5, MaximumGenerations = 200 };
var exploit = geneticAlgorithm with { MutationRate = 0.05, MaximumGenerations = 300 };

var staged = new TwoStageAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem,
    PopulationState<RealVector>> { First = explore, Second = exploit };

var run = staged.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .TrackBestMedianWorst(out var wholeRun);
```

Because the children are resolved through the registry, either stage can also be observed on its own:

```csharp
var exploreQuality = Analyzer.BestMedianWorst(explore);

var run = staged.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .WithAnalyzer(exploreQuality)
    .TrackBestMedianWorst(out var wholeRun);
```

`exploreQuality` then holds only the first stage's generations, and `wholeRun` holds every state the meta-algorithm yielded. Note that `explore` and `exploit` must be distinct objects for this to work, since an anchor is matched by reference.

## Child registries

Resolve a child once per instance when the child should keep its state for the whole run. When a child needs a fresh execution instance per pass — as `CycleAlgorithm` does for each cycle — create a child registry and resolve against that:

```csharp
var childRegistry = registry.CreateChildRegistry();
var instance = childRegistry.Resolve(childAlgorithm);
```

A child registry inherits replacements from its parent, so observation still works. Calling `CreateExecutionInstance` on the child registry does not, for the reason in the warning above.

## Preserve run behavior

A meta-algorithm should:

1. Yield every state its children yield, unless it deliberately summarizes them.
2. Derive each child's random stream from the supplied one with a stable index.
3. Pass the cancellation token to every child stream.
4. Keep the configuration record immutable and store run data on the instance.

Read [Observability and analysis](/guide/execution/observability-and-analysis) for what anchoring on a child algorithm means for the resulting series.
