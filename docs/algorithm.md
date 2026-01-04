# Algorithm

An **algorithm** drives the optimization process.

In HeuristicLib’s abstractions, an algorithm is an **iterative state transformer**:

> Starting from an optional initial state, it produces a sequence of states until a termination policy says “stop”.

## The key interface: `IIterable<...>`

`IIterable<TG, TS, TP, TR>` is the core contract:

- `TR ExecuteStep(TP problem, TR? previousState, IRandomNumberGenerator random)`
- `ITerminator<...> Terminator { get; }`
- `IInterceptor<...>? Interceptor { get; }`

The library also provides extension methods:

- `Execute(problem, random, initialState?)` returns the last produced state.
- `ExecuteStreaming(problem, random, initialState?)` yields states as they are produced.

## `IAlgorithm<...>` and the base class in this repository

`IAlgorithm<...>` is an aggregate interface that combines:

- `IExecutable` (run to completion)
- `IIterable` (run step-by-step)
- `IObservable` (exposes an optional observer)

In this repository, most concrete algorithms inherit from:

- `Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>`

That base class wires the common properties (`Terminator`, `Interceptor`, `Evaluator`) and provides an `Execute(problem, random)` convenience method.

## Algorithms available in this repository

Evolutionary:

- `GeneticAlgorithm<...>`
- `NSGA2<...>`
- `EvolutionStrategy<...>`
- `ALPSGeneticAlgorithm<...>`

Local search:

- `HillClimber<...>`

Meta algorithms:

- `ConcatAlgorithm<...>`

## The canonical story: Genetic Algorithm

If you’re learning the library, start with the genetic algorithm. It composes the most recognizable operator set:

- create initial genotypes
- evaluate them
- select parents
- crossover and mutate offspring
- replace the population

The produced state is a population snapshot, which makes progress easy to reason about.

## Builders (recommended for configuration)

For common algorithms, the repository provides builders that collect configuration in one place.

Example (genetic algorithm):

```csharp
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

var builder = new GeneticAlgorithmBuilder<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> {
	Creator = new RandomPermutationCreator(),
	Crossover = new OrderCrossover(),
	Mutator = new SwapSingleSolutionMutator(),
	PopulationSize = 200,
	MutationRate = 0.20,
	Elites = 2,
	Terminator = new AfterIterationsTerminator<Permutation>(maximumIterations: 200)
};

var algorithm = builder.Build();
```

## Extension points

- **Termination**: `ITerminator` decides when to stop.
- **Interception**: `IInterceptor` transforms the produced state after each step.
- **Observation**: `IIterationObserver` can be used as a side-effect hook and is invoked once per produced state during execution. See [Execution model](execution-model.md).

## Related pages

- [Algorithm state](algorithm-state.md)
- [Operators](operators.md)
- [Execution model](execution-model.md)
