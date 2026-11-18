# Operator composition

::: info Advanced extension
Start with [built-in operators](/guide/fundamentals/operators). Compose operators when several reusable policies should behave as one operator in an algorithm configuration.
:::

Operator composition builds a new operator configuration from one or more existing operator configurations. The resulting configuration remains reusable and resolves its child executors as part of the execution graph.

Composition policies describe how child operations are coordinated. `WrappingOperator` and `MultiOperator` are authoring infrastructure for implementing those policies. They do not prescribe behavior by themselves.

The built in composition overview is:

| Composition aspect                   | Coordination semantics                                                                                        | Helpers and examples                                                                                        | Applicability                                                |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| Weighted alternatives per item       | Chooses a child independently for each item of a call, invokes grouped child batches and restores input order | `ChoosePerItem`                                                                                             | Creators, crossovers, mutators, evaluators and refiners      |
| Weighted alternatives per invocation | Chooses one child for the complete operation invocation                                                       | `ChoosePerInvocation`                                                                                       | Every operation-parameterized operator                       |
| Conditional application              | Chooses between an operation and role specific unchanged behavior                                             | `mutator.AppliedAtRate(...)`, `crossover.AppliedAtRate(...)`, `refiner.AppliedAtRate(...)`                  | Mutators, crossovers and refiners                            |
| Sequential composition               | Passes each stage result to the next stage in order                                                           | `PipelineMutator`, `PipelineRefiner`, `PipelineInterceptor`                                                 | Mutators, refiners and interceptors                          |
| Repeated composition                 | Feeds a result back into the same child for a configured number of iterations                                 | `IteratedRefiner`                                                                                           | Refiners                                                     |
| Objective-aware retention            | Measures a child's result and keeps it only when a criterion counts it as an improvement                      | `ImprovementCheckingRefiner`                                                                                | Refiners                                                     |
| Transient refinement evaluation      | Refines candidates temporarily, measures the refined copies and discards them                                 | `RefinementEvaluator`                                                                                       | Evaluators composed with a refiner                           |
| Unconditional postprocessing         | Invokes a source operation and always transforms its complete result                                          | `TransformedCreator`, `TransformedCrossover`                                                                | Creators and crossovers with a mutator transformation        |
| Logical composition                  | Combines child conditions with short circuit Boolean logic                                                    | `AnyTerminator`, `AllTerminator`                                                                            | Terminators                                                  |
| Fallback composition                 | Handles part of a request directly and delegates the remainder                                                | `PredefinedCandidatesCreator`                                                                               | Creators                                                     |
| Specialized coordination             | Applies a policy whose meaning depends on one operator contract                                               | Caching, repetition and evaluation limits, elite inclusion, mate restrictions and gender specific selection | Evaluators and selectors                                     |
| Inspection                           | Wraps a selected execution boundary to observe, count or measure calls                                        | `Observing`, source counters and source durations                                                           | Role-specific observations; counting and timing across roles |

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

`ChoosePerItem<TOperation, TCandidate>` selects a child independently for each item of a call: each requested candidate of a creation, each parent group of a crossover and each candidate of a mutation, evaluation or refinement. Items assigned to the same child are passed to that child as one call. Results are then restored to the original item order.

This order restoration requires every selected child to return exactly one result for each assigned item, so a child creator returns the assigned count and every other child preserves its assigned batch size. Selection, replacement, termination and interception calls have no independent items. Their operations are not marked with `IItemOperation<TSelf>`, so `ChoosePerItem` does not compile for them.

Weights are retained exactly as configured. Omitting weights selects uniformly across all children. Positive finite weights participate proportionally; zero, negative finite weights, negative infinity and `NaN` mean never. Positive infinity overrides finite weights, with multiple positive-infinity children selected uniformly. If no child is selectable, selection falls back to uniform across all children. Configured weights are compiled once for repeated sampling rather than normalized or recalculated for every choice.

`ChoosePerInvocation<Selection, TCandidate>` and `ChoosePerInvocation<Replacement, TCandidate>` make one weighted choice for each complete selection or replacement call. The selected child receives the complete population inputs and requested count. This preserves the meaning of a selector or replacer policy as a decision over a complete population instead of mixing fragments from several policies.

`ChoosePerInvocation` makes the same per-invocation choice for any operator using `IOperator<TOperation, TCandidate>`, including mutation, evaluation and operations defined outside the library. Each call samples one child with the call's random number generator and runs on that child with the same generator, unchanged inputs and the run's search space and problem. Use it when a batch should stay with one policy, such as evaluating a complete batch with either an exact or a surrogate evaluator, and use `ChoosePerItem` when each item should choose independently. Its weights follow the rules above, and building its executor rejects an empty child collection or a weight count that differs from the child count.

```csharp
var creator = ChoosePerItem.Create(
    [uniformCreator, normalCreator],
    [0.75, 0.25]);

var mutator = ChoosePerItem.Create(
    [smallMutation, largeMutation],
    [0.8, 0.2]);

var selector = ChoosePerInvocation.Create(
    [tournamentSelector, proportionalSelector],
    [0.6, 0.4]);

var evaluator = ChoosePerInvocation.Create(
    [exactEvaluator, surrogateEvaluator],
    [0.1, 0.9]);
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

Improvement checking evaluates twice and the algorithm evaluates the returned candidate again. Without caching this costs three problem evaluations per refined candidate. Use the same evaluator configuration to select all those calls for observation and, within one scope, to use one cache or evaluation allowance.

Configuration reference identity selects observation targets and executors. Reusing the evaluator configuration for the same problem within one scope gives both uses one executor and its private fields, including its cache. The algorithm and improvement check resolve their evaluators in that same scope. Use `Sharing(evaluator)` on an enclosing composition to retain the executor across child scopes, such as Cycle turns.

The snippets in this section use these namespaces:

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
```

Counters and durations are configuration nodes that place nothing. A source creates their placement substitution, selecting the evaluator by reference and collecting into one result per run:

```csharp
var evaluator = algorithm.Evaluator;
var counter = new Counter();
var unshared = algorithm with
{
    Refiner = parameterFitting.CheckedForImprovement()
};
var run = unshared.CreateRun(problem, random, [evaluator.Invocations.CountingCandidates(counter)]);
```

The improvement check uses its own evaluator configuration here, so the counter measures only the algorithm's evaluations. Pass the selected evaluator to the improvement check to include comparison work:

```csharp
var evaluator = algorithm.Evaluator;
var counter = new Counter();
var shared = algorithm with
{
    Refiner = parameterFitting.CheckedForImprovement(evaluator)
};
var run = shared.CreateRun(problem, random, [evaluator.Invocations.CountingCandidates(counter)]);
```

Each refined candidate causes two comparison evaluations and the algorithm's subsequent evaluation. All three reach the selected evaluator and increment the same counter.

An external terminator reads the same run-bound counter definition. Read its value with `run.Result(counter).CurrentCount`:

```csharp
var budgeted = shared.TerminatedBy(
    AfterOperatorCountTerminator.For(problem, counter, maximumCount: 100_000));
var run = budgeted.CreateRun(problem, random, [evaluator.Invocations.CountingCandidates(counter)]);
```

`shared.LimitedToCandidates(evaluator, 100_000)` places a candidate counter around the selected evaluator inside the budgeted algorithm, and stops the algorithm once that count reaches the limit. Comparison work counts when the refiner holds the selected evaluator. A different evaluator is outside that selection. The budget is checked after a yielded state, so a batch or iteration can take the count past the limit. A nonpositive limit stops after the first state.

The same budgets apply to any operator, whatever its operation:

```csharp
var calls = shared.LimitedToInvocations(shared.Refiner!, 1_000);
var timed = shared.LimitedToDuration(shared.Evaluator, TimeSpan.FromSeconds(10));
```

`LimitedToInvocations` counts successful calls, and `LimitedToCandidates` counts their candidate work: input candidates for evaluation and returned candidates for candidate-producing operations. Timing uses the run's time provider and also records failed calls. The operator is matched by reference, and it is an error when the algorithm does not hold it. For an operator implementing several operations, pass it through a variable of the operation's operator type. With `CreateRun`, apply budgets first and name analysis targets from the budgeted configuration. To exclude observer time from a duration budget, place analysis with `Substitute` before applying that budget.

Pass a placement to combine work from several operators in one budget. It receives the algorithm and the budget's counter or duration, places the wrappers, and returns the algorithm to limit:

```csharp
var combined = algorithm.LimitedToCount(1_000, (ga, calls) => ga.Substitute(
    ga.Mutator.Invocations.Counting(calls),
    ga.Crossover.Invocations.Counting(calls)));
var timed = algorithm.LimitedToDuration(TimeSpan.FromSeconds(10), (ga, time) => ga.Substitute(
    ga.Mutator.Invocations.Timing(time),
    ga.Crossover.Invocations.Timing(time)));
```

The placement runs during the call and is not stored. Each `Counting` states what it counts, so `ga.Evaluator.Invocations.CountingCandidates(calls)` adds evaluated candidates where the default adds successful calls. Each `Counting` or `Timing` places one wrapper, so naming a target twice counts it twice, and an operator implementing several operations takes one call for each operation to count.

The counter or duration stays inside the budget and restarts each time the budget is built, such as in each Cycle turn, while a counter placed outside the budget with `Invocations.Counting(counter)` spans the run. Duration is the sum of invocation durations, including overlapping and nested time, rather than wall clock time for the algorithm. Repeated runs remain independent.

Budgets are ordinary configurations that hold their algorithm with the wrappers in place:

```csharp
var constructed = new OperatorCountBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
    algorithm.Counting(algorithm.Mutator, out var mutations), mutations, 1_000);
```

`OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>` likewise takes the algorithm, the `Duration` its wrappers add to, and the limit. Because the budget holds a copy of the algorithm, a separate run of the original algorithm neither counts against the budget nor shares its executors.

Reusing the cached evaluator configuration gives the algorithm and the improvement check one executor and cache within their scope, so the algorithm's evaluation of the returned candidate is a cache hit:

```csharp
var evaluator = new ProblemEvaluator<RealVector>().Cached();

var cached = algorithm with
{
    Evaluator = evaluator,
    Refiner = parameterFitting.CheckedForImprovement(evaluator)
};
```

To retain that cache across child scopes, such as Cycle turns, apply `Sharing(evaluator)` to the enclosing composition.

`EvaluationCache<TKey>` uses MemoryCache for concurrent storage, key equality and eviction, with an optional entry count limit. Lookup, evaluation and publication are separate operations, so concurrent misses may evaluate the same key more than once. Share only when keys and evaluation semantics agree across every use. Child evaluators, key selectors and other dependencies must support the concurrency the author permits. A cache cannot make incompatible problems, parameter settings or stochastic results interchangeable.

When writing an operator whose executor may be shared, use concurrency safe operations on its data or synchronize the complete invariant you need to preserve. Every participant must use the same synchronization object. A lock must cover every path to the data. Prefer keeping synchronization inside the data structure itself. A cache with epoch metadata, for example, must coordinate invalidation and result publication with that epoch. Choose a simple lock, a read/write lock or concurrent storage according to those requirements. Avoid holding a shared lock across expensive child evaluation when validity can instead be checked before publication. Safe storage alone does not guarantee one evaluation per key or a strict shared evaluation budget.

Select the outer cache configuration to count evaluation requests and its child evaluator to count actual problem evaluations:

```csharp
var problemEvaluator = new ProblemEvaluator<RealVector>();
var cache = problemEvaluator.Cached();
var requests = new Counter();
var solves = new Counter();

var cached = algorithm with { Evaluator = cache };
var run = cached.CreateRun(problem, random, [cache.Invocations.CountingCandidates(requests), problemEvaluator.Invocations.CountingCandidates(solves)]);
```

After the run, `run.Result(requests).CurrentCount` includes cache hits and `run.Result(solves).CurrentCount` excludes them. To include refinement comparisons, give `CheckedForImprovement` the same `cache` configuration; both uses reach one cache within their scope. Use `Sharing(cache)` on the enclosing composition to retain it across child scopes. Counting selects an existing configuration; it does not return an evaluator that can be placed inside the cache.

`LimitEvaluator` accounting is private to its executor. Reuse the limited evaluator configuration when the refiner and algorithm need one allowance within their scope. Use `Sharing(limited)` on the enclosing composition to retain that allowance across child scopes. Its separate check, evaluation and increment do not promise a strict limit under concurrent use. Cache and limit ordering determines whether hits consume allowance:

```csharp
new CachingEvaluator<...>(problemEvaluator, keySelector).LimitEvaluations(100_000) // cache hits consume budget
new CachingEvaluator<...>(problemEvaluator.LimitEvaluations(100_000), keySelector) // cache hits do not consume budget
```

With the limit outside, the budget means evaluation requests, so a refiner re-evaluating a candidate the cache already knows still spends from it. With the cache outside, the request never reaches the limit and the budget means actual problem evaluations.

To attribute refinement effort separately instead of folding it into one counter, give the refiner its own evaluator and count that evaluator:

```csharp
var refinementEvaluator = new ProblemEvaluator<RealVector>();
var refinementCount = new Counter();
var attributed = algorithm with
{
    Refiner = parameterFitting.CheckedForImprovement(refinementEvaluator)
};
var run = attributed.CreateRun(problem, random, [refinementEvaluator.Invocations.CountingCandidates(refinementCount)]);
```

`run.Result(refinementCount).CurrentCount` measures the improvement check's comparison evaluations. The algorithm's own evaluator is a different configuration, so its calls do not contribute to that count.

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

See [Termination](/guide/glossary#termination) for algorithm-owned termination and external early stopping.

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

## Observation and inspection boundaries

Source counters, durations, analyzers and `Observing`, attached with `Substitute`, add `Counted`, `Timed` or `Observed` wrappers around the configurations they select.

Selecting a complete composition observes its outer operation boundary and includes nested work. Selecting one child observes only calls reaching that child. For example, measuring a `TransformedCrossover` includes crossover and mutation work, while measuring its child crossover includes only crossover work.

Counters and durations keep their results in the run that holds them, so independent runs using the same definition measure separately. Counters increment after a successful child call. Duration measurement keeps timestamps local to each invocation and records elapsed time even when the child throws.

A named configuration such as `mutator.Named("exploration")` supplies a selectable wrapper boundary while forwarding the child's executor. A predicate such as `node is INamedConfiguration { Name: "exploration" }` selects that wrapper. A selection by role type may select both the wrapper and its child.

See [Observability and analysis](/guide/execution/observability-and-analysis) for observation, counter, duration and selection APIs.

## Adaptable parameters

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/AdaptationDocumentationSpecs.cs#adaptation-wiring{csharp}

`GaussianMutator.MutationStrength` remains the configured base value. Its optional `MutationStrengthAdaptation` child is an `IConfigurationNode<IAdaptation>`: the executor resolves it and calls `Apply(baseValue)` once per mutation invocation. `ScaleAdaptation` starts with factor 1 and `OffsetAdaptation` with offset 0. A rule writes their runtime objects through the settable `Factor` and `Offset` properties.

The strategy's slot is `ScaleAdaptation?`, the form its 1/5 success rule writes. Both slots must name the same instance. An unset strategy slot means no adaptation. Wrappers forward nothing: `Timed`, `Counted`, `PipelineMutator` and `ChoosePerInvocation` can surround mutators that read the same link. Several mutators can read one adaptation with different base values. External nodes may implement `IConfigurationNode<IAdaptation>` directly, including as records.

A scope resolves one runtime adaptation per configuration reference. Independent runs remain independent. To retain adaptation across Cycle turns, share the adaptation on the Cycle. Sharing the mutator also requires sharing the adaptation: otherwise the retained mutator reads its original adaptation while each fresh strategy writes a different one. The resolver does not detect unused links.

`Retry(3)` includes the first attempt. It catches only a directly thrown `RetryableOperationException`; cancellation, ordinary failures and aggregate exceptions propagate. Every attempt receives the same arguments, child and RNG. Failed attempts retain their random draws, state changes and inspection effects; retry provides no rollback.

## Randomness and executors

Every operation call receives an explicit random number generator, and compositions invoke children in the order defined by their policy. Random draw order is therefore part of reproducible behavior. Adding, removing or reordering child operators may change later random draws even when the same root seed is used. The built in terminators and interceptors draw nothing, so `AnyTerminator`, `AllTerminator` and `PipelineInterceptor` are deterministic with respect to child ordering unless a child draws or depends on some other explicit input or external resource.

Child configurations resolve when each composition executor is built. Each construction has independent private fields. Sharing a configuration gives all its uses within the sharing node one executor.

See [Reproducible randomness](/guide/execution/randomness) and [Running algorithms](/guide/execution/running-algorithms) for the underlying models.

## Authoring a composition

Use `WrappingOperator<TOperation, TCandidate>` for one child and `MultiOperator<TOperation, TCandidate>` for several.

Derive from `Operator<TOperation, TCandidate>` when a composition coordinates different operations or declares children with specific domain responsibilities. `TransformedCreator` and `TransformedCrossover` follow this path because each coordinates both a source operator and a mutator.

The authored executor holds resolved children and private mutable fields. The configuration owns parameters and child configurations.

The two bases declare their children. A composition deriving from `Operator<TOperation, TCandidate>` declares its own, as [the validation guide](/guide/fundamentals/invariants-and-validation#a-composition-answers-for-what-it-resolves) shows, because pre-flight validation and substitution reach a child only through its declaration.

### A complete stateful composition

This `RoundRobinMutator` sends each complete batch to the next child in a fixed order. Unlike weighted selection, it remembers which child should run next. Its cursor advances only after a successful child call, including a successful empty batch.

Use these imports in a C# project with implicit system usings enabled:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-type{csharp}

The multi base snapshots the configured child collection. `CreateExecutor` validates it, resolves current children through the typed scope and constructs an executor with its own cursor field. Every separate construction starts at the first child.

The executor retains its contextual children and cursor. `offspring` is local to one invocation. This example expects sequential calls on one executor. Authors must coordinate any concurrent calls that mutate the same fields.

#### Construct another observation context

Two distinct no-change mutators make the selection order visible through counters without changing candidate values:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-usage{csharp}

The results read through `observedScope.Results.Result(firstCalls)` and `observedScope.Results.Result(secondCalls)` have counts one and zero. The observed executor starts at the first child. The original executor continues to its second child with its original unobserved children.

An independent root prepares a fresh cursor:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-independent{csharp}

`independentScope.Results.Result(independentFirstCalls).CurrentCount` is one. A fresh child scope also creates private fields for configurations that are not shared. See [Execution resolution](/contributing/architecture/execution-resolution#results-and-construction).

### Author a concurrent cache wrapper

This executable example keeps an `EvaluationCache<int>` in its executor. MemoryCache provides concurrent storage and eviction, with an optional entry count limit. The wrapper keeps its child executor private and evaluates outside cache storage operations. This protocol allows duplicate evaluations on overlapping misses. It assumes immutable keys and deterministic evaluation with compatible semantics for every use of the shared executor. Cache admission is not guaranteed at capacity, and eviction order is not part of the evaluator contract.

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/ResourceCachingDocumentationSpecs.cs#resource-cache-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/ResourceCachingDocumentationSpecs.cs#resource-cache-authoring{csharp}

The following usage shares the wrapper, so both resolutions receive one executor and one cache, and calls it concurrently. Separate RNGs belong to the separate calls. `Substitute` places the count on the child evaluator in the configuration, so the shared executor counts through it.

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/ResourceCachingDocumentationSpecs.cs#resource-cache-usage{csharp}

Sharing does not synchronize calls. The operator author remains responsible for concurrent operations on the executor's data, child calls and key selection. Keep synchronization with the data, or ensure every path to it uses the same synchronization object.

The experimental dynamic cache additionally coordinates entries, epoch and consecutive hit accounting under one shared lock. It rechecks the epoch before publication and never publishes results from an earlier epoch. Once the hit threshold is reached, it requests at most one update per observed epoch. A miss resets the hit streak but does not queue another request while an update is pending. Key and evaluation compatibility still belongs to the author.

Executors have no disposal lifecycle, so keep only data that needs no deterministic cleanup in executor fields. `EvaluationCache<TKey>` uses MemoryCache with statistics and linked entry tracking disabled and without expiration tokens or callbacks, so it requires none.
