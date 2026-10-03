# Writing algorithms

Create an algorithm only when the state transition or control flow is new. Put domain evaluation in a problem and local search policies in operators whenever those existing extension points are sufficient.

## Configuration and execution

An algorithm has two parts:

1. An immutable configuration record that holds operators and settings.
2. Prepared execution state and an execution node bound to resolved operators and interceptors.

Each call to `Stream` or `CompleteAsync` starts an independent root execution. Preparation creates persistent state once for that logical execution; contextual bindings share it while receiving their own resolved children.

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
    public required ICreator<RealVector> Creator { get; init; }

    public IEvaluator<RealVector> Evaluator { get; init; } = new ProblemEvaluator<RealVector>();

    protected override ExecutionFactory<IterativeAlgorithmExecution<RealVector, TRunSearchSpace, TRunProblem, SingleSolutionState<RealVector>>> CreateIterationFactory<TRunSearchSpace, TRunProblem>() =>
        scope =>
        {
            var typed = scope.For<RealVector, TRunSearchSpace, TRunProblem, SingleSolutionState<RealVector>>();

            return new Execution<TRunSearchSpace, TRunProblem>(
                typed.ResolveOptional(Interceptor),
                typed.Resolve(Creator),
                typed.Resolve(Evaluator));
        };

    private sealed class Execution<TSearchSpace, TProblem>(
        IInterceptorExecution<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>>? interceptor,
        ICreatorExecution<RealVector, TSearchSpace, TProblem> creator,
        IEvaluatorExecution<RealVector, TSearchSpace, TProblem> evaluator)
        : IterativeAlgorithmExecution<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>>(interceptor)
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

That choice is what keeps the operator slots at one type argument each. `ICreator<RealVector>` accepts any creator written for real vectors, including one written against `BoundedRealVectorSearchSpace`, because the run supplies the space when the execution node is created.

The run's search space and problem arrive as method type arguments on `CreateIterationFactory`. The nested execution class is generic in them and carries the matching constraints. Configurations retain candidate-only operator roles.

The public `CreateExecutionFactory` forwards to this protected preparation hook without a construction scope. The hook returns an ordinary `ExecutionFactory` whose result must derive from `IterativeAlgorithmExecution`. Inside that factory, resolve the optional `Interceptor` alongside the other configured children and pass it to the execution constructor. The execution base applies it in the iteration loop, before checking terminal state and yielding; its result becomes the next iteration's previous state.

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

Its protected `CreateIterationFactory` takes no method type arguments and its execution class can be nongeneric. The hook returns `ExecutionFactory<IterativeAlgorithmExecution<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, SingleSolutionState<Permutation>>>`. Operator slots still use candidate-only roles. The public factory checks run compatibility with the declared search-space and problem contracts before preparation. Choose this base when the algorithm reads those contracts itself.

Use `.TerminatedAfterIterations(count)` or another terminator when the algorithm does not stop itself.

## Prepare state and bind children

Resolve the optional interceptor and every other configured child operator through the construction scope supplied to the prepared execution factory. Retain the resolved children on that execution node. Do not call operator configurations directly from `ExecuteStep` or resolve children on every iteration.

Allocate persistent counters and helper data in `CreateIterationFactory`, before returning its delegate, and capture that data for each binding. Keep iteration position, previous search state and per-invocation values in the operation or iterator. A paused iterator retains its original children and interceptor when another context binds an execution. Never mutate the configuration record.

## Preserve run behavior

A custom algorithm should:

1. Yield a state after each meaningful step.
2. Accept the supplied random source and derive stable child streams when work is independent.
3. Check cancellation through the inherited run loop.
4. Keep simultaneous runs independent.
5. Work with interceptors when it derives from `IterativeAlgorithm`.

Read [Running algorithms](/guide/execution/running-algorithms) for the consumer model and [Configuration vs execution nodes](/contributing/architecture/execution-nodes) for the internal ownership rules.

To write an algorithm that coordinates other algorithms rather than operators, continue with [Write a meta-algorithm](/guide/extending/writing-meta-algorithms).
