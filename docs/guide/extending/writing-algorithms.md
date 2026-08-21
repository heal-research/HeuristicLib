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
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

public sealed record SingleCreateAlgorithm
    : IterativeAlgorithm<
        SingleCreateAlgorithm,
        RealVector,
        RealVectorSearchSpace,
        TestFunctionProblem,
        SingleSolutionState<RealVector>>
{
    public required ICreator<
        RealVector,
        RealVectorSearchSpace,
        TestFunctionProblem> Creator { get; init; }

    public IEvaluator<
        RealVector,
        RealVectorSearchSpace,
        TestFunctionProblem> Evaluator { get; init; } = new ProblemEvaluator<RealVector>();

    protected override IterativeAlgorithmInstance<
        RealVector,
        RealVectorSearchSpace,
        TestFunctionProblem,
        SingleSolutionState<RealVector>> CreateExecutionInstance(
            ExecutionInstanceRegistry registry,
            IInterceptorInstance<
                RealVector,
                RealVectorSearchSpace,
                TestFunctionProblem,
                SingleSolutionState<RealVector>>? interceptor) =>
        new Instance(
            interceptor,
            registry.Resolve(Creator),
            registry.Resolve(Evaluator));

    private sealed class Instance(
        IInterceptorInstance<
            RealVector,
            RealVectorSearchSpace,
            TestFunctionProblem,
            SingleSolutionState<RealVector>>? interceptor,
        ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> creator,
        IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> evaluator)
        : IterativeAlgorithmInstance<
            RealVector,
            RealVectorSearchSpace,
            TestFunctionProblem,
            SingleSolutionState<RealVector>>(interceptor)
    {
        protected override SingleSolutionState<RealVector> ExecuteStep(
            SingleSolutionState<RealVector>? previousState,
            TestFunctionProblem problem,
            IRandomNumberGenerator random)
        {
            var candidate = creator.Create(1, random, problem.SearchSpace, problem)[0];
            var objective = evaluator.Evaluate(
                [candidate],
                random,
                problem.SearchSpace,
                problem)[0];

            return SingleSolutionState.From(candidate.ToEvaluated(objective));
        }
    }
}
```

Use `.WithMaxIterations(count)` or another terminator when the algorithm does not stop itself.

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
