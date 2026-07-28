# Getting started

This page is the shortest path from having a problem to running an algorithm and understanding the default extension story.

The most important ideas are:

1. Configure problems and operators as plain objects.
2. Run algorithms streaming first.
3. When you implement your own algorithm, start from `IterativeAlgorithm<...>`.

## Run a genetic algorithm

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

var problem = TravelingSalesmanProblem.CreateDefault();
var random = RandomNumberGenerator.Create(seed: 123);

var algorithm = new GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>
{
    PopulationSize = 200,
    MaximumGenerations = 200,
    Creator = new RandomPermutationCreator(),
    Crossover = new OrderCrossover(),
    Mutator = new SwapSingleSolutionMutator(),
    MutationRate = 0.20,
    Selector = TournamentSelector.For(problem, tournamentSize: 3),
    Elites = 2
};

await foreach (var state in algorithm.Stream(problem, random))
{
    Console.WriteLine(state.Population.EvaluatedCandidates.Count);
}
```

This is the intended everyday style:

- configure an algorithm directly
- use algorithm-owned budgets such as `MaximumGenerations` for ordinary algorithm setup
- keep behavior-affecting dependencies explicit
- stream states when you want progress

## Implement a small custom algorithm

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

public sealed record MyAlgorithm<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<MyAlgorithm<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TCandidate>();
    public int MaximumStates { get; init; } = 1;

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>> CreateIterativeAlgorithmInstance(
        ExecutionInstanceRegistry registry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? resolvedInterceptor) =>
        new Instance(resolvedInterceptor, registry.Resolve(Creator), registry.Resolve(Evaluator), MaximumStates);

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? interceptor,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        int maximumStates)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>(interceptor)
    {
        private int producedStates;

        protected override bool HasCompleted(int yieldedStateCount, SingleSolutionState<TCandidate>? previousState, TProblem problem) =>
            producedStates >= maximumStates;

        protected override SingleSolutionState<TCandidate> ExecuteStep(SingleSolutionState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            producedStates++;
            var candidate = creator.Create(1, random, problem.SearchSpace, problem)[0];
            var objectiveVector = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];

            return SingleSolutionState.From(candidate, objectiveVector);
        }
    }
}
```

Here the public search state is the current evaluated candidate. If your algorithm needs other public progress data, add it explicitly to your concrete search-state type.

The reusable configuration contains settings and child operator configurations. The nested instance owns resolved child operator instances, step behavior and mutable run data such as `producedStates`. The iterative base resolves and invokes the optional interceptor as part of its sealed streaming lifecycle.

## What not to learn first

You do not need full registry control or meta algorithm infrastructure to implement an ordinary algorithm. Resolve declared children eagerly in the instance creation method then keep execution behavior inside the nested instance.

## Next steps

- [Core concepts](core-concepts.md)
- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)
- [Operators](operators.md)
- [Operator authoring](operator-authoring.md)
