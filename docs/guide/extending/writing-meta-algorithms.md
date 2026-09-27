# Writing meta-algorithms

::: info Advanced extension
This page assumes you have read [Write an algorithm](/guide/extending/writing-algorithms) and [Configuration vs execution nodes](/contributing/architecture/execution-nodes).
:::

A meta-algorithm coordinates child algorithms rather than operators. It is still an algorithm: it produces search states, it can be run directly, and it can itself be a child of another meta-algorithm. `CycleAlgorithm` and `PipelineAlgorithm` are the built-in examples.

## The same two parts

A meta-algorithm splits into a configuration record and an execution node, exactly as an ordinary algorithm does. Child algorithms are configuration objects held by the record. Their execution nodes belong to the meta-algorithm's execution.

## Resolve child algorithms through the scope

::: warning Never call CreateExecutionInstance on a child algorithm
Always obtain a child algorithm's execution node with `scope.Resolve(childAlgorithm)`. Calling `childAlgorithm.CreateExecutionInstance(scope)` yourself compiles, runs, and produces correct search states — and silently breaks observation.

`ResolutionScope.Resolve` is what applies the wrappers an [analyzer](/guide/execution/observability-and-analysis) installed for the run. Bypassing it means an analyzer observing that child algorithm, or an operator inside it, records nothing at all. There is no error and no warning; the result list is simply empty.

The `HLib0001` analyzer does **not** catch this. It only inspects calls made inside a `CreateExecutionInstance` method, and a meta-algorithm that stores the scope and resolves its children lazily during the run is outside that window.
:::

The same rule applies to operators. Resolve every child once, while creating the execution, and pass the resolved executions to it.

## A two-stage meta-algorithm

This meta-algorithm runs one algorithm to completion, then hands its final search state to a second algorithm as the initial state. Both children share the candidate, search space, problem and search state types, so the state passes straight through.

```csharp
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

public sealed record TwoStageAlgorithm<TCandidate, TSearchState>
    : Algorithm<TwoStageAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TCandidate, TSearchState> First { get; init; }

    public required IAlgorithm<TCandidate, TSearchState> Second { get; init; }

    public override IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>
        CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>();

        return new Execution<TRunSearchSpace, TRunProblem>(typed.Resolve(First), typed.Resolve(Second));
    }

    private sealed class Execution<TSearchSpace, TProblem>(
        IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> first,
        IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> second)
        : AlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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

- **Both children are resolved in `CreateExecutionInstance`**, not during the run. That is where the scope is available and where wrappers are applied.
- **The last state of the first stage becomes the initial state of the second.** Nothing converts between them, because both stages are declared over the same `TSearchState`.
- **Each stage gets its own random stream** through `random.Fork(index)`. Forking by a stable index keeps the stages independent and the run reproducible.
- **The stages are named by candidate and state only.** `IAlgorithm<TCandidate, TSearchState>` accepts any algorithm over that candidate producing that state, whatever search space or problem it was written for. The run supplies those, which arrive as the method type arguments `TRunSearchSpace` and `TRunProblem` on `CreateExecutionInstance` and are threaded into the nested execution class. Binding the types once with `scope.For<...>()` is what keeps the two `Resolve` calls free of type arguments.

Use it by naming the two stages:

```csharp
using HEAL.HeuristicLib.Analysis; // TracePopulationQuality

var explore = geneticAlgorithm with { MutationRate = 0.5, MaximumGenerations = 200 };
var exploit = geneticAlgorithm with { MutationRate = 0.05, MaximumGenerations = 300 };

var staged = new TwoStageAlgorithm<RealVector, PopulationState<RealVector>>
{
    First = explore,
    Second = exploit
};

var wholeRun = staged.TracePopulationQuality();
var run = staged.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .Attach(wholeRun);
```

Two type arguments, not four. The search space and the problem are supplied by `CreateRun`, so the same `staged` value composes stages written for any real vector problem.

Because the children are resolved through the scope, either stage can also be observed on its own:

```csharp
var exploreQuality = explore.TracePopulationQuality();

var wholeRun = staged.TracePopulationQuality();
var run = staged.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .Attach(exploreQuality)
    .Attach(wholeRun);
```

`exploreQuality` then holds only the first stage's generations, and `wholeRun` holds every state the meta-algorithm yielded. Note that `explore` and `exploit` must be distinct objects for this to work, since observation sources are matched by reference.

## Child scopes

Resolve a child once per execution when the child should keep its state for the whole run. When a child needs a fresh execution node per pass — as `CycleAlgorithm` does for each cycle — create a child scope and resolve against that:

```csharp
var childScope = scope.CreateChildScope();
var execution = childScope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm);
```

The scope itself has no type arguments, so it cannot infer the four the resolution needs and the call names them. Where several children are resolved against the same child scope, bind it once with `childScope.For<TCandidate, TSearchSpace, TProblem, TSearchState>()` and the individual `Resolve` calls need no type arguments at all. Both spellings reach the same resolution; the typed scope only saves the repetition.

A child scope applies its parent's wrappers, so observation still works. Calling `CreateExecutionInstance` with the child scope does not, for the reason in the warning above.

## Preserve run behavior

A meta-algorithm should:

1. Yield every state its children yield, unless it deliberately summarizes them.
2. Derive each child's random stream from the supplied one with a stable index.
3. Pass the cancellation token to every child stream.
4. Keep the configuration record immutable and store run data on the execution.

Read [Observability and analysis](/guide/execution/observability-and-analysis) for what observing a child algorithm means for the resulting series.
