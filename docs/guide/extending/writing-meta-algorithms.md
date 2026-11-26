# Writing meta-algorithms

::: info Advanced extension
This page assumes you have read [Write an algorithm](/guide/extending/writing-algorithms) and [Execution nodes](/contributing/architecture/execution-nodes).
:::

A meta-algorithm coordinates child algorithms rather than operators. It is still an algorithm: it produces search states, it can be run directly, and it can itself be a child of another meta-algorithm. `CycleAlgorithm` and `PipelineAlgorithm` are the built-in examples.

## Use the built-in meta-algorithms first

Two common schedules need no new type. `Then` creates a `PipelineAlgorithm` that runs each stage to completion and starts the next stage from the last state of the previous one. `CycleWith` creates a `CycleAlgorithm` that repeats its stages in order, each turn continuing from the last state:

```csharp
var explore = geneticAlgorithm with { MutationRate = 0.5, MaximumGenerations = 200 };
var exploit = geneticAlgorithm with { MutationRate = 0.05, MaximumGenerations = 300 };

var pipeline = explore.Then(exploit);
var cycle = explore.CycleWith(exploit, maximumCycles: 3);
```

An algorithm counts the generations of each call, so every turn of `cycle` runs up to 200 or 300 generations again. Every stage of a pipeline and every turn of a cycle also resolves its algorithm in a new child scope, so private executor data, such as the count of a terminator, starts fresh. Use [`Sharing`](/guide/fundamentals/configurations#sharing-executors) on the pipeline or cycle to keep one executor for a configuration across stages or turns, such as a cache or a terminator whose count should carry over. Both meta-algorithms yield every state their stages yield, so `pipeline.Iterations` observes the whole run and `explore.Iterations` observes only that stage.

Write your own meta-algorithm when the schedule is something else, such as choosing the next stage from the current state.

## The same two parts

A meta-algorithm splits into a configuration record and an executor, exactly as an ordinary algorithm does. Child algorithms are configuration objects held by the record. The meta-algorithm's bound node calls their typed executors.

## Resolve child algorithms through the scope

::: warning Resolve child algorithms through the scope
Obtain a child algorithm's executor with `scope.Resolve(childAlgorithm)`. Calling the child's `CreateExecutor` directly bypasses the scope's construction checks and sharing.

`ResolutionScope.Resolve` builds one executor per configuration/problem reference pair in a scope, checks it against the run and honors sharing. Bypassing a child's resolution builds a second executor whose private data the scope does not know, beside the one other requests receive.

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

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var first = scope.Resolve(First);
        var second = scope.Resolve(Second);
        return new Executor(first, second);
    }

    private sealed class Executor(
        IAlgorithmExecutor<TSearchState> first,
        IAlgorithmExecutor<TSearchState> second)
        : AlgorithmExecutor<TSearchState>
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(
            IRandomNumberGenerator random,
            TSearchState? initialState = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var state = initialState;

            await foreach (var nextState in first.RunStreamingAsync(random.Fork(0), state, ct))
            {
                state = nextState;
                yield return nextState;
            }

            await foreach (var nextState in second.RunStreamingAsync(random.Fork(1), state, ct))
            {
                yield return nextState;
            }
        }
    }
}
```

Five details matter:

- **Both children are declared as parameters.** Analysis and other substitutions reach only declared children, so an undeclared child could not be observed.
- **Both children are resolved in `CreateExecutor`, in the one scope it received.** A configuration that both stages hold by the same reference, such as the same algorithm in both positions or an operator the two stages have in common, is built once, and its executor serves both stages, private fields included. Equal but distinct configurations get separate executors. Each build receives children with the requesting context's observations, and [sharing](/guide/fundamentals/configurations#sharing-executors) decides what a child scope or another run reuses.
- **The last state of the first stage becomes the initial state of the second.** Nothing converts between them, because both stages are declared over the same `TSearchState`.
- **Each stage gets its own random stream** through `random.Fork(index)`. Forking by a stable index keeps the stages independent and the run reproducible.
- **The stages are named by candidate and state only.** `IAlgorithm<TCandidate, TSearchState>` accepts any algorithm over that candidate producing that state, whatever search space or problem it was written for. The run supplies those as the method type arguments `TSearchSpace` and `TProblem` on `CreateExecutor`. The typed scope keeps both `Resolve` calls free of type arguments. The resolved children retain their problem bindings, and the nested executor derives from `AlgorithmExecutor<TSearchState>`. A wrapper can derive from `WrappingAlgorithmExecutor<TSearchState>` and use its protected `ChildAlgorithm`. Pass any problem or search space the wrapper reads to its constructor.

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
var run = staged.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [wholeRun]);
```

`TwoStageAlgorithm` takes the candidate and search state types only. `CreateRun` supplies the search space and the problem, so the same `staged` value composes stages written for any real vector problem. `explore` and `exploit` are copies of one `geneticAlgorithm`, so they hold its creator, mutator and evaluator by the same references. This meta-algorithm resolves both stages in one scope, so the scope builds one executor of each for both stages. The built-in pipeline resolves each stage in a child scope instead and shares nothing unless told to. Stages of `TwoStageAlgorithm` that must not share an executor hold distinct configurations.

Because the children are declared, either stage can also be observed on its own. A trace holds data once the run has produced it, so complete the run before reading the results. Run this code from an async method, passing your host application's `cancellationToken`:

```csharp
var exploreQuality = explore.Iterations.TracePopulationQuality();

var wholeRun = staged.Iterations.TracePopulationQuality();
var run = staged.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [exploreQuality, wholeRun]);
await run.CompleteAsync(cancellationToken: cancellationToken);

var exploreTrace = run.Result(exploreQuality);
var wholeTrace = run.Result(wholeRun);
```

`exploreTrace` holds only the first stage's generations, and `wholeTrace` holds every state the meta-algorithm yielded. Note that `explore` and `exploit` must be distinct objects for this to work, since observation sources are matched by reference.

## Deferred children, turns and shared executors

Resolve fixed children through the scope received by `CreateExecutor`. Retain that scope on the executor when child construction is deferred. Create a child scope when an activation needs fresh private fields while keeping the same run results:

```csharp
var execution = scope.CreateChildScope().Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm, problem);
```

A typed scope created with `For<TCandidate, TSearchSpace, TProblem, TSearchState>(problem)` avoids repeating the type arguments. It is a view of the same scope, not a child scope. To bind a child to another subproblem, create a view with that problem and declare its signature on the child parameter with `ChildExecution`.

Pipeline resolves a fresh executor for each stage. Cycle resolves fresh executors for each turn and carries search state forward. `CycleAlgorithm.MaximumCycles` limits the number of cycles: `null` means unlimited, and a nonpositive value runs no cycles.

```csharp
var cycle = first.CycleWith(second, maximumCycles: 3).Sharing(first, second);
```

`Sharing` on the Cycle keeps one executor for a configuration across turns. [Configurations](/guide/fundamentals/configurations#sharing-executors) states which configurations it shares, who owns the executor and what a separately constructed Cycle starts with. A Cycle turn is one call of an inner algorithm; [Cycles and turns](/guide/execution/observability-and-analysis#cycles-and-turns) shows how to observe the calls of the inner algorithms.

### A new problem object for each turn

A scope keeps one executor for each configuration and problem object. A meta-algorithm that creates a new problem object for every turn therefore gets a new, empty executor for the child in every turn, while the scope keeps all the earlier ones. There are two remedies. Update one subproblem object in place, which avoids the new executors: its objective and search space stay fixed for the lifetime of the object, but its other instance data may change, so the child's executor is resolved once. The update reaches the child only when its executor reads the changed data from the problem at each call, not when it copied the data at construction. Or resolve each turn in a child scope, which still gives each turn a new executor but stops them from accumulating: a discarded child scope releases the executors it owns, together with the scope. An ancestor scope that [shares](/guide/fundamentals/configurations#sharing-executors) a configuration still owns that executor, and it keeps one for each problem object it is asked for, so a shared configuration resolved for a new problem object every turn accumulates executors there.

Use these imports in a C# project with implicit system usings enabled:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/MetaAlgorithmDocumentationSpecs.cs#meta-algorithm-turns-imports{csharp}

The example resolves a creator that remembers how many predefined candidates it has returned. `subproblem` and `anotherSubproblem` are two distinct problem objects over one search space, and `random` is the call's random number generator:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/MetaAlgorithmDocumentationSpecs.cs#new-problem-per-turn{csharp}

The second problem object restarts the creator with its first candidate, and the scope still holds the executor of the first problem, which continues with the second candidate when it is requested again. The child scope builds a third executor.

## Preserve run behavior

A meta-algorithm should:

1. Yield every state its children yield, unless it deliberately summarizes them.
2. Derive each child's random stream from the supplied one with a stable index.
3. Pass the cancellation token to every child stream.
4. Keep configurations immutable, private data in executor fields and invocation progress in iterators. Use explicit `Sharing` for intentional sharing.

Read [Analysis of algorithm runs](/guide/execution/observability-and-analysis) for what observing a child algorithm means for the resulting traces.
