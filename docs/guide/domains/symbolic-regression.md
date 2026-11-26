# Symbolic regression

Symbolic regression searches for an expression that predicts a target series from
input features. HeuristicLib composes it from parts that each own one concern:

| Part                             | Responsibility                                                                            |
| -------------------------------- | ----------------------------------------------------------------------------------------- |
| `ExpressionTree`                 | The immutable candidate. See [symbolic expressions](/guide/domains/symbolic-expressions). |
| `ExpressionTreeSearchSpace`      | Which expressions are valid: symbols, length, depth.                                      |
| Creators, crossover, mutators    | How new candidates are produced within that search space.                                 |
| `SymbolicRegressionProblem`      | Binds training data and metrics, and evaluates candidates.                                |
| `NumericParameterFittingRefiner` | Fits numeric parameters before evaluation.                                                |
| `SymbolicRegressor`              | The fitted predictor used after the search.                                               |

The genotype owns structure only. Search spaces and operators produce and validate
candidates; evaluators and the problem score them without changing them;
variation operators and refiners produce transformed candidates.

## Search space

An `ExpressionTreeSearchSpace` combines hard limits with the symbols a search may use:

```csharp
var searchSpace = new ExpressionTreeSearchSpace(
    maximumLength: 15,
    maximumDepth: 4,
    operations: Symbols.MinimalOperations,
    variables: ["x0", "x1"]);
```

`Symbols` exposes every built-in operation individually and three ready-made sets:
`MinimalOperations` (the four arithmetic operations), `DefaultOperations`, and
`AllOperations`. An overload takes explicit `ConstantSymbol` values, and further
overloads accept a symbol list directly with optional selection weights when some
symbols should be drawn more often than others.

### Symbols and numeric terminals

Every node keeps the `Symbol` it was created from. A symbol owns semantic identity,
arity, how the node compiles, how its payload is initialized, and how it is locally
perturbed. Symbol equality is value equality, not reference identity: two separately
constructed but equal symbols produce equal nodes, while symbols differing in
behavior-affecting configuration do not.

Numeric terminals come in two kinds:

- `Symbols.FixedConstant(value)` is a constant such as `pi`. It never local-mutates,
  though ordinary terminal replacement can still replace it. A fixed constant is _not_
  an evolvable constant with a zero-width distribution.
- `Symbols.Constant(initialDistribution, perturbation)` is an evolvable constant,
  sampled from its initial distribution when created and changed by local mutation
  afterwards. These are the values numeric parameter fitting adjusts.

Creation and local change are separate settings: an initial distribution samples
new values, a perturbation transforms an existing one. **Neither is a bound.** An
initial `Uniform(-1, 1)` does not constrain what later mutation or parameter fitting
may reach; hard value bounds are out of scope.

`NumericPerturbation` provides:

| Perturbation                    | Effect                                                             |
| ------------------------------- | ------------------------------------------------------------------ |
| `Additive(delta)`               | `value + delta`                                                    |
| `Multiplicative(relativeDelta)` | `value * (1 + delta)`, relative to the current value               |
| `Resample(distribution)`        | discard the value, sample the given distribution                   |
| `ResampleInitial()`             | discard the value, sample the owning symbol's initial distribution |
| `Choose(weighted)`              | pick one perturbation by weight; nestable                          |
| `Chain(perturbations)`          | apply in sequence and change nothing if any stage is inapplicable  |

`NumericPerturbation.Default` is `Choose(80% Multiplicative(Uniform(-0.1, 0.1)), 20%
ResampleInitial())` over a `Uniform(-1, 1)` initial distribution. The resampling branch
exists so an exact zero can escape an otherwise multiplicative step. Distributions
support weighted mixtures and may be nested.

A perturbation that samples a no-op still counts as success. It does not retry to force
a changed offspring.

Variable symbols own their allowed names and optional aligned selection weights, and
create nodes only from that set. Local variable perturbation samples uniformly from
the allowed names including the current one, so a symbol with a single allowed
variable perturbs to a valid no-op.

`LocalPerturbationMutator` chooses targets with `LocalPerturbationTargets.One`, `.All`,
or `.Each(probability)`, taken from one snapshot of eligible points so a node is
perturbed at most once per call.

`searchSpace.Contains(expression)` is the authoritative validity check. Containment
uses **aggregate coverage**: a candidate is contained when its operation semantics,
variable names, and constant kinds are covered by the configured symbols as a whole,
and when it respects the length and depth limits. A node does not need to originate
from a structurally identical symbol, so expressions built by different but
compatible configurations remain interchangeable. Selection weights and perturbation
settings are search guidance and do not affect containment.

## Operators

All symbolic-expression operators are typed over `ExpressionTree` and
`ExpressionTreeSearchSpace`, produce candidates that are valid by construction, and
never mutate their inputs.

### Creators

| Creator                        | Behavior                                                                                                                                  |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `GrowTreeCreator`              | Samples any symbol that is structurally viable within the remaining length and depth budget, giving irregular shapes.                     |
| `FullTreeCreator`              | Keeps every leaf at one selected depth. Arities are constrained during creation, so no retries are needed.                                |
| `RampedHalfAndHalfTreeCreator` | Alternates full and grow creation across an inclusive depth range. The usual choice for initializing a population.                        |
| `BalancedTreeCreator`          | Expands randomly selected positions level by level toward a target length. `Irregularity` controls whether terminals may be chosen early. |
| `ProbabilisticTreeCreator`     | Luke's PTC2. Targets a requested length, drawn uniformly from the search-space range unless a distribution is supplied.                   |

Depth settings are nullable and inherit the search-space limits when left unset. An
explicit setting must stay feasible within those limits; a larger candidate domain
needs a larger search space, not a larger operator setting.

### Crossover and mutation

`SubtreeCrossover` selects one destination point and one valid donor without retry
loops. Its `InternalNodeProbability` defaults to uniform selection over all nodes;
setting `0.9` gives the conventional Koza-style bias toward internal nodes.

| Mutator                    | Behavior                                                                                                                                                                     |
| -------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `NodeReplacementMutator`   | Replaces a symbol while keeping the children, when arity allows.                                                                                                             |
| `SubtreeMutator`           | Replaces one occurrence with a freshly created subtree that fits the exact remaining budget.                                                                                 |
| `ShrinkSubtreeMutator`     | Replaces an operation occurrence with a terminal, so length strictly decreases.                                                                                              |
| `LocalPerturbationMutator` | Perturbs numeric and variable payloads. `One`, `All`, and `Each(probability)` select how many points are touched; the perturbation itself belongs to the originating symbol. |

Combine them with the general `ChoosePerItem` rather than a symbolic-expression
specific multi-mutator:

```csharp
Mutator = new ChoosePerItem<Mutation, ExpressionTree>(
    [new NodeReplacementMutator(), new SubtreeMutator(), new LocalPerturbationMutator()])
{
    Weights = [1.0, 1.0, 1.0]
}
```

## Problem and objectives

`SymbolicRegressionProblem` binds one `RegressionData` training set, its metrics, and
one search space. The simple constructor defaults to `Metrics.MSE`:

```csharp
var problem = new SymbolicRegressionProblem(data, Metrics.RMSE, searchSpace);
```

Two metric families are available. **Regression metrics** (`IRegressionMetric`)
compare predictions with the target; **expression metrics** (`IExpressionMetric`)
inspect the genotype without predicting anything:

```csharp
var problem = new SymbolicRegressionProblem(
    data,
    Metrics.MSE,
    ExpressionMetrics.Length,
    searchSpace);
```

The objective vector is then `[MSE, Length]`, with both directions minimizing.

A regression metric is an `IPredictionMetric<double>`, as
[data and machine learning](/guide/domains/data-and-machine-learning#metrics)
explains, and the problem keeps its regression metrics in `PredictionMetrics`.
The static class `ExpressionMetrics` offers `Length`, `VariableCount`, the number of
variable occurrences, and `Complexity`. All three are minimized. `Complexity` scores
the tree recursively, from its leaves to its root:

- A variable scores 2 and any other terminal, such as a constant, scores 1.
- Negation keeps the score of its operand.
- Addition and subtraction add the scores of their operands.
- Multiplication and division score `(1 + left) * (1 + right)`.
- Exponential and logarithm score 2 raised to the score of their operand, and square
  root scores the cube of its operand's score.
- Any other operation scores 1 plus the scores of its operands.

Regression metrics are calculated once from one shared prediction vector and appear
before expression metrics in the objective vector. Either collection may be empty,
but not both. Multiple objectives use lexicographic total order by default while
their individual directions still govern dominance. The problem evaluates only its
bound training data; to predict on other data, use a [predictor](#predictors).

Set `useLinearScaling: true` to fit a least-squares slope and intercept for each
candidate evaluation. Scaling is applied to the predictions every metric sees and
leaves the genotype untouched, so it does not affect expression identity, length,
containment, mutation, or crossover. See [data and machine learning](/guide/domains/data-and-machine-learning#linear-scaling).

## Running a search

```csharp
var algorithm = GeneticAlgorithm.Create(
    new RampedHalfAndHalfTreeCreator(),
    new SubtreeCrossover(),
    ChoosePerItem.Create(
        new NodeReplacementMutator(),
        new SubtreeMutator(),
        new LocalPerturbationMutator()),
    selector: TournamentSelector.For(problem, tournamentSize: 2),
    populationSize: 24,
    maximumGenerations: 8,
    mutationRate: 0.2);

var finalState = await algorithm.RunToCompletionAsync(problem, RandomNumberGenerator.Create(123));
```

## Numeric parameter fitting

Evolutionary search finds structure well and numeric constants poorly. A refiner
fits the evolvable constants of a candidate with Levenberg-Marquardt, using
automatic differentiation over the expression, and returns a **new** expression with
the fitted values applied. The fitted values are constants in the genotype and
parameters in the numerics.

Other tools call this constant optimization, as PySR and HeuristicLab do, or
parameter identification or parameter optimization. Those names help you find related
material, but API names here say numeric parameter fitting. Avoid calling it
coefficient optimization: a coefficient is a multiplicative factor, and
[linear scaling](/guide/domains/data-and-machine-learning#linear-scaling) is what fits
coefficients.

```csharp
Refiner = new NumericParameterFittingRefiner { MaximumIterations = 10 }
```

The algorithm applies it after creation and after final variation, immediately before
evaluation. Fixed constants are left alone. Structurally shared constant nodes at
different points are fitted as independent parameters. The source expression is never
modified.

Refinement commonly uses over 95 percent of total run time. The settings that matter
most are how many candidates are
refined and how many iterations each fit gets:

```csharp
Refiner = new NumericParameterFittingRefiner().AppliedAtRate(0.25)
```

`FittingData` restricts fitting to a subset of rows. Wrapping with
`.CheckedForImprovement()` keeps a fit only when it actually improved the candidate.
Refiner composition, evaluation accounting, and the cost measurements behind these
recommendations are covered in
[operator composition](/guide/extending/operator-composition#refiner-composition).

A search space may allow operations the differentiable compiler cannot lower. Pre-run
validation does not check differentiability, so the refiner reports the incompatibility
when it first encounters an affected candidate.

## Compiled execution

Use `Evaluate` for a one-off prediction. It compiles the expression and evaluates it
against the data columns:

```csharp
double[] predictions = expression.Evaluate(data.Inputs);
```

Use `Compile` when one expression is evaluated many times, such as when scoring a final
model or predicting repeatedly in production, so the expression is compiled once:

```csharp
var compiled = expression.Compile(optimize: true);
double[] predictions = compiled.Evaluate(data.Inputs);
```

`Evaluate` also accepts a caller-provided destination span, and a workspace span for
allocation-sensitive loops. `EvaluateSingleRow` evaluates one observation from named
values.

A `CompiledExpression` is for evaluation only; edit the `ExpressionTree` and compile
again to change it. Invalid numeric results follow ordinary IEEE 754 behavior and
produce `NaN` or infinity. Evaluation does not clamp results; apply bounds with
`ToBounded` on a predictor, or penalties in your objective.

## Building expressions against a search space

`ExpressionDraft.Build()` produces a standalone expression using draft-local symbols.
`Build(searchSpace)` instead resolves each draft term against the search space's
symbols, so the resulting nodes carry the origins that search-space-aware operators
expect. Resolution succeeds only when every unbound term has exactly one viable
symbol; zero or multiple matches are an error rather than an arbitrary choice.
`TryBuild` gives the same resolution without throwing.

Pass an explicit symbol when several would match with `Variable(name, variableSymbol)`.

Duplicate symbols in a search space stay distinct selection entries in their supplied
order, so accidental duplication stays visible instead of being silently merged.

## Predictors

The problem evaluates training data. Predicting on validation, test, or production
inputs goes through a predictor:

```csharp
var regressor = expression.ToRegressor("prediction");
Series<double> predictions = regressor.Predict(testInputs);
```

`ToRegressor` compiles the expression once and the returned `SymbolicRegressor` retains
the compiled form. `ToBounded(lower, upper)` constrains outputs.
`FitLinearScaling(data)` fits and retains affine scaling for later prediction. This
carries search-time linear scaling into the final model.
