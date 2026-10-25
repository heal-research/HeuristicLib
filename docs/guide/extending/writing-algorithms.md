# Writing algorithms

Create an algorithm only when the state transition or control flow is new. Put domain evaluation in a problem and local search policies in operators whenever those existing extension points are sufficient.

## Configuration and execution

An algorithm has two parts:

1. An immutable configuration record that holds operators and settings.
2. An executor bound to resolved operators and the interceptor, built for every resolution, and scoped state that those executors share.

Each call to `Stream` or `CompleteAsync` starts an independent root execution. Every resolution of the algorithm builds a new executor with its own resolved children; data those executors share is scoped state, which the scope creates once per owning scope.

## Implement an iterative algorithm

Derive from `IterativeAlgorithm` when one previous state produces one next state. This example creates and evaluates one candidate per step:

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

public sealed record SingleCreateAlgorithm
    : IterativeAlgorithm<SingleCreateAlgorithm, RealVector, SingleSolutionState<RealVector>>
{
    public required IOperator<Creation, RealVector> Creator { get; init; }

    public IOperator<Evaluation, RealVector> Evaluator { get; init; } = new ProblemEvaluator<RealVector>();

    protected override IterativeAlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope)
    {
        var interceptor = scope.ResolveOptional(Interceptor);
        var creator = scope.Resolve(Creator);
        var evaluator = scope.Resolve(Evaluator);
        return new Executor<TSearchSpace, TProblem>(interceptor, creator, evaluator);
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        IInterceptionExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>>? interceptor,
        ICreationExecutor<RealVector, TSearchSpace, TProblem> creator,
        IEvaluationExecutor<RealVector, TSearchSpace, TProblem> evaluator)
        : IterativeAlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>>(interceptor)
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace>
    {
        protected override SingleSolutionState<RealVector> ExecuteStep(
            SingleSolutionState<RealVector>? previousState,
            TProblem problem,
            IRandomNumberGenerator random)
        {
            var candidate = creator.Create(1, random, problem.SearchSpace, problem)[0];
            var objective = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];

            return SingleSolutionState.From(candidate, objective);
        }
    }
}
```

### Why the configuration names only the candidate

`SingleCreateAlgorithm` never reads a member of a particular problem. It hands the search space and the problem straight to its operators, so it does not need to name either, and the base above takes three type arguments: the algorithm's own type, the candidate, and the state it produces.

Operator slots, including terminators, name their operation and candidate. An interceptor slot also names the search state its operation returns, as the inherited `IOperator<Interception<SingleSolutionState<RealVector>>, RealVector>? Interceptor` does. `IOperator<Creation, RealVector>` accepts any creator written for real vectors, including one written against `BoundedRealVectorSearchSpace`, because the run supplies the space when the executor is created.

The run's search space and problem arrive as method type arguments on `CreateExecutor`, together with a scope typed to them. The nested executor class is generic in them and carries the matching constraints. Configurations omit the run types from operator slots.

Resolution calls this protected hook for every build. Return an executor derived from `IterativeAlgorithmExecutor`; this base's hook declares only `IAlgorithmExecutor`, so the compiler does not check it. Resolve the optional `Interceptor` alongside the other configured children and pass it to the executor constructor. The executor base applies it in the iteration loop, before checking terminal state and yielding; its result becomes the next iteration's previous state. `ExecuteStep`, the interceptor and the protected `IsTerminalState` hook receive the same per-iteration random number generator, so an algorithm that owns a terminator passes that generator on.

### When to name the search space and problem instead

An algorithm that reads something only one problem has — a distance matrix, a dataset, a domain-specific bound — cannot work that way, and derives from the five-argument base:

```csharp
public sealed record MatrixAwareAlgorithm
    : IterativeAlgorithm<
        MatrixAwareAlgorithm,
        Permutation,
        PermutationSearchSpace,
        TravelingSalesmanProblem,
        SingleSolutionState<Permutation>>
```

Its protected `CreateExecutor` takes no method type arguments and receives a scope typed to the declared contracts, so its executor class can be nongeneric. The hook returns `IterativeAlgorithmExecutor<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, SingleSolutionState<Permutation>>`. Operator slots still omit the run types. The base rejects a run whose search space or problem does not convert to the declared contracts before it calls the hook. Choose this base when the algorithm reads those contracts itself.

Use `.TerminatedAfterIterations(count)` or another terminator when the algorithm does not stop itself.

## Share state and bind children

Resolve the optional interceptor and every other configured child operator through the scope that `CreateExecutor` receives. Retain the resolved children on the executor. Do not call operator configurations directly from `ExecuteStep` or resolve children on every iteration.

Take data that every build shares, such as a counter or a cache, from `scope.GetScopedState(this, create)`. The scope creates it on the first request and returns the same object to every later build in the owning scope. Keep iteration position, previous search state and per-invocation values in the operation or iterator. A paused iterator retains its original children and interceptor when another context builds an executor. Never mutate the configuration record.

## Preserve run behavior

A custom algorithm should:

1. Yield a state after each meaningful step.
2. Accept the supplied random source and derive stable child streams when work is independent.
3. Check cancellation through the inherited run loop.
4. Keep simultaneous runs independent.
5. Work with interceptors when it derives from `IterativeAlgorithm`.

Read [Running algorithms](/guide/execution/running-algorithms) for the consumer model and [Configuration vs executors](/contributing/architecture/execution-nodes) for the internal ownership rules.

To write an algorithm that coordinates other algorithms rather than operators, continue with [Write a meta-algorithm](/guide/extending/writing-meta-algorithms).
