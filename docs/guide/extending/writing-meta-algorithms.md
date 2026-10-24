# Writing meta-algorithms

::: info Advanced extension
This page assumes you have read [Write an algorithm](/guide/extending/writing-algorithms) and [Configuration vs executors](/contributing/architecture/execution-nodes).
:::

A meta-algorithm coordinates child algorithms rather than operators. It is still an algorithm: it produces search states, it can be run directly, and it can itself be a child of another meta-algorithm. `CycleAlgorithm` and `PipelineAlgorithm` are the built-in examples.

## The same two parts

A meta-algorithm splits into a configuration record and an executor, exactly as an ordinary algorithm does. Child algorithms are configuration objects held by the record. The meta-algorithm's bound node calls their typed executors.

## Resolve child algorithms through the scope

::: warning Resolve child algorithms through the scope
Obtain a child algorithm's executor with `scope.Resolve(childAlgorithm)`. Creating and invoking its factory directly bypasses that child's advice and state reuse.

`ResolutionScope.Resolve` applies matching advice from the current aspects, including [analyzer observations](/guide/execution/observability-and-analysis). Bypassing a child's resolution means an analyzer observing that child misses its states.

The `HLib0001` analyzer reports direct child `CreateExecutorFactory` and `CreateExecutor` calls in preparation and construction hooks, binding lambdas and deferred execution methods. It does not prove correct state ownership or follow factories passed through arbitrary helper code. Keep child construction on the scope's resolution path.
:::

The same rule applies to operators. An operator's `CreateExecutor` resolves fixed children through its scope, takes persistent data from `scope.GetExecutionState(this, create)` and passes both to the executor it builds. A meta-algorithm with deferred children can retain its factory's construction scope for later child activation.

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

    public override ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>() => scope =>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>();

        return new Executor<TRunSearchSpace, TRunProblem>(typed.Resolve(First), typed.Resolve(Second));
    };

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

Four details carry the design:

- **Both children are resolved inside the returned factory.** Each bound node receives children with the requesting context's observations while their persistent state follows the scope's sharing rules.
- **The last state of the first stage becomes the initial state of the second.** Nothing converts between them, because both stages are declared over the same `TSearchState`.
- **Each stage gets its own random stream** through `random.Fork(index)`. Forking by a stable index keeps the stages independent and the run reproducible.
- **The stages are named by candidate and state only.** `IAlgorithm<TCandidate, TSearchState>` accepts any algorithm over that candidate producing that state, whatever search space or problem it was written for. The run supplies those as the method type arguments `TRunSearchSpace` and `TRunProblem` on `CreateExecutorFactory`. They are threaded into the nested executor class. Binding the types once with `scope.For<...>()` keeps the two `Resolve` calls free of type arguments.

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
var run = staged.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .AddAspect(wholeRun);
```

Two type arguments, not four. The search space and the problem are supplied by `CreateRun`, so the same `staged` value composes stages written for any real vector problem.

Because the children are resolved through the scope, either stage can also be observed on its own:

```csharp
var exploreQuality = explore.Iterations.TracePopulationQuality();

var wholeRun = staged.Iterations.TracePopulationQuality();
var run = staged.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .AddAspect(exploreQuality)
    .AddAspect(wholeRun);
```

`exploreQuality` then holds only the first stage's generations, and `wholeRun` holds every state the meta-algorithm yielded. Note that `explore` and `exploit` must be distinct objects for this to work, since observation sources are matched by reference.

## Child scopes

Use the factory's construction scope for fixed shared children. For deferred children, retain that scope on the bound node. When an activation needs a fresh child sharing domain, create a child scope and resolve against it:

```csharp
var childScope = scope.CreateChildScope();
var execution = childScope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm);
```

The scope itself has no type arguments, so it cannot infer the four the resolution needs and the call names them. Where several children are resolved against the same child scope, bind it once with `childScope.For<TCandidate, TSearchSpace, TProblem, TSearchState>()` and the individual `Resolve` calls need no type arguments at all. Both spellings reach the same resolution; the typed scope only saves the repetition.

A fresh child scope inherits its parent's aspects and any execution state already selected by an ancestor. It does not reset ancestor-owned state. Pipeline creates one fresh child scope for each stage activation.

Cycle's `ScopePolicy` selects one of three policies:

| Policy                      | Inner state                                                                         |
| --------------------------- | ----------------------------------------------------------------------------------- |
| `ChildScopePolicy.Fresh`    | New child-local state for each activation; the default.                             |
| `ChildScopePolicy.Retained` | Child-local state retained per inner algorithm reference within the prepared Cycle. |
| `ChildScopePolicy.Shared`   | The Cycle's own state, shared with sibling and direct uses of the same references.  |

```csharp
var cycle = first.CycleWith(second, maximumCycles: 3, scopePolicy: ChildScopePolicy.Retained);
```

Equal but distinct algorithm configurations retain separate child state. Repeated references retain the same child state. Common dependencies first selected in separate child states stay separate; dependencies already owned by an ancestor are reused in every mode.

For a meta-algorithm with the same activation policies, prepare one `ChildScopeSelector` from its `ChildScopePolicy` before returning the executor factory. Pass that selector to each constructed node and call `GetScopeFor(child, scope)` when activating a child. The selector retains child state across bindings without retaining an earlier aspect context. Keep invocation progress and the current construction scope on the constructed executor; persistent state must not capture the first requesting scope.

## Preserve run behavior

A meta-algorithm should:

1. Yield every state its children yield, unless it deliberately summarizes them.
2. Derive each child's random stream from the supplied one with a stable index.
3. Pass the cancellation token to every child stream.
4. Keep the configuration immutable, prepare persistent data once, and keep invocation progress and resources inside each iterator.

Read [Observability and analysis](/guide/execution/observability-and-analysis) for what observing a child algorithm means for the resulting series.
