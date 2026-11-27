# Operators by encoding

The operators of the core package, grouped by the candidate they work on. [Operators](/guide/fundamentals/operators) explains the roles, and [Operator composition](/guide/fundamentals/operator-composition) covers the operators that combine others.

## Real vectors

Candidate `RealVector`, search space `BoundedRealVectorSearchSpace`.

| Operator                               | Role      | Does                                                                   |
| -------------------------------------- | --------- | ---------------------------------------------------------------------- |
| `UniformDistributedCreator`            | Creator   | Draws each element uniformly within the bounds                         |
| `NormalDistributedCreator`             | Creator   | Draws each element from a normal distribution, clamped to the bounds   |
| `AlphaBetaBlendCrossover`              | Crossover | Samples each element around both parents, wider towards the better one |
| `SimulatedBinaryCrossover`             | Crossover | Simulated binary crossover (SBX)                                       |
| `SelfAdaptiveSimulatedBinaryCrossover` | Crossover | The self-adaptive variant of SBX                                       |
| `SinglePointCrossover`                 | Crossover | Joins the head of one parent with the tail of the other                |
| `GaussianMutator`                      | Mutator   | Adds normal noise to each element with a given rate and strength       |
| `PolynomialMutator`                    | Mutator   | Polynomial mutation, clamped to the bounds                             |

## Integer vectors

Candidate `IntegerVector`, search space `IntegerVectorSearchSpace`. The rounded operators compute a real value and round it into the bounds.

| Operator                            | Role      | Does                                                             |
| ----------------------------------- | --------- | ---------------------------------------------------------------- |
| `UniformDistributedCreator`         | Creator   | Draws each element uniformly within the bounds                   |
| `NormalDistributedCreator`          | Creator   | Draws each element from a rounded normal distribution            |
| `DiscreteCrossover`                 | Crossover | Takes each element from a randomly chosen parent                 |
| `SinglePointCrossover`              | Crossover | Joins the head of one parent with the tail of the other          |
| `RoundedAverageCrossover`           | Crossover | Averages the parents                                             |
| `RoundedBlendAlphaCrossover`        | Crossover | Samples each element from the parents' range, widened by `Alpha` |
| `RoundedBlendAlphaBetaCrossover`    | Crossover | Like blend alpha, widened by `Alpha` and `Beta`                  |
| `RoundedHeuristicCrossover`         | Crossover | Steps from the worse parent past the better one                  |
| `RoundedLocalCrossover`             | Crossover | A random weighted average per element                            |
| `RoundedUniformArithmeticCrossover` | Crossover | A weighted average at positions chosen with `Probability`        |
| `UniformOnePositionMutator`         | Mutator   | Replaces one element with a uniform value                        |
| `UniformSomePositionsMutator`       | Mutator   | Replaces each element with probability `Probability`             |
| `RoundedNormalOnePositionMutator`   | Mutator   | Adds rounded normal noise to one element                         |
| `RoundedNormalAllPositionsMutator`  | Mutator   | Adds rounded normal noise to every element                       |

## Bool vectors

Candidate `BoolVector`, search space `BoolVectorSearchSpace` or `FixedCardinalityBoolVectorSearchSpace`.

| Operator                  | Role      | Does                                                                      |
| ------------------------- | --------- | ------------------------------------------------------------------------- |
| `RandomBoolVectorCreator` | Creator   | Sets each element with probability `SetProbability`                       |
| `BitUniformCrossover`     | Crossover | Takes each element from a randomly chosen parent                          |
| `BitFlipMutator`          | Mutator   | Flips each element with probability `FlipProbability`                     |
| `BitSwapMutator`          | Mutator   | Swaps one set and one cleared element, so the count of set elements stays |

## Permutations

Candidate `Permutation`, search space `PermutationSearchSpace`.

| Operator                     | Role      | Does                                                               |
| ---------------------------- | --------- | ------------------------------------------------------------------ |
| `RandomPermutationCreator`   | Creator   | Draws a uniformly random permutation                               |
| `OrderCrossover`             | Crossover | Keeps a segment of one parent, fills the rest in the other's order |
| `EdgeRecombinationCrossover` | Crossover | Builds the child from edges both parents contain                   |
| `SwapMutator`                | Mutator   | Swaps two elements                                                 |
| `InversionMutator`           | Mutator   | Reverses a segment                                                 |

## Expression trees

Candidate `ExpressionTree`, search space `ExpressionTreeSearchSpace`. [Symbolic expressions](/guide/domains/symbolic-expressions) and [Symbolic regression](/guide/domains/symbolic-regression) explain the settings and the numeric perturbations.

| Operator                         | Role      | Does                                                         |
| -------------------------------- | --------- | ------------------------------------------------------------ |
| `GrowTreeCreator`                | Creator   | Grows trees of mixed shape up to `MaximumDepth`              |
| `FullTreeCreator`                | Creator   | Builds trees whose leaves all sit at `Depth`                 |
| `RampedHalfAndHalfTreeCreator`   | Creator   | Mixes grow and full trees over a range of depths             |
| `BalancedTreeCreator`            | Creator   | Builds trees of a requested length breadth-first             |
| `ProbabilisticTreeCreator`       | Creator   | Probabilistic tree creation (PTC2) for a requested length    |
| `SubtreeCrossover`               | Crossover | Replaces a subtree of one parent with a subtree of the other |
| `SubtreeMutator`                 | Mutator   | Replaces a random subtree with a newly grown one             |
| `ShrinkSubtreeMutator`           | Mutator   | Replaces an operation with a terminal                        |
| `NodeReplacementMutator`         | Mutator   | Replaces one symbol with another of the same arity           |
| `LocalPerturbationMutator`       | Mutator   | Perturbs constants and variable weights                      |
| `NumericParameterFittingRefiner` | Refiner   | Fits the numeric parameters of a symbolic regression model   |

## Any candidate

These operators work for every candidate type.

### Selectors

| Operator                           | Does                                                             |
| ---------------------------------- | ---------------------------------------------------------------- |
| `TournamentSelector`               | Picks the best of `TournamentSize` random candidates             |
| `ProportionalSelector`             | Picks in proportion to quality                                   |
| `LinearRankSelector`               | Picks in proportion to rank                                      |
| `GeneralizedRankSelector`          | Picks by rank with a configurable `Pressure`                     |
| `RandomSelector`                   | Picks uniformly                                                  |
| `BestSelector`                     | Picks the best candidates                                        |
| `WorstSelector`                    | Picks the worst candidates                                       |
| `ParetoCrowdingTournamentSelector` | Tournament by Pareto rank, then crowding distance                |
| `EliteSelector`                    | Takes the best `Elites`, then asks another selector for the rest |
| `NoSameMatesSelector`              | Asks another selector again when both parents are the same       |
| `GenderSpecificSelector`           | Pairs one candidate from each of two selectors                   |

### Replacers

| Operator                 | Does                                                    |
| ------------------------ | ------------------------------------------------------- |
| `ElitismReplacer`        | Keeps the best `Elites` of the previous population      |
| `PlusSelectionReplacer`  | Keeps the best of parents and offspring together        |
| `CommaSelectionReplacer` | Keeps the best offspring only                           |
| `ParetoCrowdingReplacer` | Keeps candidates by Pareto rank, then crowding distance |

### Evaluators and refiners

| Operator                     | Role      | Does                                                               |
| ---------------------------- | --------- | ------------------------------------------------------------------ |
| `ProblemEvaluator`           | Evaluator | Evaluates through the problem                                      |
| `CachingEvaluator`           | Evaluator | Reuses results for equal keys                                      |
| `LimitEvaluator`             | Evaluator | Returns a fallback objective after `MaxEvaluations`                |
| `RepeatingEvaluator`         | Evaluator | Evaluates repeatedly and aggregates the results                    |
| `RelativeQualityEvaluator`   | Evaluator | Normalizes against a best-known objective vector                   |
| `RefinementEvaluator`        | Evaluator | Evaluates refined copies and discards them                         |
| `NoChangeRefiner`            | Refiner   | Returns the candidates unchanged                                   |
| `IteratedRefiner`            | Refiner   | Applies a refiner to its own output                                |
| `ImprovementCheckingRefiner` | Refiner   | Keeps a refinement only if a criterion counts it as an improvement |
| `PipelineRefiner`            | Refiner   | Applies refiners in order                                          |

### Terminators

| Operator                          | Stops when                                            |
| --------------------------------- | ----------------------------------------------------- |
| `AfterIterationsTerminator`       | A number of iterations is reached                     |
| `AfterElapsedTimeTerminator`      | A time span has passed                                |
| `AfterOperatorCountTerminator`    | A counter reaches a count                             |
| `AfterOperatorDurationTerminator` | A duration reaches a time span                        |
| `TargetTerminator`                | The best objective reaches a target                   |
| `StagnationTerminator`            | The objective has not improved for a number of states |
| `CancellationTokenTerminator`     | A token is canceled                                   |
| `AnyTerminator`                   | Any child terminator fires                            |
| `AllTerminator`                   | All child terminators fire                            |
| `NeverTerminator`                 | Never                                                 |

### Interceptors, mutators, crossovers and creators

| Operator                      | Role        | Does                                                      |
| ----------------------------- | ----------- | --------------------------------------------------------- |
| `IdentityInterceptor`         | Interceptor | Returns the state unchanged                               |
| `RemoveDuplicatesInterceptor` | Interceptor | Removes duplicate candidates from the state               |
| `PipelineInterceptor`         | Interceptor | Applies interceptors in order                             |
| `NoChangeMutator`             | Mutator     | Returns the candidates unchanged                          |
| `PipelineMutator`             | Mutator     | Applies mutators in order                                 |
| `RandomCrossover`             | Crossover   | Returns one parent unchanged, chosen at random            |
| `SelectFirstParentCrossover`  | Crossover   | Returns the first parent                                  |
| `SelectSecondParentCrossover` | Crossover   | Returns the second parent                                 |
| `TransformedCrossover`        | Crossover   | Crosses, then always applies a mutator                    |
| `TransformedCreator`          | Creator     | Creates, then always applies a mutator                    |
| `PredefinedCandidatesCreator` | Creator     | Returns given candidates first, then asks another creator |
