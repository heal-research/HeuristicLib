# Algorithms

An algorithm controls how a search begins, how one state becomes the next and when the run stops. You configure it as a plain C# value, then run it with a problem and random number generator.

## Choose a starting point

| Algorithm           | Use it when                                                              |
| ------------------- | ------------------------------------------------------------------------ |
| `HillClimber`       | A meaningful local mutation exists and a fast baseline is useful         |
| `GeneticAlgorithm`  | A population and recombination can explore a broad search space          |
| `EvolutionStrategy` | You are optimizing real valued candidates through mutation and selection |
| `NSGA2`             | The problem has competing objectives and you want a diverse Pareto front |

`NSGA2` differs from the others in what it returns. It approximates a set of tradeoffs rather than one best candidate, and it needs a selector and replacer that understand dominance. See [Multiobjective optimization](/examples/multi-objective).

Start with the simplest algorithm that represents the search you need. A hill climber makes a useful baseline. A more elaborate method should earn its complexity through repeated experiments.

## Choose a construction form

HeuristicLib offers three ways to construct a standard algorithm. They create the same configuration record and none of them starts a run.

| Form                               | Use it when                                                                          | What the caller supplies                                                           |
| ---------------------------------- | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------- |
| Constructor and object initializer | You need an exact generic type or want every configured member visible as a property | Generic type arguments and every required operator                                 |
| `Create(...)`                      | You chose the required operators and want their types inferred                       | Required operators, followed by any algorithm setting overrides                    |
| `For(...)`                         | A problem or search space recommends some or all required operators                   | A problem or search space, followed by any operator or algorithm setting overrides |

All three forms use the algorithm's public defaults class for omitted optional settings. Direct construction and `Create(...)` never choose required variation operators for you. `For(...)` is the only form that asks a problem first, then its search space, for recommendations.

The constructor is the direct record API. `Create(...)` and `For(...)` are type inference conveniences over it. After construction, every form supports the same property access and `with` expressions.

All three forms work with any problem. `For(...)` chooses each required operator from an explicit argument, a problem recommendation or a search space recommendation, in that order. If any required parameters remain missing, it throws `InvalidOperationException` during construction and names every missing parameter.

## Configure a genetic algorithm

```csharp
var algorithm = GeneticAlgorithm.Create(
    new UniformDistributedCreator(problem.SearchSpace),
    new AlphaBetaBlendCrossover { Alpha = 0.7 },
    new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
    selector: TournamentSelector.For(problem, tournamentSize: 2),
    populationSize: 50,
    maximumGenerations: 100,
    mutationRate: 0.2);
```

Algorithm owned settings such as `MaximumGenerations` describe ordinary run behavior. The operators make search policy visible and replaceable. `Create(...)` infers the generic arguments from those operators.

## Use operator recommendations

Some problems and search spaces recommend operators. Use `For(problem, ...)` when those recommendations cover the required parameters you do not supply:

```csharp
var algorithm = GeneticAlgorithm.For(
    travelingSalesmanProblem,
    populationSize: 100,
    maximumGenerations: 500);
```

For each required operator, `For(problem, ...)` checks these sources in order:

1. The operator argument passed to `For(...)`.
2. A recommendation from the problem.
3. A recommendation from the problem's search space.

`For(searchSpace, ...)` uses the same order without the problem step. A source may decline to recommend an operator for its current state. The factory then continues to the next source. If any required operator remains missing, the factory throws `InvalidOperationException` and names all missing parameters.

The current built in recommendations are deliberately small:

| Source | Creator | Crossover | Mutator |
| --- | --- | --- | --- |
| `PermutationSearchSpace` | `RandomPermutationCreator` | `EdgeRecombinationCrossover` | `InversionMutator` |
| `TravelingSalesmanProblem` | none | `OrderCrossover` | none |

This means the example uses order crossover from `TravelingSalesmanProblem`, then random creation and inversion mutation from `PermutationSearchSpace`. The problem recommendation wins over the search space recommendation for crossover.

Each standard algorithm has a public defaults class, such as `GeneticAlgorithmDefaults` or `NSGA2Defaults`. Direct record construction, `Create(...)` and `For(...)` read the same values for omitted algorithm settings. Switching construction forms therefore does not change an omitted population size, selector or evaluator. Operator recommendations are separate because a problem or search space owns the information needed to make them.

`For(searchSpace, ...)` returns a configuration that can run against any compatible problem. `GeneticAlgorithm` and `NSGA2` request a creator, crossover and mutator. `EvolutionStrategy` and `HillClimber` request a creator and mutator. Optional operators are not populated from recommendations. For example, `EvolutionStrategy.Crossover` remains `null` unless the caller supplies it.

Recommendation discovery uses `IRecommends<TOperator>`, where `TOperator` is the requested operator role. `TryCreateRecommendedOperator` returns `true` with a fresh, nonnull operator. It returns `false` with `null` when the source declines for its current state. A problem or search space can implement this contract for built in or consumer defined roles. Consumer algorithms can use `OperatorRecommendationResolution` to apply the same precedence and aggregate missing parameter error.

Recommendations are documented starting points. They are not tuned for every instance and changing them can change results. State important choices explicitly when a configuration must remain independent of future recommendation changes:

```csharp
var algorithm = GeneticAlgorithm.For(travelingSalesmanProblem)
    with { Crossover = new EdgeRecombinationCrossover() };
```

Use `Create(...)` when every required operator is part of the experiment. Optional arguments override algorithm defaults in either form.

## Run it

Ask for only the final state when intermediate progress is not needed:

```csharp
var finalState = await algorithm.CompleteAsync(problem, random);
```

Stream states for progress, visualization or early inspection:

```csharp
await foreach (var state in algorithm.Stream(problem, random))
{
    Console.WriteLine(state.Population.EvaluatedCandidates.Count);
}
```

Each call starts a fresh run. See [Running algorithms](/guide/execution/running-algorithms) for cancellation, budgets and reproducibility.

## Compare algorithms fairly

- Use the same problem instances and seed schedule.
- Compare outcomes over repeated runs.
- Record evaluation counts when algorithms do different amounts of work per iteration.
- Include a simple baseline.
- Avoid tuning on the same instances used for the final comparison.

The [Experiments](/guide/execution/experiments) API helps express repetitions and parameter grids.

## Custom algorithms

Most domain customization belongs in a problem or operator. Create an algorithm only when the state transition or control flow itself is new. Read [Writing algorithms](/guide/extending/writing-algorithms) before implementing one.
