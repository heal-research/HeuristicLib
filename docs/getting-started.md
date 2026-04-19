# Getting started

This page is the shortest path from having a problem to running an algorithm and understanding the default extension story.

The most important current ideas are:

1. configure problems and operators as plain objects
2. run algorithms streaming-first
3. when you implement your own algorithm, start from `IterativeAlgorithm<...>` or `StatefulIterativeAlgorithm<...>`

## Run a genetic algorithm

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

var problem = TravelingSalesmanProblem.CreateDefault();
var rng = new SystemRandomNumberGenerator(seed: 123);

var algorithm = new GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> {
  PopulationSize = 200,
  Creator = new RandomPermutationCreator(),
  Crossover = new OrderCrossover(),
  Mutator = new SwapSingleSolutionMutator(),
  MutationRate = 0.20,
  Selector = new TournamentSelector<Permutation>(tournamentSize: 3),
  Elites = 2,
  Evaluator = new DirectEvaluator<Permutation>()
}.WithMaxIterations(maxIterations: 200);

await foreach (var state in algorithm.RunStreamingAsync(problem, rng)) {
  Console.WriteLine(state.Population.Solutions.Count);
}
```

This is the intended everyday style:

- configure an algorithm directly
- keep behavior-affecting dependencies explicit
- stream states when you want progress

## Implement a small custom algorithm

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

public sealed record MyState : AlgorithmState
{
  public required int Iteration { get; init; }
}

public sealed record MyAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, MyState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected override MyState ExecuteStep(
    MyState? previousState,
    IOperatorExecutor executor,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    return new MyState {
      Iteration = (previousState?.Iteration ?? 0) + 1
    };
  }
}
```

If the algorithm needs hidden mutable per-run state, switch to `StatefulIterativeAlgorithm<...>` and add `TRuntimeState`.

## What not to learn first

You do not need to start with:

- execution instances
- manual registry resolution
- meta algorithms

Those are real parts of the system, but they are not the intended first extension path anymore.

## Next steps

- [Core concepts](core-concepts.md)
- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)
- [Operators](operators.md)
