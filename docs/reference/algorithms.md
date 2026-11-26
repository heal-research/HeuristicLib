# Algorithms and defaults

The algorithms of the core package, the operators each one requires and the value it takes for every setting you omit. [Algorithms](/guide/fundamentals/algorithms) explains how to choose and construct one.

## Search algorithms

| Algorithm              | Search state             | Requires                          | Optional operators                                                                           |
| ---------------------- | ------------------------ | --------------------------------- | -------------------------------------------------------------------------------------------- |
| `GeneticAlgorithm<T>`  | `PopulationState<T>`     | `Creator`, `Crossover`, `Mutator` | `Selector`, `Evaluator`, `Refiner`, `Terminator`, `Interceptor`                              |
| `EvolutionStrategy<T>` | `PopulationState<T>`     | `Creator`, `Mutator`              | `Crossover`, `Selector`, `Evaluator`, `Refiner`, `MutationStrengthAdaptation`, `Interceptor` |
| `NSGA2<T>`             | `PopulationState<T>`     | `Creator`, `Crossover`, `Mutator` | `Selector`, `Replacer`, `Evaluator`, `Refiner`, `Interceptor`                                |
| `HillClimber<T>`       | `SingleSolutionState<T>` | `Creator`, `Mutator`              | `Evaluator`, `Refiner`, `Interceptor`                                                        |

`For(problem, ...)` and `For(searchSpace, ...)` fill missing required operators from [recommendations](#operator-recommendations). Optional operators are never filled from recommendations.

## Defaults

Each algorithm reads its defaults from a public class, such as `GeneticAlgorithmDefaults`. Direct construction, `Create(...)` and `For(...)` use the same values.

### `GeneticAlgorithmDefaults`

| Setting              | Default                        |
| -------------------- | ------------------------------ |
| `PopulationSize`     | 100                            |
| `MaximumGenerations` | 1000                           |
| `MutationRate`       | 0.1                            |
| `Elites`             | 1                              |
| `Selector`           | `TournamentSelector` of size 2 |
| `Evaluator`          | `ProblemEvaluator`             |

### `EvolutionStrategyDefaults`

| Setting              | Default            |
| -------------------- | ------------------ |
| `PopulationSize`     | 100                |
| `NumberOfChildren`   | 100                |
| `Strategy`           | `Plus`             |
| `MaximumGenerations` | 1000               |
| `Selector`           | `RandomSelector`   |
| `Evaluator`          | `ProblemEvaluator` |

### `NSGA2Defaults`

| Setting              | Default                                                        |
| -------------------- | -------------------------------------------------------------- |
| `PopulationSize`     | 100                                                            |
| `MaximumGenerations` | 1000                                                           |
| `MutationRate`       | 0.1                                                            |
| `Selector`           | `ParetoCrowdingTournamentSelector`, dominating on equal values |
| `Replacer`           | `ParetoCrowdingReplacer`, dominating on equal values           |
| `Evaluator`          | `ProblemEvaluator`                                             |

### `HillClimberDefaults`

| Setting        | Default            |
| -------------- | ------------------ |
| `Direction`    | `FirstImprovement` |
| `MaxNeighbors` | 100                |
| `BatchSize`    | 100                |
| `Evaluator`    | `ProblemEvaluator` |

A `MaximumGenerations` of `null` lets a run end on its terminator alone. Defaults are starting points, not tuned values. Record the values you rely on when a result must stay reproducible.

## Operator recommendations

| Source                     | Creator                    | Crossover                    | Mutator            |
| -------------------------- | -------------------------- | ---------------------------- | ------------------ |
| `PermutationSearchSpace`   | `RandomPermutationCreator` | `EdgeRecombinationCrossover` | `InversionMutator` |
| `TravelingSalesmanProblem` | none                       | `OrderCrossover`             | none               |

A problem's recommendation wins over its search space's.

## Algorithms that wrap or combine algorithms

| Algorithm                          | Created with                                                    | Does                                                                  |
| ---------------------------------- | --------------------------------------------------------------- | --------------------------------------------------------------------- |
| `PipelineAlgorithm`                | `first.Then(second)`                                            | Runs each stage to completion and starts the next from its last state |
| `CycleAlgorithm`                   | `first.CycleWith(second)`                                       | Repeats its stages in order, each turn continuing from the last state |
| `StateTerminatedAlgorithm`         | `algorithm.TerminatedBy(terminator)`                            | Stops the inner algorithm when a terminator fires                     |
| `OperatorCountBudgetAlgorithm`     | `LimitedToInvocations`, `LimitedToCandidates`, `LimitedToCount` | Stops when counted operator work reaches a limit                      |
| `OperatorDurationBudgetAlgorithm`  | `algorithm.LimitedToDuration(limit, placement)`                 | Stops when timed operator work reaches a limit                        |
| `AlgorithmDurationBudgetAlgorithm` | `algorithm.LimitedToDuration(limit)`                            | Stops when the algorithm's own running time reaches a limit           |
| `NamedAlgorithm`                   | `algorithm.Named(name)`                                         | Gives an algorithm a name that selections can find                    |
| `SharingAlgorithm`                 | `algorithm.Sharing(configurations)`                             | Keeps one executor for the listed configurations across child scopes  |
