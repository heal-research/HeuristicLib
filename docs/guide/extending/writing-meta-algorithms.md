# Writing meta-algorithms

::: info Advanced extension
This page assumes you have read [Write an algorithm](/guide/extending/writing-algorithms) and [Configuration vs executors](/contributing/architecture/execution-nodes).
:::

A meta-algorithm coordinates child algorithms rather than operators. It is still an algorithm: it produces search states, it can be run directly, and it can itself be a child of another meta-algorithm. `CycleAlgorithm` and `PipelineAlgorithm` are the built-in examples.

## The same two parts

A meta-algorithm splits into a configuration record and an executor, exactly as an ordinary algorithm does. Child algorithms are configuration objects held by the record. The meta-algorithm's bound node calls their typed executors.

## Resolve child algorithms through the scope

::: warning Resolve child algorithms through the scope
Obtain a child algorithm's executor with `scope.Resolve(childAlgorithm)`. Calling the child's `CreateExecutor` directly bypasses the scope's construction checks and sharing.

`ResolutionScope.Resolve` builds one executor per configuration in a scope, checks it against the run and honors sharing. Bypassing a child's resolution builds a second executor whose private data the scope does not know, beside the one other requests receive.

The `HLib0001` analyzer reports direct child `CreateExecutor` calls in construction hooks, custom resolvers and deferred execution methods. It does not prove correct state ownership or follow configurations passed through arbitrary helper code. Keep child construction on the scope's resolution path.
:::

The same rule applies to operators. An operator's `CreateExecutor` resolves fixed children through its scope, initializes private executor fields and passes resolved children to the executor it builds. A meta-algorithm with deferred children can retain the scope its `CreateExecutor` receives for later child activation.

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

    public static Parameter<TwoStageAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> FirstParameter { get; } =
        new(nameof(First), static algorithm => algorithm.First, static (algorithm, value) => algorithm with { First = value });

    public static Parameter<TwoStageAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> SecondParameter { get; } =
        new(nameof(Second), static algorithm => algorithm.Second, static (algorithm, value) => algorithm with { Second = value });

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), FirstParameter, SecondParameter];

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var first = scope.Resolve(First);
        var second = scope.Resolve(Second);
        return new Executor<TSearchSpace, TProblem>(first, second);
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> first,
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> second)
        : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
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

Five details carry the design:

- **Both children are declared as parameters.** Analysis and other substitutions reach only declared children, so an undeclared child could not be observed.
- **Both children are resolved in `CreateExecutor`.** Each build receives children with the requesting context's observations, with independent private fields and any explicitly shared resources.
- **The last state of the first stage becomes the initial state of the second.** Nothing converts between them, because both stages are declared over the same `TSearchState`.
- **Each stage gets its own random stream** through `random.Fork(index)`. Forking by a stable index keeps the stages independent and the run reproducible.
- **The stages are named by candidate and state only.** `IAlgorithm<TCandidate, TSearchState>` accepts any algorithm over that candidate producing that state, whatever search space or problem it was written for. The run supplies those as the method type arguments `TSearchSpace` and `TProblem` on `CreateExecutor`. They are threaded into the nested executor class. The scope the hook receives is typed to them, which keeps the two `Resolve` calls free of type arguments.

Use it by naming the two stages:

```csharp
using HEAL.HeuristicLib.Analysis; // TracePopulationQuality
using HEAL.HeuristicLib.Inspection; // Iterations

var explore = geneticAlgorithm with { MutationRate = 0.5, MaximumGenerations = 200 };
var exploit = geneticAlgorithm with { MutationRate = 0.05, MaximumGenerations = 300 };

var staged = new TwoStageAlgorithm<RealVector, PopulationState<RealVector>>
{
    First = explore,
    Second = exploit
};

var wholeRun = staged.Iterations.TracePopulationQuality();
var run = staged.AnalyzedBy(wholeRun).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
```

Two type arguments, not four. The search space and the problem are supplied by `CreateRun`, so the same `staged` value composes stages written for any real vector problem.

Because the children are declared, either stage can also be observed on its own:

```csharp
var exploreQuality = explore.Iterations.TracePopulationQuality();

var wholeRun = staged.Iterations.TracePopulationQuality();
var run = staged.AnalyzedBy(exploreQuality, wholeRun).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
```

`run.Result(exploreQuality)` then holds only the first stage's generations, and `run.Result(wholeRun)` holds every state the meta-algorithm yielded. Note that `explore` and `exploit` must be distinct objects for this to work, since observation sources are matched by reference.

## Deferred children and shared executors

Resolve fixed children through the scope received by `CreateExecutor`. Retain that scope on the executor when child construction is deferred. Each call constructs fresh private fields while using the same run results:

```csharp
var execution = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm);
```

A typed scope created with `For<TCandidate, TSearchSpace, TProblem, TSearchState>()` avoids repeating the type arguments.

Pipeline resolves a fresh executor for each stage. Cycle resolves fresh executors for each turn and carries search state forward. Share a configuration on the Cycle to keep one executor for it across turns:

```csharp
var cycle = first.CycleWith(second, maximumCycles: 3).Sharing(first, second);
```

Sharing is local to one executor of the sharing node and uses configuration reference identity, so `Cycle(A, A)` shared on A uses one A executor. Equal but distinct configurations keep separate executors. A shared executor preserves its private fields and resolved children. A separately constructed Cycle starts independently. Share any configuration the same way, for example a cached evaluator used by both algorithms, to keep only that part across turns while terminators and budgets restart.

A Cycle turn is one call of an inner algorithm. Observe the inner algorithms' `Calls` to record starts and outcomes, including a turn that yields no state; see [Cycles and turns](/guide/execution/observability-and-analysis#cycles-and-turns).

## Preserve run behavior

A meta-algorithm should:

1. Yield every state its children yield, unless it deliberately summarizes them.
2. Derive each child's random stream from the supplied one with a stable index.
3. Pass the cancellation token to every child stream.
4. Keep configurations immutable, private data in executor fields and invocation progress in iterators. Use explicit `Sharing` for intentional sharing.

Read [Observability and analysis](/guide/execution/observability-and-analysis) for what observing a child algorithm means for the resulting series.
