# Aspect redesign

Status, 2026-10-15: the foundation is implemented in `7db6e6df`; the integration audit, documentation repairs and architecture/authoring review are committed in `bc6e15a9`. Their final whole-solution validation passed all 2,993 tests. The Inspection and matching follow-up is committed in `28309b6f`; its final whole-solution validation passed all 3,007 tests. Performance measurement remains deferred by user decision.

## Goal

Align the aspect types with AOP and with how they are actually used, and simplify the architecture by removing responsibilities rather than renaming them:

- A **pointcut** selects configurations.
- An **advice** wraps the execution built for a selected configuration. It contains no pointcut.
- An **aspect** pairs pointcuts with advice. It neither composes other aspects nor declares lazily.

A **join point** is the execution built for one configuration that a pointcut selects. Configuration wrappers and aspects stay separate worlds: wrappers are part of the configuration graph, while aspects are woven in when resolution turns the configuration graph into an execution graph.

## Decisions

### Pointcut

`Pointcut<TConfiguration>` is abstract with `public abstract bool Matches(TConfiguration)`. Custom selection can derive a small subclass or use the runtime-only `Pointcut.Predicate<TConfiguration>` factory. Reference, OfType, Predicate, Named, And, Or and Not implementations are private nested classes. A non-generic `Pointcut` base lets pointcuts for different roles share one list. The 2026-10-15 follow-up below records predicate, negation and name matching.

### Advice

Advice wraps executions, not configurations. An execution type depends on the run's search space and problem, which are unknown when an aspect is written. Each advised role therefore has one abstract advice base whose method is generic over those types. The bases are boilerplate written once per role:

```csharp
public abstract class Advice
{
    private protected Advice() { }
}
public abstract class Advice<TConfiguration> : Advice where TConfiguration : class, IConfigurationNode;

public abstract class MutatorAdvice<TCandidate> : Advice<IMutator<TCandidate>>
{
    public abstract WrapperExecutionFactory<IMutatorExecution<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TSearchSpace, TProblem>(IMutator<TCandidate> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
```

The method reuses the name and delegate of the wrapping operators' hook. The resolver calls it once per join point and owner state. The method validates the run's types and allocates state that must survive rebuilding the execution graph, such as an observed algorithm's iteration counter. The returned function wraps each construction of that join point's execution. The resolver keeps the function internally: it is the advice applied to one join point, not a separate concept, and it has no public type.

Advice types are classes. The resolver compares advice by reference, never by value.

Decision, 2026-10-15: retain the single `Advice` concept and its name. It provides around advice: an execution wrapper controls whether and how the underlying operation is invoked and can change behavior as well as observe it. Separate `Before`, `AfterReturning`, `AfterThrowing` and `After` convenience APIs are deferred for future discussion in the [developer backlog](developer-backlog.md). Algorithm advice can wrap `IAlgorithmExecution` and its returned stream; this decision adds no algorithm join points and does not replace the configured `Interceptor`.

`Advice<TConfiguration>` names the configuration role an advice can attach to; it does not change what is wrapped. It lets the compiler reject a pointcut and advice for different roles. `Aspect.Create<TSelected, TConfiguration>(Pointcut<TSelected>, Advice<TConfiguration>) where TSelected : TConfiguration` infers both type arguments, including for `Pointcut.Reference(gaussianMutator)`. Call sites must never need explicit type arguments; the draft verifies this.

Observation advice per role, such as `MutatorObservationAdvice`, reports successful calls. Counting and timing advice are standalone. Timing needs a hook before the call, and standalone counting avoids allocating an observation per call. Observation advice takes only its callback and reports the configuration selected by the pointcut.

The role's resolution extension converts an advice to the run's types: only it knows the search space and problem needed to call `CreateWrapperFactory<TSearchSpace, TProblem>(selectedConfiguration)`. It keeps passing a callback for this into `Resolve`, now reduced to a cast and one call. The `Resolve` parameters are named after the methods they call: `prepareFactory` becomes `createExecutionFactory`, and `prepareAdvice` becomes `createWrapperFactory`. The callback is `Func<Advice, TConfiguration, WrapperExecutionFactory<TExecution>>` and passes the selected configuration to the role advice. Alternatives assessed earlier: static hooks on execution roles fail with CS8705 for an execution implementing two roles; factory objects replace the delegate factory contract and add allocations.

### Aspect

`Aspect` is an abstract class rather than a record, because analyzers derive from it and own mutable results. It stores its pointcut/advice pairs as tuples:

```csharp
public abstract class Aspect
{
    public ImmutableArray<(Pointcut Pointcut, Advice Advice)> Advices { get; protected init; }
}
```

Derived constructors assign `Advices`, so the aspect still sets up its own pointcuts and advices from the operators a user passes in, and observation callbacks can capture the analyzer itself. Init properties are assigned after the constructor runs, so a value needed to build the advices must be a constructor parameter or be read when a callback fires. For example, `RankAnalyzer`'s `ObjectiveComparer`, which it passes to the `GenealogyAnalyzer` it creates, becomes a constructor parameter. An aspect holds several advices; an analyzer observing several operators is one aspect. Each advice instance should pair with one pointcut. If it appears with two pointcuts, the first pairing wins without a check. Applying the same behavior at two pointcuts needs two advice instances, which may write to a shared object such as one counter.

`Aspect` has no `Declare`, builder, lock, child collection or `Func` fields. Users create a single observation with a static factory returning a one-pair aspect, such as `Aspect.Observe(mutator, callback)`.

### Aspects and parent scopes

There is no composite aspect. A resolution scope stores its own aspects and its parent scope. `Advices()` walks that chain and yields each advice instance once at its first position; its comment explains why:

- The order in which aspects are added is application order; earlier advice wraps closer to the source execution.
- A new scope layer precedes inherited layers.
- Duplicates are removed by advice reference; the first occurrence determines the position.
- Combining aspects concatenates their lists. A trace includes its clocks' pairs. Two traces sharing a clock contain the same advice instances, so the clock applies once.
- The advice instance is the key for its join-point state. No registration or declaration type is introduced.

### Predefined inspection and configured behavior

- Counting, timing and observing do not change what an operator computes. They are advice only. The `Counting*` and `DurationMeasuring*` operators of all nine roles, with their `CountCalls` and `MeasureDuration` extensions, are removed. Tests that configure them migrate to aspects.
- Caching, limits, repetition, iterated and improvement-checking refiners and the dynamic evaluators change results or cost. They stay operators only.

These choices classify the predefined content. General advice may change behavior, and algorithms or operators may create aspects internally. Observation preserves the observed operation; inspection updates only its own collected data.

### Operator budgets

Budgets stay configuration nodes. Their execution factory adds an aspect to the child scope that resolves the inner algorithm:

```csharp
var counter = new CountAccumulator();
var counting = Aspect.Create(Pointcut.Reference(ObservedOperator), Counting.CreateAdvice(counter));
return scope => new OperatorBudgetAlgorithmExecution<…>(
    scope.With(counting).Resolve<…>(Algorithm), counter, MaximumCount);
```

`IOperatorCounting<TOperator>` and `IOperatorDurationMeasuring<TOperator>` are value strategies in Inspection. Each role supplies a sealed counting and duration-measuring record in its advice files. Budgets require `Counting` or `DurationMeasuring` instead of delegate properties; the role extensions pass these values. The aspect exists only in the budget's child scope, so it counts only calls made inside the budgeted algorithm. If the algorithm does not reference the observed operator, nothing matches and the budget never stops. This is accepted.

### Removed

- Recipes, wrapper configurations used as advice, and the runtime `Advice<TExecution>` delegate.
- `AspectBuilder`, `Aspect.Create(declare)`, the private callback and rule subclasses, and the `Action<AspectBuilder>` overloads of `ResolutionScope.Create`, `CreateChildScope` and `With`.
- `Aspect.Collect` and lazy once-only declaration with its lock, recursion flag and retry logic.
- The wrapping operators' public static `CreateWrapperFactory(wrapper)` bridges. The protected hook stays for configured wrappers.
- The instrumentation operators listed above.

### Unchanged

- State ownership: factories by configuration reference, ancestor reuse without hoisting, and dependencies resolved in the owner's state with the requesting aspects. `ChildScopePolicy` remains. The public scope API is `Create`, `CreateChildScope`, `With` and `Resolve*`. `CreateChildScopeWithoutAspects` and `WithAspectsFrom` are removed; Retained uses internal `CreateChildState` and `WithState` through `ChildScopeSelector`, retaining advice state in `ResolutionScope.State` keyed by advice.
- Controls project from the raw execution. Nothing is stored on failure, so a later resolution retries. Recursion is detected across a root state and its children.
- Join-point state lives in the owner state, weakly keyed by advice, so it is collectible in both directions.
- `AspectFactory`, per-trial aspects and `TrialAspect` remain, with their constraints moved to the new `Aspect`.
- `ElapsedTimeClock` captures its start time when constructed.

### Adding aspects to runs

A run only collects aspects; weaving happens later, when resolution builds the execution graph. `AlgorithmRun.Attach` therefore becomes `AddAspect`, and `ExperimentRun.AttachPerTrial` becomes `AddAspectPerTrial`. `ExperimentRun.GetAttached` becomes `GetAspectPerTrial`.

### Placement and factories

Predefined aspects, observation records and advice, `Observation`, `ObservationSignature`, clocks and their factories live in `HEAL.HeuristicLib.Inspection`, under role grouping folders. They are content-layer types; only `CountAccumulator` and `DurationAccumulator` remain domain vocabulary. `Analysis` keeps traces, measurements, analyzers and internal `Moment`. Mechanisms stay in Execution; role advice bases sit beside role interfaces. Convenience factories are static extension members on `Aspect` or `Clock`, except `Clock.FromElapsedTime`, which is a static member. Pointcut factories remain on Pointcut. Interceptor and terminator advice factories include `TRunSearchState`; `AlgorithmAdvice<TCandidate, TSearchState>` targets `IAlgorithm<TCandidate, TSearchState>`.

## Accepted first draft

The accepted first draft replaced types in place and established the mutator and evaluator template:

- core types: the non-generic `Pointcut` base, `Advice`, `Advice<TConfiguration>`, `Aspect` with `Create` and `Observe`, and aspect inheritance on `ResolutionScope`, with the removals listed above;
- mutator and evaluator: advice bases, observation, counting and timing advice, the `createWrapperFactory` callbacks in their resolution extensions, removal of their static bridges and instrumentation operators;
- callers limited to these roles: both operator budgets with their mutator and evaluator extensions, `EvaluationClock`, `TraceAnalyzer`, the mutator and evaluator analyzer factories, `AlgorithmRun` and `ExperimentRun`.

`ElapsedTimeClock` lives in `Inspection/Clock.cs` and captures its start time on construction. The completed remaining-role migration follows the accepted mutator and evaluator pattern.

## Implementation completion checklist

- [x] Glossary: `Aspect` and `Pointcut`, a new `Advice` entry and join point wording; remove recipe and composition wording.
- [x] [Execution resolution](../docs/contributing/architecture/execution-resolution.md), the analyzer architecture page and the user guides on observability and operator budgets.
- [x] API usage specs, samples and tests that use `AspectBuilder`, recipes or the instrumentation operators.

Commit `7db6e6df` contains the completed migration, including instrumentation, budgets, analyzers, API usage specs, tests and published documentation. The former migration-report link was removed because its target is absent from the committed tree. The [execution-resolution plan](execution-resolution-redesign.md#step-5-architecture-and-authoring-review-2026-10-15) records the completed integration/architecture reviews and final whole-solution validation.

## Inspection and matching follow-up, 2026-10-15

The user authorized implementation after the architecture and terminology discussion. This is an addition to the completed foundation, not a reopening of its resolver design.

- Rename the user-facing concept and namespace from Instrumentation to `HEAL.HeuristicLib.Inspection`, including source folders, consumers and architecture expectations. Keep AOP mechanisms in Execution and result derivation in Analysis. The breaking rename has no compatibility aliases.
- Add `Pointcut.Predicate<TConfiguration>(predicate)` as a runtime-only convenience for configuration-property or attribute matching. Preserve custom subclasses, query-time evaluation, declared-type filtering, short-circuit composition and exception propagation.
- Add `Not()` and `!` as complements within the declared configuration type.
- Add `INamedConfiguration` and `Pointcut.Named<TConfiguration>(name)`. Names compare ordinally and case-sensitively, need not be unique and do not identify resolution state or caller paths.
- Supply a `Named*` configuration for all nine operator roles and for algorithms. Each keeps its role and declared child, forwards the child's execution, exposes an init-only Name, and supports direct construction, a type-inferred Create factory and a Named fluent method. Name pointcuts select the wrapper, not its child.
- Retain the single around-capable Advice contract. Existing algorithm advice wraps RunStreamingAsync and its stream. Observation must preserve inputs, outputs and computation; algorithms may explicitly use inspection data to make decisions. No new persistable aspect wrapper, lifecycle advice kinds, caller paths or interceptor replacement is part of this package.

Validation: the baseline Release build passed with 118 existing warnings and zero errors; all 46 inspection tests passed both before and after the namespace rename. After removing null guards on non-nullable references and the two tests requiring them, focused pointcut/named-wrapper tests pass 31 cases, and pointcut API usage specs pass 15 cases. Final Release solution build passed with zero errors and 79 warnings from the incremental build. The final pre-commit whole-solution run passed all **3,007 tests**, with zero failures or skips: 2,534 core, 207 API usage, 239 Experimental and 27 scenarios. This supersedes the earlier 3,009-case run, which included the two removed null-contract tests. The documentation build, changed-Markdown Prettier check, whitespace verification, style verification at warning severity and analyzer verification at error severity all passed. Format tools reported workspace-loading warnings. The namespace move was checked against the original source. The user separately authorized staging and then committing the final package as `28309b6f`.

## Final package review, 2026-10-15

Scope: review `bc6e15a955e3b6df8ed219ed3fdffef194c9b6dc...28309b6f9330f8c77b94de868dea96ad2194fb68`, the single committed Inspection and matching package. The earlier resolver architecture/authoring review remains the foundation review.

- **Standards: no findings.** Runtime predicates follow the documented runtime-only exception to value-strategy rules; typed pointcuts remain local to the supplied configuration; named wrappers preserve configuration ownership, public API conventions and the non-nullable reference contract. Per-role repetition and forwarding serve the approved naming responsibility.
- **Spec and behavior: no findings.** All 34 Inspection source moves preserve their implementations apart from namespace references. Predicate evaluation, exception propagation, declared-type negation, exact/nonunique names, consumer-defined named configurations, shared child state, independent named observations and algorithm stream advice match the accepted scope and its tests.

No source changes are required by this review. The final pre-commit whole-solution result remains 3,007 passing tests; these closure corrections change only plans, so the suite was not rerun. Changed Markdown and whitespace checks pass. The user-local untracked `.claude/settings.local.json` was excluded from review. No staging, commit, history or integration action is part of this closure work. The agreed implementation scope is complete; deferred features remain in the developer backlog.
