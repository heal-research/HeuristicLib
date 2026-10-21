# Operator composition

::: info Advanced extension
Start with [built-in operators](/guide/fundamentals/operators). Compose operators when several reusable policies should behave as one operator in an algorithm configuration.
:::

Operator composition builds a new operator configuration from one or more existing operator configurations. The resulting configuration remains reusable and resolves its child executors as part of the execution graph.

Composition policies describe how child operations are coordinated. `WrappingOperator` and `MultiOperator` are authoring infrastructure for implementing those policies. They do not prescribe behavior by themselves.

The built in composition overview is:

| Composition aspect              | Coordination semantics                                                                                        | Helpers and examples                                                                                        | Applicability                                                |
| ------------------------------- | ------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| Weighted alternatives per item  | Chooses a child independently for each item of a call, invokes grouped child batches and restores input order | `ChoosePerCandidate`                                                                                        | Creators, crossovers, mutators, evaluators and refiners      |
| Weighted alternatives per call  | Chooses one child for the complete operation call                                                             | `ChoosePerCall`                                                                                             | Every operation-parameterized operator                       |
| Conditional application         | Chooses between an operation and role specific unchanged behavior                                             | `mutator.AppliedAtRate(...)`, `crossover.AppliedAtRate(...)`, `refiner.AppliedAtRate(...)`                  | Mutators, crossovers and refiners                            |
| Sequential composition          | Passes each stage result to the next stage in order                                                           | `PipelineMutator`, `PipelineRefiner`, `PipelineInterceptor`                                                 | Mutators, refiners and interceptors                          |
| Repeated composition            | Feeds a result back into the same child for a configured number of iterations                                 | `IteratedRefiner`                                                                                           | Refiners                                                     |
| Objective-aware retention       | Measures a child's result and keeps it only when a criterion counts it as an improvement                      | `ImprovementCheckingRefiner`                                                                                | Refiners                                                     |
| Transient refinement evaluation | Refines candidates temporarily, measures the refined copies and discards them                                 | `RefinementEvaluator`                                                                                       | Evaluators composed with a refiner                           |
| Unconditional postprocessing    | Invokes a source operation and always transforms its complete result                                          | `TransformedCreator`, `TransformedCrossover`                                                                | Creators and crossovers with a mutator transformation        |
| Logical composition             | Combines child conditions with short circuit Boolean logic                                                    | `AnyTerminator`, `AllTerminator`                                                                            | Terminators                                                  |
| Fallback composition            | Handles part of a request directly and delegates the remainder                                                | `PredefinedCandidatesCreator`                                                                               | Creators                                                     |
| Specialized coordination        | Applies a policy whose meaning depends on one operator contract                                               | Caching, repetition and evaluation limits, elite inclusion, mate restrictions and gender specific selection | Evaluators and selectors                                     |
| Inspection                      | Wraps a selected execution boundary to observe, count or measure calls                                        | `Aspect.Observe`, `Aspect.CountCalls` and `Aspect.MeasureDuration`                                          | Role-specific observations; counting and timing across roles |

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

`ChoosePerCandidate<TOperation, TCandidate>` selects a child independently for each item of a call: each requested candidate of a creation, each parent group of a crossover and each candidate of a mutation, evaluation or refinement. Items assigned to the same child are passed to that child as one call. Results are then restored to the original item order.

This order restoration requires every selected child to return exactly one result for each assigned item, so a child creator returns the assigned count and every other child preserves its assigned batch size. Selection, replacement, termination and interception calls have no independent items. Their operations are not marked with `IItemOperation<TSelf>`, so `ChoosePerCandidate` does not compile for them.

Weights are retained exactly as configured. Omitting weights selects uniformly across all children. Positive finite weights participate proportionally; zero, negative finite weights, negative infinity and `NaN` mean never. Positive infinity overrides finite weights, with multiple positive-infinity children selected uniformly. If no child is selectable, selection falls back to uniform across all children. Configured weights are compiled once for repeated sampling rather than normalized or recalculated for every choice.

`ChoosePerCall<Selection, TCandidate>` and `ChoosePerCall<Replacement, TCandidate>` make one weighted choice for each complete selection or replacement call. The selected child receives the complete population inputs and requested count. This preserves the meaning of a selector or replacer policy as a decision over a complete population instead of mixing fragments from several policies.

`ChoosePerCall` makes the same whole-call choice for any operator using `IOperator<TOperation, TCandidate>`, including mutation, evaluation and operations defined outside the library. Each call samples one child with the call's random number generator and runs on that child with the same generator, unchanged inputs and the run's search space and problem. Use it when a batch should stay with one policy, such as evaluating a complete batch with either an exact or a surrogate evaluator, and use `ChoosePerCandidate` when each item should choose independently. Its weights follow the rules above, and building its executor rejects an empty child collection or a weight count that differs from the child count.

```csharp
var creator = ChoosePerCandidate.Create(
    [uniformCreator, normalCreator],
    [0.75, 0.25]);

var mutator = ChoosePerCandidate.Create(
    [smallMutation, largeMutation],
    [0.8, 0.2]);

var selector = ChoosePerCall.Create(
    [tournamentSelector, proportionalSelector],
    [0.6, 0.4]);

var evaluator = ChoosePerCall.Create(
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

Improvement checking evaluates twice, and the algorithm then evaluates the returned candidate itself, so the naive configuration costs three problem evaluations per refined candidate where an unchecked refiner costs one. That cost is deliberately visible rather than hidden, and it is controlled by one thing: whether the refiner and the algorithm hold the **same evaluator object**.

Resolution uses configuration reference identity to select prepared execution state. Reusing the same evaluator object through the same resolution scope preserves that state, including its counters and caches. Executors are ephemeral bindings and may be rebuilt around that state for each context. Two separately constructed but structurally identical configurations have independent prepared state.

The snippets in this section use these namespaces:

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
```

Counting and timing are aspects. They select the configured evaluator by reference and leave the reusable algorithm configuration unchanged:

```csharp
var evaluator = algorithm.Evaluator;
var counting = Aspect.CountCandidates(evaluator, out var counter);
var unshared = algorithm with
{
    Refiner = parameterFitting.CheckedForImprovement()
};
var run = unshared.CreateRun(problem, random).AddAspect(counting);
```

The improvement check uses its own evaluator configuration here, so the counter measures only the algorithm's evaluations. Share the selected evaluator to include comparison work:

```csharp
var evaluator = algorithm.Evaluator;
var counting = Aspect.CountCandidates(evaluator, out var counter);
var shared = algorithm with
{
    Refiner = parameterFitting.CheckedForImprovement(evaluator)
};
var run = shared.CreateRun(problem, random).AddAspect(counting);
```

Each refined candidate causes two comparison evaluations and the algorithm's subsequent evaluation. All three reach the selected evaluator and increment the same counter.

An external terminator can read that accumulator:

```csharp
var budgeted = shared.TerminatedBy(
    AfterOperatorCountTerminator.For(problem, counter, maximumCount: 100_000));
var run = budgeted.CreateRun(problem, random).AddAspect(counting);
```

`shared.LimitedToCandidates(evaluator, 100_000)` owns its accumulator and adds counting advice only to the child scope resolving the budgeted algorithm. Comparison work counts when the refiner holds the selected evaluator. A different evaluator is outside that selection. The budget is checked after a yielded state, so a batch or iteration can take the count past the limit. A nonpositive limit stops after the first state.

The same budgets apply to any operator, whatever its operation:

```csharp
var calls = shared.LimitedToCalls(refiner, 1_000);
var timed = shared.LimitedToDuration(evaluator, TimeSpan.FromSeconds(10));
```

`LimitedToCalls` counts successful calls, and `LimitedToCandidates` counts the candidates they return for an operation that reports candidate work. Timing also records failed calls. Timing accepts an explicit `TimeProvider`. Both are scoped to the budget's inner algorithm. If that algorithm never resolves the selected operator, nothing is recorded and the budget never stops it.

Several observed operators can feed the same termination criterion by sharing one `CountAccumulator`. For example, the same counter can track refiner calls and evaluations.

Sharing a `CachingEvaluator` pays for two evaluations rather than three, because the algorithm's own evaluation of the returned candidate is already in the cache:

```csharp
var evaluator = new CachingEvaluator<...>(new ProblemEvaluator<...>(), keySelector);

var cached = algorithm with
{
    Evaluator = evaluator,
    Refiner = parameterFitting.CheckedForImprovement(evaluator)
};
```

Select the outer cache configuration to count evaluation requests and its child evaluator to count actual problem evaluations:

```csharp
var problemEvaluator = new ProblemEvaluator<RealVector>();
var cache = new CachingEvaluator<RealVector>(problemEvaluator);
var requests = Aspect.CountCandidates(cache, out var requestCount);
var solves = Aspect.CountCandidates(problemEvaluator, out var solveCount);

var cached = algorithm with { Evaluator = cache };
var run = cached.CreateRun(problem, random)
    .AddAspect(requests)
    .AddAspect(solves);
```

After the run, `requestCount` includes cache hits and `solveCount` excludes them. To include refinement comparisons, give `CheckedForImprovement` the same `cache` configuration. Counting selects an existing configuration; it does not return an evaluator that can be placed inside the cache.

`LimitEvaluator` follows the same rule, and there the choice decides when a run stops rather than what a number reads:

```csharp
new CachingEvaluator<...>(problemEvaluator, keySelector).LimitEvaluations(100_000) // cache hits consume budget
new CachingEvaluator<...>(problemEvaluator.LimitEvaluations(100_000), keySelector) // cache hits do not consume budget
```

With the limit outside, the budget means evaluation requests, so a refiner re-evaluating a candidate the cache already knows still spends from it. With the cache outside, the request never reaches the limit and the budget means actual problem evaluations.

To attribute refinement effort separately instead of folding it into one counter, give the refiner its own evaluator and select that evaluator with an aspect:

```csharp
var refinementEvaluator = new ProblemEvaluator<RealVector>();
var refinementCounting = Aspect.CountCandidates(refinementEvaluator, out var refinementCount);
var attributed = algorithm with
{
    Refiner = parameterFitting.CheckedForImprovement(refinementEvaluator)
};
var run = attributed.CreateRun(problem, random).AddAspect(refinementCounting);
```

`refinementCount` measures the improvement check's comparison evaluations. The algorithm's own evaluator is a different configuration, so its calls do not contribute to that count.

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

## Observation and inspection boundaries

Inspection uses execution advice installed through aspects. `Aspect.Observe`, `Aspect.CountCalls` and `Aspect.MeasureDuration` select existing configurations and wrap their executors; they do not add observation, counting or timing configurations to the operator graph.

Selecting a complete composition observes its outer operation boundary and includes nested work. Selecting one child observes only calls reaching that child. For example, measuring a `TransformedCrossover` includes crossover and mutation work, while measuring its child crossover includes only crossover work.

Counting and duration advice retain their supplied result accumulator. Independent root executions using the same accumulator also contribute to it. Counters increment after a successful child call. Duration measurement keeps timestamps local to each invocation and records elapsed time even when the child throws.

A named configuration such as `mutator.Named("exploration")` supplies a selectable wrapper boundary while forwarding the child's executor. `Pointcut.Named<IOperator<Mutation, TCandidate>>("exploration")` selects that wrapper. Broad role pointcuts may select both the wrapper and its child.

See [Observability and analysis](/guide/execution/observability-and-analysis) for observation, counter, duration and pointcut APIs.

## Specialized executor capabilities

General wrappers and compositions preserve the operator role contract. Additional controls belong to the selected configuration's raw executor. `GaussianMutator` exposes `IMutationStrengthControl`, which changes execution-scoped mutation strength without providing a mutation operation.

An observation wrapper can implement `IMutationExecutor` without implementing `IMutationStrengthControl`. An `is` check on the wrapped mutator would then hide the source's adaptation capability. Resolving the control separately lets an algorithm keep adapting the selected source while every mutation call passes through the observation wrapper. The control interface contains no mutation operation, so it supplies no alternative path for mutation calls to bypass observations.

`EvolutionStrategy` projects this optional control during resolution and invokes mutation through the advised executor. Advice applied directly to the selected Gaussian configuration preserves access to its raw strength control. Rebinding preserves that selected state while applying the requesting scope's observations to mutation calls. Generic naming and retry wrappers pass on the same controls, including through child inspection.

An algorithm author can request a control through its typed scope without supplying factory wiring:

```csharp
var mutator = typed.Resolve(Mutator, out IMutationStrengthControl? strength);
```

Use `mutator` for mutation and `strength`, when non-null, to read or update the selected executor's strength.

An explicitly configured wrapper is a separate selected configuration. For example, assigning `PipelineMutator.Create(gaussian)` to `EvolutionStrategy.Mutator` exposes the pipeline's ordinary mutation role, so that configuration has no strength adaptation. A custom configured wrapper must deliberately expose `IMutationStrengthControl` if its own mutation behavior supports that control; the resolver does not search its children for additional capabilities.

### Transparent wrappers and controls

Every operation uses `IOperator<TOperation, TCandidate>`. The generic `Named` and `Retry` wrappers pass on the child's controls along with its executor: an executor implementing `IExecutorControlProvider` supplies another executor's controls, and `ExecutorControls.Find` follows it. Stacking `gaussian.Retry(3).Named("inner").Named("outer")` therefore keeps `EvolutionStrategy` strength adaptation, including when inspection wraps the Gaussian child.

`Retry(3)` includes the first attempt. It catches only a directly thrown `RetryableOperationException`; cancellation, ordinary failures and aggregate exceptions propagate. Every attempt receives the same arguments, child and RNG. Failed attempts retain their random draws, state changes and inspection effects; retry provides no rollback.

A multi-child composite such as a pipeline exposes its own controls. It does not inherit an arbitrary child's strength control. `ChoosePerCall` exposes none, because a different child can run on every call.

## Randomness and executors

Every operation call receives an explicit random number generator, and compositions invoke children in the order defined by their policy. Random draw order is therefore part of reproducible behavior. Adding, removing or reordering child operators may change later random draws even when the same root seed is used. The built in terminators and interceptors draw nothing, so `AnyTerminator`, `AllTerminator` and `PipelineInterceptor` are deterministic with respect to child ordering unless a child draws or depends on some other explicit input or external resource.

Child configurations are resolved when each composition executor is built. Reusing the same child configuration elsewhere through the same resolution scope preserves its prepared execution state. The resulting child executor objects may differ because each binding applies the requesting context's aspects.

See [Reproducible randomness](/guide/execution/randomness) and [Running algorithms](/guide/execution/running-algorithms) for the underlying models.

## Authoring a composition

Use `WrappingOperator<TOperation, TCandidate>` for one child and `MultiOperator<TOperation, TCandidate>` for several.

Derive from `Operator<TOperation, TCandidate>` when a composition coordinates different operations or declares children with specific domain responsibilities. `TransformedCreator` and `TransformedCrossover` follow this path because each coordinates both a source operator and a mutator.

The authored executor holds resolved child bindings and the prepared execution state needed for its context. Persistent mutable state is prepared once and survives rebuilding those bindings. The reusable configuration owns parameters and child configurations.

### A complete stateful composition

This `RoundRobinMutator` sends each complete batch to the next child in a fixed order. Unlike weighted selection, it remembers which child should run next. Its cursor advances only after a successful child call, including a successful empty batch.

Use these imports in a C# project with implicit system usings enabled:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-type{csharp}

The constructor retains child configurations through the multi base, which snapshots their collection. `CreateOperatorFactory` validates that collection and allocates `ExecutionState` once. The returned `Factory` keeps that cursor; its `CreateExecutor` resolves the current children through the typed scope it receives and builds an executor around the same cursor. Allocating `ExecutionState` inside `CreateExecutor` would reset the cursor whenever another observation context binds the composition.

The executor retains the resolved children of its context; it keeps only the cursor in shared state. `offspring` is local to one invocation. This example expects sequential calls: sharing the state does not serialize concurrent mutation operations.

#### Add inspection without restarting the sequence

Two distinct no-change mutators make the selection order visible through counters without changing candidate values:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-usage{csharp}

`firstCalls.CurrentCount` is zero and `secondCalls.CurrentCount` is one. The observed binding resumes at the second child; the existing binding retains its unobserved children. Both bindings advance the same cursor because `With` shares prepared state.

An independent root prepares a fresh cursor, even when it resolves the same configuration object:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-independent{csharp}

`independentFirstCalls.CurrentCount` is one. A child scope instead follows ancestor reuse: it does not reset state already owned by its parent. See [Execution resolution](/contributing/architecture/execution-resolution#state-and-construction) for those selection rules.
