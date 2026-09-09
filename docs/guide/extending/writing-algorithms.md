# Writing algorithms

Create an algorithm only when the state transition or control flow is new. Put domain evaluation in a problem and local search policies in operators whenever those existing extension points are sufficient.

## Configuration and execution

An algorithm has two parts:

1. An immutable configuration record that holds operators and settings.
2. An execution instance that owns resolved operators and mutable run data.

Each call to `Stream` or `CompleteAsync` creates a new execution instance. Reusing one configuration therefore starts independent runs.

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

    protected override IterativeAlgorithmInstance<RealVector, TRunSearchSpace, TRunProblem, SingleSolutionState<RealVector>>
        CreateExecutionInstance<TRunSearchSpace, TRunProblem>(
            ExecutionInstanceRegistry instanceRegistry,
            IInterceptorInstance<RealVector, TRunSearchSpace, TRunProblem, SingleSolutionState<RealVector>>? resolvedInterceptor)
    {
        var resolver = instanceRegistry.For<RealVector, TRunSearchSpace, TRunProblem>();

        return new Instance<TRunSearchSpace, TRunProblem>(
            resolvedInterceptor,
            resolver.Resolve(Creator),
            resolver.Resolve(Evaluator));
    }

    private sealed class Instance<TSearchSpace, TProblem>(
        IInterceptorInstance<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>>? interceptor,
        ICreatorInstance<RealVector, TSearchSpace, TProblem> creator,
        IEvaluatorInstance<RealVector, TSearchSpace, TProblem> evaluator)
        : IterativeAlgorithmInstance<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>>(interceptor)
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

That choice is what keeps the operator slots at one type argument each. `ICreator<RealVector>` accepts any creator written for real vectors, including one written against `BoundedRealVectorSearchSpace`, because the run supplies the space when the execution instance is created.

The cost is visible in the example and worth naming: the run's search space and problem arrive as *method* type arguments on `CreateExecutionInstance`, so the nested instance class is generic in them and carries two constraints. That is the whole price, it is paid once by the algorithm's author, and it is paid nowhere by anyone configuring or holding the algorithm.

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

Its `CreateExecutionInstance` takes no type arguments and its instance class is not generic, so authoring is simpler. In exchange, every mention of the configuration names five types, its operator slots name three each, and it runs over exactly one problem type; anything else is refused when the execution graph is built. Choose this base when the algorithm genuinely reads the problem, not to avoid the generic instance class.

Use `.TerminatedAfterIterations(count)` or another terminator when the algorithm does not stop itself.

## Resolve operators once

Resolve every configured child operator through `ExecutionInstanceRegistry` while creating the algorithm instance. Do not call operator configurations directly from `ExecuteStep` and do not create new child instances for every iteration.

Store counters and other changing values on the nested execution instance. Never mutate the configuration record.

## Preserve run behavior

A custom algorithm should:

1. Yield a state after each meaningful step.
2. Accept the supplied random source and derive stable child streams when work is independent.
3. Check cancellation through the inherited run loop.
4. Keep simultaneous runs independent.
5. Work with interceptors when it derives from `IterativeAlgorithm`.

Read [Running algorithms](/guide/execution/running-algorithms) for the consumer model and [Configuration vs execution instances](/contributing/architecture/execution-instances) for the internal ownership rules.

To write an algorithm that coordinates other algorithms rather than operators, continue with [Write a meta-algorithm](/guide/extending/writing-meta-algorithms).
