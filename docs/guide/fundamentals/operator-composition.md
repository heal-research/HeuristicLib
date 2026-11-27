# Operator composition

Operator composition builds a new operator configuration from one or more existing operator configurations. The resulting configuration remains reusable and resolves its child executors as part of the execution graph.

Composition policies describe how child operations are coordinated. `WrappingOperator` and `MultiOperator` are authoring infrastructure for implementing those policies and do not prescribe behavior by themselves. [Write a composition](/extend/writing-compositions) covers them.

The built in composition overview is:

| Composition                          | Coordination semantics                                                                                        | Helpers and examples                                                                                        | Applicability                                                |
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
| Operator work budgets                | Stops an algorithm when the counted or timed work of selected operators reaches a limit                       | `LimitedToInvocations`, `LimitedToCandidates`, `LimitedToCount`, `LimitedToDuration`                        | Algorithms                                                   |
| Inspection                           | Wraps a selected execution boundary to observe, count or measure calls                                        | `Observing`, source counters and source durations                                                           | Role-specific observations. Counting and timing across roles |

<CompositionTrees>

Four compositions as trees. The composed configuration sits on top and holds its children as parameters.

</CompositionTrees>

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

Weights are retained exactly as configured. Omitting weights selects uniformly across all children. Positive finite weights participate proportionally. Zero, negative finite weights, negative infinity and `NaN` mean never. Positive infinity overrides finite weights, with multiple positive-infinity children selected uniformly. If no child is selectable, selection falls back to uniform across all children. Configured weights are compiled once for repeated sampling rather than normalized or recalculated for every choice.

`ChoosePerInvocation<Selection, TCandidate>` and `ChoosePerInvocation<Replacement, TCandidate>` make one weighted choice for each complete selection or replacement call. The selected child receives the complete population inputs and requested count. This preserves the meaning of a selector or replacer policy as a decision over a complete population instead of mixing fragments from several policies.

`ChoosePerInvocation` makes the same per-invocation choice for any operator using `IOperator<TOperation, TCandidate>`, including mutation, evaluation and operations defined outside the library. Each call samples one child with the call's random number generator and runs on that child with the same generator, unchanged inputs and the run's search space and problem. Use it when a batch should stay with one policy, such as evaluating a complete batch with either an exact or a surrogate evaluator, and use `ChoosePerItem` when each item should choose independently. Its weights follow the rules above, and building its executor rejects an empty child collection or a weight count that differs from the child count.

<ChoiceGranularity>

One mutation call of six candidates with two children, A and B. `ChoosePerItem` draws for each candidate and calls each chosen child once. `ChoosePerInvocation` draws once for the call.

</ChoiceGranularity>

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

A criterion answers one question: is the refined objective vector an improvement on the original? Implement `IImprovementCriterion` when none of the built-in criteria expresses your acceptance rule. This criterion accepts a refinement only when the first objective improves by at least a configured margin, while selection continues to rank by the whole objective vector:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/ImprovementCriterionDocumentationSpecs.cs#improvement-criterion-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/ImprovementCriterionDocumentationSpecs.cs#improvement-criterion-type{csharp}

Pass it to `CheckedForImprovement` like a built-in criterion. A criterion is a plain decision over objective vectors, so it can also be called directly, for example in a test:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/ImprovementCriterionDocumentationSpecs.cs#improvement-criterion-usage{csharp}

Apply the margin in the objective's direction, as the built-in criteria do, so that a positive margin always means "better by at least this much".

Nesting decides where acceptance happens, and the two orders run different searches:

```csharp
IteratedRefiner.Create(parameterFitting.CheckedForImprovement(), iterations: 5)  // memetic hill climb
parameterFitting.AsIterated(5).CheckedForImprovement()                           // accept the final result once
```

A refinement that has to pass through a worse candidate to reach a better one is rejected by the first, because the uphill step never survives its round, and kept by the second, which judges only the final result.

### Evaluations of a refined candidate

Improvement checking evaluates the original candidate and the refined one, and the algorithm then evaluates the candidate it receives. Without a cache this costs three problem evaluations for every refined candidate. Give the improvement check the evaluator configuration the algorithm uses. A scope builds one executor for that configuration, so the algorithm and the check share its cache, its counters and its evaluation allowance, and the algorithm's evaluation of the returned candidate is a cache hit. [Sharing executors](/guide/fundamentals/configurations#sharing-executors) explains how to keep that executor across child scopes, such as Cycle turns.

This example runs one generation of four distinct predefined candidates. It shares one cache between the algorithm and its improvement check, and it counts the evaluation requests and the evaluations that reach the problem. Use these imports in a C# project with implicit system usings enabled, and run the code from an async method that receives your host application's `cancellationToken`:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/SharedCacheDocumentationSpecs.cs#shared-cache-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/SharedCacheDocumentationSpecs.cs#shared-cache{csharp}

`cache.Invocations.CountingCandidates(requests)` counts every candidate that the algorithm and the improvement check ask the cache to evaluate, hits included. The check evaluates the four candidates before and after refinement, and the algorithm evaluates them again, so `evaluationRequests` is twelve. `problemEvaluator.Invocations.CountingCandidates(solves)` counts only the candidates that reach the problem, so `problemEvaluations` is four: each distinct candidate misses once and the other eight requests are hits. Two caches, one for the algorithm and one for the check, would send eight candidates to the problem, because each cache misses once for every candidate. A counter selects an existing configuration. It does not return an evaluator that could be placed inside the cache. [Count, time and observe an operator](/guide/execution/observability-and-analysis#count-time-and-observe-an-operator) shows how to place counters, and [A concurrent cache wrapper](/extend/writing-compositions#a-concurrent-cache-wrapper) shows how to write a cache of your own.

To attribute the refinement effort separately instead, give the improvement check an evaluator configuration of its own and count that one. The algorithm's evaluator is a different configuration, so its calls do not contribute to that count.

`LimitEvaluator` keeps its allowance in its executor, so one limited evaluator configuration lets the algorithm and the check draw from one allowance. Its check, evaluation and increment are separate steps, which do not promise a strict limit under concurrent use. The order of cache and limit decides whether cache hits consume the allowance:

```csharp
problemEvaluator.Cached(keySelector).LimitEvaluations(100_000) // cache hits consume budget
problemEvaluator.LimitEvaluations(100_000).Cached(keySelector) // cache hits do not consume budget
```

<CacheLimitOrder>

The two orders as layers. A request enters the outer layer, and a cache hit returns from the cache layer without reaching the layers inside it.

</CacheLimitOrder>

With the limit outside, the budget means evaluation requests, so a refiner re-evaluating a candidate the cache already knows still spends from it. With the cache outside, a hit never reaches the limit and the budget means actual problem evaluations. Once the allowance is spent, a miss reaches the limiter, which returns its fallback objective vector, and the outer cache stores that fallback as the candidate's result.

### Refinement evaluation

`RefinementEvaluator` composes a refiner into the evaluator side instead. It refines the candidates, evaluates the refined copies through its evaluator and returns those objective vectors for the candidates that were supplied. The refined candidates are transient: they are discarded when the evaluation returns and are never written back into the population.

```csharp
var evaluator = RefinementEvaluator.Create(parameterFitting);
var sharedEvaluator = RefinementEvaluator.Create(parameterFitting, algorithm.Evaluator);
var fluentEvaluator = algorithm.Evaluator.AppliedAfterRefinement(parameterFitting);
```

This is Baldwinian refinement, because the refinement influences fitness without becoming part of the candidate: a candidate is credited with what it could reach through refinement while the population keeps the unrefined genotype. Configuring the same refiner as the algorithm's refiner makes it Lamarckian, because the algorithm then continues with the refined candidate.

`RefinementEvaluator` compares nothing and evaluates once. Give it the algorithm's evaluator configuration to share that evaluator's executor, as [above](#evaluations-of-a-refined-candidate) explains.

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

See [Termination](/reference/glossary#termination) for algorithm-owned termination and external early stopping.

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

Each of these follows its own rule for the candidates it returns:

- `PredefinedCandidatesCreator` returns its configured candidates in order across successive calls and asks the fallback creator only for the remaining places. Each executor returns each predefined candidate at most once.
- `EliteSelector` takes up to `elites` best candidates and asks its selector only for the remaining count. When the elites fill the request, the selector is not called and consumes no random draws.
- `GenderSpecificSelector` returns female 0, male 0, female 1, male 1 and so on. For an odd requested count, the female selector supplies the last candidate.
- For requests of complete mate pairs, `NoSameMatesSelector` asks its child selector up to `maximumAttempts` times. It keeps pairs whose objective vectors differ and fills any remaining places with equal-vector pairs from the last attempt.

## Budgets on operator work

A budget stops an algorithm when the counted or timed work of selected operators reaches a limit. Budgets are ordinary configurations: `OperatorCountBudgetAlgorithm` and `OperatorDurationBudgetAlgorithm` hold the algorithm with the wrappers already in place, the `Counter` or `Duration` the wrappers add to, and the limit. Each extension places a counter or a timer around the selected operator inside the budgeted algorithm and stops the algorithm once the count or the duration reaches the limit. Invocation and duration budgets apply to any operator, whatever its operation. A candidate budget needs an operation that reports candidate work:

```csharp
var calls = algorithm.LimitedToInvocations(algorithm.Mutator, 1_000);
var candidates = algorithm.LimitedToCandidates(algorithm.Evaluator, 100_000);
var seconds = algorithm.LimitedToDuration(algorithm.Evaluator, TimeSpan.FromSeconds(10));
```

`LimitedToInvocations` counts successful calls, and `LimitedToCandidates` counts their candidate work: input candidates for evaluation and returned candidates for candidate-producing operations. `LimitedToCandidates` therefore requires an operation that reports candidate work, as creation, crossover, mutation, evaluation, refinement, selection and replacement do. For a terminator or an interceptor, which report none, the run rejects the budget when it builds the executor. Timing uses the run's time provider and also records failed calls. The operator is matched by reference, and it is an error when the algorithm does not hold it. For an operator implementing several operations, pass it through a variable of the operation's operator type. The budget is checked after a yielded state, so a batch or an iteration can take the count past the limit, and a nonpositive limit stops after the first state. Comparison work of an improvement check counts when the check holds the selected evaluator, as [Evaluations of a refined candidate](#evaluations-of-a-refined-candidate) explains.

Pass a placement to combine work from several operators in one budget. It receives the algorithm and the budget's counter or duration, places the wrappers, and returns the algorithm to limit:

```csharp
var combined = algorithm.LimitedToCount(1_000, (ga, calls) => ga.Substitute(
    ga.Mutator.Invocations.Counting(calls),
    ga.Crossover.Invocations.Counting(calls)));
var timed = algorithm.LimitedToDuration(TimeSpan.FromSeconds(10), (ga, time) => ga.Substitute(
    ga.Mutator.Invocations.Timing(time),
    ga.Crossover.Invocations.Timing(time)));
```

The placement runs during the call and is not stored. Each `Counting` states what it counts, so `ga.Evaluator.Invocations.CountingCandidates(calls)` adds evaluated candidates where the default adds successful calls. Each `Counting` or `Timing` places one wrapper, so naming a target twice counts it twice, and an operator implementing several operations takes one call for each operation to count. Duration is the sum of invocation durations, including overlapping and nested time, rather than wall clock time for the algorithm.

The counter or duration lives inside the budget and starts again each time the budget is built, such as in each Cycle turn, while a counter placed outside the budget with `Invocations.Counting(counter)` spans the run. [Sharing executors](/guide/fundamentals/configurations#sharing-executors) explains this rule for counters and durations. Name the targets of an analysis from the budgeted configuration, as [Configurations](/guide/fundamentals/configurations#substituting-for-one-run) states. A duration budget excludes observer time only when it times the retained inner operator, the configuration an observation wraps, and not the observed wrapper. Place the analysis with `Substitute` first, then budget the original operator by its own reference, so that the timer sits closer to the operator than the observer. A budget that names the observed wrapper, or one applied before the analysis, measures the observer too. To stop on a counter you placed yourself, use a terminator, as [Share accounting with a terminator](/guide/execution/observability-and-analysis#share-accounting-with-a-terminator) shows.

A budget holds a copy of the algorithm, so a separate run of the original algorithm neither counts against the budget nor shares its executors. A budget built by hand takes the algorithm with a wrapper already placed:

```csharp
var constructed = new OperatorCountBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
    algorithm.Counting(algorithm.Mutator, out var mutations), mutations, 1_000);
```

`OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>` likewise takes the algorithm, the `Duration` its wrappers add to, and the limit.

## Observation and inspection boundaries

Source counters, durations, analyzers and `Observing`, placed with `Substitute`, add `Counted`, `Timed` or `Observed` wrappers around the configurations they select.

Selecting a complete composition observes its outer operation boundary and includes nested work. Selecting one child observes only calls reaching that child. For example, measuring a `TransformedCrossover` includes crossover and mutation work, while measuring its child crossover includes only crossover work. A named configuration such as `mutator.Named("exploration")` supplies a selectable boundary around a child.

See [Analysis of algorithm runs](/guide/execution/observability-and-analysis) for observation, counter, duration and selection APIs, and [Select by name](/guide/execution/observability-and-analysis#select-by-name) for named boundaries.

## Adaptable parameters

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/AdaptationDocumentationSpecs.cs#adaptation-wiring{csharp}

`GaussianMutator.MutationStrength` remains the configured base value. Its optional `MutationStrengthAdaptation` child is an `IConfigurationNode<IAdaptation>`: the executor resolves it and calls `Apply(baseValue)` once per mutation invocation. `ScaleAdaptation` starts with factor 1 and `OffsetAdaptation` with offset 0. A rule writes their runtime objects through the settable `Factor` and `Offset` properties.

The strategy's slot is `ScaleAdaptation?`, the form its 1/5 success rule writes. Both slots must name the same instance. An unset strategy slot means no adaptation. Wrappers forward nothing: `Timed`, `Counted`, `PipelineMutator` and `ChoosePerInvocation` can surround mutators that read the same link. Several mutators can read one adaptation with different base values. External nodes may implement `IConfigurationNode<IAdaptation>` directly, including as records.

A scope resolves one runtime adaptation per configuration reference, and independent runs remain independent. To retain an adaptation across Cycle turns, [share](/guide/fundamentals/configurations#sharing-executors) the adaptation on the Cycle. Sharing the mutator also requires sharing the adaptation: otherwise the retained mutator reads its original adaptation while each fresh strategy writes a different one. The resolver does not detect unused links.

`Retry(3)` includes the first attempt. It catches only a directly thrown `RetryableOperationException`. Cancellation, ordinary failures and aggregate exceptions propagate. Every attempt receives the same arguments, child and RNG. Failed attempts retain their random draws, state changes and inspection effects. Retry provides no rollback.

## Randomness and executors

Every operation call receives an explicit random number generator, and compositions invoke children in the order defined by their policy. Random draw order is therefore part of reproducible behavior. Adding, removing or reordering child operators may change later random draws even when the same root seed is used. The built in terminators and interceptors draw nothing, so `AnyTerminator`, `AllTerminator` and `PipelineInterceptor` are deterministic with respect to child ordering unless a child draws or depends on some other explicit input or external resource.

Child configurations resolve when each composition executor is built, and each construction has independent private fields. [Sharing](/guide/fundamentals/configurations#sharing-executors) keeps one executor across child scopes.

See [Reproducible randomness](/guide/execution/randomness) and [Algorithm runs](/guide/execution/running-algorithms) for the underlying models.

## Next steps

- [Validation and invariants](/guide/fundamentals/invariants-and-validation) continues the guide with what a run checks before it starts.
- [Write a composition](/extend/writing-compositions) when no built-in composition expresses the policy you need.
- [Write wrappers](/extend/writing-wrappers) places runtime policies around the executors of selected operators and algorithms.
- [Configurations](/guide/fundamentals/configurations#sharing-executors) explains how `Sharing` keeps an executor, such as a cache, across child scopes.
