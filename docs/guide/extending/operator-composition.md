# Operator composition

::: info Advanced extension
Start with [built-in operators](/guide/fundamentals/operators). Compose operators when several reusable policies should behave as one operator in an algorithm configuration.
:::

Operator composition builds a new operator configuration from one or more existing operator configurations. The resulting configuration remains reusable and resolves its child execution instances as part of the execution graph.

Composition policies describe how child operations are coordinated. `Wrapping*` and `Multi*` bases are authoring infrastructure for implementing those policies. They do not prescribe behavior by themselves.

The built in composition overview is:

| Composition aspect                | Coordination semantics                                                                                            | Helpers and examples                                                                                        | Applicability                                         |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ----------------------------------------------------- |
| Weighted alternatives per element | Chooses a child independently for each element of a batch, invokes grouped child batches and restores input order | `ChooseOneCreator`, `ChooseOneCrossover`, `ChooseOneMutator`, `ChooseOneRefiner`                            | Creators, crossovers, mutators and refiners           |
| Weighted alternatives per call    | Chooses one child for the complete operation call                                                                 | `ChooseOneSelector`, `ChooseOneReplacer`                                                                    | Selectors and replacers                               |
| Conditional application           | Chooses between an operation and role specific unchanged behavior                                                 | `mutator.AppliedAtRate(...)`, `crossover.AppliedAtRate(...)`, `refiner.AppliedAtRate(...)`                                 | Mutators, crossovers and refiners                     |
| Sequential composition            | Passes each stage result to the next stage in order                                                               | `PipelineMutator`, `PipelineRefiner`, `PipelineInterceptor`                                                 | Mutators, refiners and interceptors                   |
| Repeated composition              | Feeds a result back into the same child for a configured number of iterations                                     | `IteratedRefiner`                                                                                           | Refiners                                              |
| Objective-aware retention         | Measures a child's result and keeps it only when a criterion counts it as an improvement                          | `ImprovementCheckingRefiner`                                                                                | Refiners                                              |
| Transient refinement evaluation   | Refines candidates temporarily, measures the refined copies and discards them                                     | `RefinementEvaluator`                                                                                       | Evaluators composed with a refiner                    |
| Unconditional postprocessing      | Invokes a source operation and always transforms its complete result                                              | `TransformedCreator`, `TransformedCrossover`                                                                | Creators and crossovers with a mutator transformation |
| Logical composition               | Combines child conditions with short circuit Boolean logic                                                        | `AnyTerminator`, `AllTerminator`                                                                            | Terminators                                           |
| Fallback composition              | Handles part of a request directly and delegates the remainder                                                    | `PredefinedCandidatesCreator`                                                                               | Creators                                              |
| Specialized coordination          | Applies a policy whose meaning depends on one operator contract                                                   | Caching, repetition and evaluation limits, elite inclusion, mate restrictions and gender specific selection | Evaluators and selectors                              |
| Observation and instrumentation   | Wraps an operation boundary to observe, count or measure calls                                                    | `Observable*`, `Counting*` and `DurationMeasuring*`                                                         | All operator roles                                    |

A missing helper usually means that the role does not provide the inputs needed by that policy or that the intermediate result has no single clear meaning. It does not prohibit authors from implementing a domain specific composition with explicit semantics.

## Cardinality conventions

Operator roles usually follow these cardinality conventions:

1. A creator returns the requested number of candidates.
2. An evaluator returns one objective vector for each supplied candidate in the same order.
3. A crossover returns one candidate for each supplied parent group.
4. A mutator returns one candidate for each supplied candidate.
5. A selector and replacer return the requested number of evaluated candidates.

These are conventions rather than universal restrictions. A specialized operator may intentionally change cardinality when its owning algorithm supports that behavior. For example, a replacer could return more candidates than requested to grow a population.

Individual composition policies may require a stronger contract. A composition must document such a requirement when its coordination behavior depends on cardinality.

## Weighted alternatives

`ChooseOneCreator`, `ChooseOneCrossover` and `ChooseOneMutator` select a child independently for each element of a requested batch. Elements assigned to the same child are passed to that child as one batch. Results are then restored to the original element order.

This order restoration requires every selected child to return exactly one result for each assigned element. `ChooseOneCreator` therefore requires each child creator to return the assigned count. `ChooseOneCrossover` and `ChooseOneMutator` require each child to preserve its assigned batch size.

Weights are retained exactly as configured. Omitting weights selects uniformly across all children. Positive finite weights participate proportionally; zero, negative finite weights, negative infinity and `NaN` mean never. Positive infinity overrides finite weights, with multiple positive-infinity children selected uniformly. If no child is selectable, selection falls back to uniform across all children. Configured weights are compiled once for repeated sampling rather than normalized or recalculated for every choice.

`ChooseOneSelector` and `ChooseOneReplacer` make one weighted choice for each complete operation call. The selected child receives the complete population inputs and requested count. This preserves the meaning of a selector or replacer policy as a decision over a complete population instead of mixing fragments from several policies.

```csharp
var creator = ChooseOneCreator.Create(
    [uniformCreator, normalCreator],
    [0.75, 0.25]);

var mutator = ChooseOneMutator.Create(
    [smallMutation, largeMutation],
    [0.8, 0.2]);

var selector = ChooseOneSelector.Create(
    [tournamentSelector, proportionalSelector],
    [0.6, 0.4]);
```

### Conditional application

`mutator.AppliedAtRate(mutationRate)` is a weighted choice between the mutator and `NoChangeMutator`. The decision is made independently for each candidate.

`crossover.AppliedAtRate(crossoverRate)` is a weighted choice between the crossover and `SelectFirstParentCrossover`. The decision is made independently for each parent group. Skipping crossover therefore returns the first parent candidate.

`refiner.AppliedAtRate(refinementRate)` is a weighted choice between the refiner and `NoChangeRefiner`. The decision is made independently for each candidate, which is the usual way to apply an expensive refinement to part of a population.

Rate controlled composition is intended for operations that should be applied conditionally. It is distinct from transformed composition, which always invokes its transformation step.

## Sequential composition

`PipelineMutator` passes the complete result batch from each mutator to the next mutator in order.

```csharp
var mutator = PipelineMutator.Create(firstMutator, secondMutator);
var fluentMutator = firstMutator.Then(secondMutator);
```

`PipelineRefiner` does the same for refiners. Ordering is semantically significant and a stage may appear more than once, so `repair`, `simplification`, `parameter fitting`, `simplification` is an ordinary configuration rather than a special case.

```csharp
var refiner = PipelineRefiner.Create(repair, simplification, parameterFitting);
var fluentRefiner = repair.Then(simplification, parameterFitting);
```

`PipelineInterceptor` similarly passes the transformed search state from each interceptor to the next interceptor.

```csharp
var interceptor = PipelineInterceptor.Create(firstInterceptor, secondInterceptor);
var fluentInterceptor = firstInterceptor.Then(secondInterceptor);
```

Selectors do not have a general pipeline because it is unclear whether a later selector should receive the original population or the previous selection and which count should apply at each stage. Replacers have the same ambiguity for the previous and offspring populations. A specialized pipeline may define those choices when an algorithm has a concrete use case.

A pipeline stage may change cardinality when the following stages and owning algorithm support it. Pipeline composition itself does not need to restore a relationship with the original input positions.

Creators do not form a natural pipeline because a creator does not consume candidates from another creator. Crossovers also consume parent groups rather than candidates produced by another crossover. Transformed composition covers the common postprocessing need for these roles.

## Refiner composition

### What refinement costs

Refinement is normally the most expensive part of an algorithm. In one genetic algorithm benchmark with 80 candidates, 30 generations and `NumericParameterFittingRefiner` at five iterations, the refiner used 96 to 99 percent of total run time across datasets with 200 to 20,000 rows. Enabling it made the run 45 to 120 times slower. Every other role used less than two percent.

Two consequences are worth carrying into a configuration. Optimizing anything else while refinement is enabled changes nothing measurable, and the setting that moves a run is how many candidates are refined at all: `refiner.AppliedAtRate(refinementRate)` is the usual lever, and a refiner's own iteration count is the next one. The exact numbers belong to one machine and one problem, but the order of magnitude is the point.

Repeated refinement is expressed through `IteratedRefiner` rather than by placing the same refiner at two lifecycle points. It applies its child exactly `Iterations` times, feeding each result back in and forking a random number generator per iteration. There is no early exit when an iteration leaves a candidate unchanged, because candidate equality is not generally meaningful and a fixed iteration count keeps the result reproducible.

```csharp
var refiner = IteratedRefiner.Create(parameterFitting, iterations: 5);
var fluentRefiner = parameterFitting.AsIterated(5);
```

### Improvement checking

An ordinary refiner returns whatever it produced, which is sometimes worse than what it started from: a local optimizer can diverge, a simplification can lose accuracy. `ImprovementCheckingRefiner` wraps any refiner and keeps its result only when it is an improvement.

```csharp
var refiner = parameterFitting.CheckedForImprovement();
```

```text
Evaluate the original candidate
→ Refine it
→ Evaluate the refined candidate
→ Return whichever one the criterion preferred
```

It remains an ordinary refiner returning a candidate, so it composes like any other. A refiner that cannot improve a candidate returns it unchanged, which means an unchanged objective vector, which the criterion reads as "not an improvement". A failed refinement therefore never produces a worse candidate, without the refiner contract needing a failure channel.

`Criterion` decides what counts as an improvement:

| Criterion                              | Keeps the refined candidate when                                                                     |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `Default`                              | It is strictly better by the problem's total objective order, or dominates where no order is defined |
| `StrictlyBetter`                       | It is strictly better by the total objective order                                                   |
| `NotWorse`                             | It is not worse by the total objective order, so an equally good result is taken                     |
| `Dominance`                            | It is at least as good on every objective and better on at least one                                 |
| `MinimumImprovement(delta)`            | Every objective improved by at least `delta`                                                         |
| `MinimumRelativeImprovement(fraction)` | Every objective improved by at least `abs(original) * fraction`                                      |

```csharp
parameterFitting.CheckedForImprovement(ImprovementChecking.MinimumImprovement(0.01))
```

Use a threshold when a marginal improvement is not worth keeping, for example when refinement makes a candidate harder to interpret or when tiny numeric gains are noise. The margin is applied in each objective's own direction, so a positive threshold always means "better by at least this much" whether the objective is minimized or maximized. Thresholds are retained exactly as configured: zero accepts anything not worse, and a negative value deliberately tolerates a bounded worsening.

A criterion is deliberately not an `IComparer<ObjectiveVector>`. It answers whether the refined result is better than the original rather than defining an ordering, so it can apply a margin in the correct objective direction. Implement `IImprovementCriterion` when acceptance depends on one dimension of a multiobjective vector while selection continues to use the whole vector.

Nesting decides where acceptance happens, and the two orders are genuinely different searches rather than two spellings of one:

```csharp
new IteratedRefiner<...>(parameterFitting.CheckedForImprovement(), 5)   // memetic hill climb
parameterFitting.AsIterated(5).CheckedForImprovement()                  // accept the final result once
```

A refinement that has to pass through a worse candidate to reach a better one is rejected by the first, because the uphill step never survives its round, and kept by the second, which judges only the final result.

### Evaluation accounting for refinement

Improvement checking evaluates twice, and the algorithm then evaluates the returned candidate itself, so the naive configuration costs three problem evaluations per refined candidate where an unchecked refiner costs one. That cost is deliberately visible rather than hidden, and it is controlled by one thing: whether the refiner and the algorithm hold the **same evaluator object**.

Execution instances are resolved by reference identity, so one evaluator object resolves to one execution instance, and therefore to one counter and one cache. Two separately constructed but structurally identical configurations resolve to two independent instances.

The snippets in this section need four namespaces:

```csharp
using HEAL.HeuristicLib.Algorithms;   // TerminatedBy, LimitedToEvaluatedCandidates
using HEAL.HeuristicLib.Analysis;     // ObservationCounter
using HEAL.HeuristicLib.Operators;    // ProblemEvaluator, CachingEvaluator, CheckedForImprovement, LimitEvaluations
using HEAL.HeuristicLib.Operators.Evaluators; // CountCandidates
```

`CountCandidates` sits one namespace deeper than the helpers it chains onto, because instrumentation is declared beside the role it instruments. It is the one import in this list that is not guessable from the snippet.

Algorithm configurations are records with `init` properties, so a slot is filled at construction or through `with`, never by assignment to an existing instance. Every snippet below therefore produces a new configuration rather than mutating one.

By default a refiner uses its own plain `ProblemEvaluator`, which is unwrapped and therefore invisible to counting and analysis:

```csharp
var unshared = algorithm with
{
    Evaluator = new ProblemEvaluator<...>().CountCandidates(out var counter),
    Refiner = parameterFitting.CheckedForImprovement()
};
```

The counter sees only the algorithm's own evaluations here; refinement effort is not measured. Sharing the counting evaluator brings it in:

```csharp
var evaluator = new ProblemEvaluator<...>().CountCandidates(out var counter);

var shared = algorithm with
{
    Evaluator = evaluator,
    Refiner = parameterFitting.CheckedForImprovement(evaluator)
};
```

All three evaluations now increment one counter, so it measures total evaluation effort including refinement.

That counter is what an algorithm terminator reads to stop the run. `AfterOperatorCountTerminator` observes it directly:

```csharp
var evaluator = new ProblemEvaluator<...>().CountCandidates(out var counter);

var budgeted = (algorithm with
    {
        Evaluator = evaluator,
        Refiner = parameterFitting.CheckedForImprovement(evaluator)
    })
    .TerminatedBy(AfterOperatorCountTerminator.For(problem, counter, maximumCount: 100_000));
```

The refiner's comparison evaluations increment the same counter that ends the run, so termination reflects the true cost of the search including refinement. Leave the refiner on its inherited default and the opposite holds: it holds a different evaluator instance, its evaluations never reach the counter, and refinement never shortens the run. Both behaviours are intentional, and the only thing that decides between them is whether the same evaluator object appears in both places.

`algorithm.LimitedToEvaluatedCandidates(evaluator, 100_000)` is the shorthand for the same arrangement. It installs a counting replacement for the evaluator instance it observes and attaches the matching terminator. Replacements are also resolved by reference, so a refiner holding that instance resolves the counted version too and the accounting works out identically.

Several observed operators can feed the same termination criterion by sharing one `ObservationCounter`. For example, the same counter can track refiner calls and evaluations.

Sharing a `CachingEvaluator` pays for two evaluations rather than three, because the algorithm's own evaluation of the returned candidate is already in the cache:

```csharp
var evaluator = new CachingEvaluator<...>(new ProblemEvaluator<...>(), keySelector);

var cached = algorithm with
{
    Evaluator = evaluator,
    Refiner = parameterFitting.CheckedForImprovement(evaluator)
};
```

Where counting sits relative to the cache decides what the counter measures:

```csharp
new CachingEvaluator<...>(problemEvaluator, keySelector).CountCandidates(out var requests) // cache hits count
new CachingEvaluator<...>(problemEvaluator.CountCandidates(out var solves), keySelector)   // cache hits do not count
```

The first counts evaluation requests, the second counts actual problem evaluations. Both are legitimate; documentation for a preset should state which one it chose.

`LimitEvaluator` follows the same rule, and there the choice decides when a run stops rather than what a number reads:

```csharp
new CachingEvaluator<...>(problemEvaluator, keySelector).LimitEvaluations(100_000) // cache hits consume budget
new CachingEvaluator<...>(problemEvaluator.LimitEvaluations(100_000), keySelector) // cache hits do not consume budget
```

With the limit outside, the budget means evaluation requests, so a refiner re-evaluating a candidate the cache already knows still spends from it. With the cache outside, the request never reaches the limit and the budget means actual problem evaluations.

To attribute refinement effort separately instead of folding it into one counter, give the refiner its own counted evaluator:

```csharp
var attributed = algorithm with
{
    Refiner = parameterFitting.CheckedForImprovement(
        new ProblemEvaluator<...>().CountCandidates(out var refinementCounter))
};
```

### Refinement evaluation

`RefinementEvaluator` composes a refiner into the evaluator side instead. It refines the candidates, evaluates the refined copies through its evaluator and returns those objective vectors for the candidates that were supplied. The refined candidates are transient: they are discarded when the evaluation returns and are never written back into the population.

```csharp
var evaluator = RefinementEvaluator.Create(parameterFitting);
var sharedEvaluator = RefinementEvaluator.Create(parameterFitting, algorithm.Evaluator);
var fluentEvaluator = algorithm.Evaluator.AppliedAfterRefinement(parameterFitting);
```

This is Baldwinian refinement, because the refinement influences fitness without becoming part of the candidate: a candidate is credited with what it could reach through refinement while the population keeps the unrefined genotype. Configuring the same refiner as the algorithm's refiner makes it Lamarckian, because the algorithm then continues with the refined candidate.

`RefinementEvaluator` compares nothing and evaluates once. Its `Evaluator` setting follows the sharing rules above.

## Transformed creators and crossovers

`TransformedCreator` invokes a creator and then always invokes one mutator on the created candidate batch.

`TransformedCrossover` invokes a crossover and then always invokes one mutator on the resulting candidate batch.

```csharp
var creator = TransformedCreator.Create(baseCreator, repairMutator);
var fluentCreator = baseCreator.TransformWith(repairMutator);

var crossover = TransformedCrossover.Create(baseCrossover, repairMutator);
var fluentCrossover = baseCrossover.TransformWith(repairMutator);
```

The mutator is invoked once for every outer operation call, even when it performs no changes. A rate controlled or weighted mutator may be supplied deliberately, but users should normally read transformed composition as unconditional postprocessing.

Transformed composition does not require the mutator to preserve cardinality. The owning algorithm remains responsible for accepting the resulting batch shape.

## Terminator composition

`AnyTerminator` stops when any child terminator stops. `AllTerminator` stops when every child terminator stops.

Use the static factories when the resulting composition type should be prominent. Use `Or(...)` and `And(...)` when the composition reads more clearly from its first condition.

```csharp
var any = AnyTerminator.Create(iterationTerminator, targetTerminator);
var fluentAny = iterationTerminator.Or(targetTerminator);

var all = AllTerminator.Create(iterationTerminator, targetTerminator);
var fluentAll = iterationTerminator.And(targetTerminator);
```

Terminator checks may update execution data. Each composition invokes its child terminators according to ordinary `Any` and `All` short circuit behavior. Child order can therefore matter when terminators have effectful checks.

For an empty child collection, `AnyTerminator` returns false and `AllTerminator` returns true.

See [Operators](/guide/fundamentals/operators#terminator-ownership) for ownership and lifecycle semantics.

## Specialized wrappers

Some compositions express a role specific policy rather than a general choose one or pipeline pattern.

1. `PredefinedCandidatesCreator` emits configured candidates before delegating remaining requests to a fallback creator.
2. Evaluator wrappers provide caching, repeated evaluation and evaluation limits. These policies rely on objective vectors remaining aligned with their input candidates.
3. Selector wrappers provide policies such as elite inclusion and avoiding equal mates.
4. `GenderSpecificSelector` requests half of the candidates from its female selector and half from its male selector, then returns consecutive female and male pairs.
5. Domain specific operators may provide narrower composition, such as selecting among symbolic expression tree mutations.

Specialized selector compositions also provide static and fluent construction:

```csharp
var predefinedCreator = PredefinedCandidatesCreator.Create(predefinedCandidates, fallbackCreator);
var fluentPredefinedCreator = fallbackCreator.SeededWith(predefinedCandidates);

var eliteSelector = EliteSelector.Create(selector, elites: 2);
var fluentEliteSelector = selector.CombinedWithElites(elites: 2);

var pairedSelector = GenderSpecificSelector.Create(femaleSelector, maleSelector);
var fluentPairedSelector = femaleSelector.PairWith(maleSelector);

var distinctMateSelector = NoSameMatesSelector.Create(selector, maximumAttempts: 10);
var fluentDistinctMateSelector = selector.AvoidSameMates(maximumAttempts: 10);
```

These types should document their own batching, state and cardinality behavior because their policies are role specific.

## Observation and instrumentation boundaries

Observable, counted and measured operators are also wrappers. Their placement determines which work they observe.

Every operator role provides public `Observable*`, `Counting*` and `DurationMeasuring*` configurations. Prefer the fluent observation, counting and duration methods because they express the wrapper at the point of composition with less type noise. They return the concrete wrapper types, while direct construction remains available for advanced cases that benefit from naming the instrumentation configuration explicitly.

Wrapping a complete composition observes the outer operation boundary and includes all nested work. Wrapping one child observes only calls that reach that child. For example, measuring a `TransformedCrossover` includes both crossover and mutation work, while measuring its child crossover includes only crossover work.

See [Observability and analysis](/guide/execution/observability-and-analysis) for observer, counter and duration APIs.

## Specialized execution instance capabilities

General wrappers and compositions currently preserve the operator role contract, but they do not automatically preserve additional execution instance capabilities. In particular, wrapping or composing an `IVariableStrengthMutator` such as `GaussianMutator` currently exposes an ordinary `IMutatorInstance` at the outer boundary.

`EvolutionStrategy` adapts mutation strength only when its resolved mutator instance implements `IVariableStrengthMutatorInstance`. Placing an observable, counting, duration measuring or other general mutator wrapper around a variable strength mutator therefore currently disables that adaptation. The wrapped mutator continues to use its configured mutation strength.

This is a known limitation. Avoid wrapping an adaptive mutator when the evolution strategy must retain mutation strength adaptation. A future design must preserve specialized run scoped capabilities through composition without requiring every general wrapper to contain role specific type checks.

## Randomness and execution instances

Compositions whose role receives an explicit random number generator invoke children in the order defined by their policy. Random draw order is therefore part of reproducible behavior. Adding, removing or reordering child operators may change later random draws even when the same root seed is used. Interceptors and terminators do not receive a random number generator, so their built in compositions are deterministic with respect to child ordering unless a child depends on some other explicit input or external resource.

Child configurations are resolved once for each composition execution instance. Reusing the same child configuration elsewhere in the same execution instance registry reuses the same child execution instance and its execution data.

See [Reproducible randomness](/guide/execution/randomness) and [Running algorithms](/guide/execution/running-algorithms) for the underlying models.

## Authoring a composition

Use the role specific `Wrapping*` base when a composition coordinates one child of the same role. Use the role specific `Multi*` base when it coordinates several children of the same role.

Derive directly from the unprefixed role base when a composition coordinates different roles or needs a custom execution structure. `TransformedCreator` and `TransformedCrossover` follow this path because each coordinates both a source operator and a mutator.

The authored execution instance owns resolved child instances and mutable execution data. The reusable configuration owns parameters and child configurations.
