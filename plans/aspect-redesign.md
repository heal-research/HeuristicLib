# Aspect redesign

Status: decided direction, 2026-10-14; not implemented. This plan supersedes the aspect, advice and recipe parts of the [execution-resolution redesign](execution-resolution-redesign.md). Its state, scope, Cycle and budget-placement decisions remain valid.

## Goal

Align the aspect types with AOP and with how they are actually used, and simplify the architecture by removing responsibilities rather than renaming them:

- A **pointcut** selects configurations.
- An **advice** wraps the execution built for a selected configuration. It contains no pointcut.
- An **aspect** pairs pointcuts with advice. It neither composes other aspects nor declares lazily.

A **join point** is the execution built for one configuration that a pointcut selects. Configuration wrappers and aspects stay separate worlds: wrappers are part of the configuration graph, while aspects are woven in when resolution turns the configuration graph into an execution graph.

## Decisions

### Pointcut

`Pointcut<TConfiguration>` stays as it is. A non-generic `Pointcut` base lets pointcuts for different roles share one list; the static `Reference` and `OfType` factories move onto it. More selection options can be added later.

### Advice

Advice wraps executions, not configurations. An execution type depends on the run's search space and problem, which are unknown when an aspect is written. Each advised role therefore has one abstract advice base whose method is generic over those types. The bases are boilerplate written once per role:

```csharp
public abstract record Advice;
public abstract record Advice<TRole> : Advice where TRole : class, IConfigurationNode;

public abstract record MutatorAdvice<TCandidate> : Advice<IMutator<TCandidate>>
{
    public abstract WrapperExecutionFactory<IMutatorExecution<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
```

The method reuses the name and delegate of the wrapping operators' hook. The resolver calls it once per join point and owner state. The method validates the run's types and allocates state that must survive rebuilding the execution graph, such as an observed algorithm's iteration counter. The returned function wraps each construction of that join point's execution. The resolver keeps the function internally: it is the advice applied to one join point, not a separate concept, and it has no public type.

Advice types are records. The resolver compares advice by reference, never by value.

`Advice<TRole>` names the configuration role an advice can attach to; it does not change what is wrapped. It lets the compiler reject a pointcut and advice for different roles. `Aspect.Create<TSelected, TRole>(Pointcut<TSelected>, Advice<TRole>) where TSelected : TRole` infers both type arguments, including for `Pointcut.Reference(gaussianMutator)`. Call sites must never need explicit type arguments; the draft verifies this.

Observation advice per role, such as `MutatorObservationAdvice`, is the basis of instrumentation. Convenience advice such as `CountingMutatorAdvice` is built on it.

The role's resolution extension converts an advice to the run's types: only it knows the search space and problem needed to call `CreateWrapperFactory<TSearchSpace, TProblem>()`. It keeps passing a callback for this into `Resolve`, now reduced to a cast and one call. The `Resolve` parameters are named after the methods they call: `prepareFactory` becomes `createExecutionFactory`, and `prepareAdvice` becomes `createWrapperFactory`. Remove the callback if a simpler route appears. Alternatives assessed earlier: static hooks on execution roles fail with CS8705 for an execution implementing two roles; factory objects replace the delegate factory contract and add allocations.

### Aspect

`Aspect` is an abstract class rather than a record, because analyzers derive from it and own mutable results. It stores its pointcut/advice pairs as tuples:

```csharp
public abstract class Aspect
{
    public ImmutableArray<(Pointcut Pointcut, Advice Advice)> Advices { get; protected init; }
}
```

Derived constructors assign `Advices`, so the aspect still sets up its own pointcuts and advices from the operators a user passes in, and observation callbacks can capture the analyzer itself. Init properties are assigned after the constructor runs, so a value needed to build the advices must be a constructor parameter or be read when a callback fires. For example, `RankAnalyzer`'s `ObjectiveComparer`, which it passes to the `GenealogyAnalyzer` it creates, becomes a constructor parameter. An aspect holds several advices; an analyzer observing several operators is one aspect. Each advice instance pairs with exactly one pointcut. Applying the same behavior at two pointcuts needs two advice instances, which may write to a shared object such as one counter.

`Aspect` has no `Declare`, builder, lock, child collection or `Func` fields. Users create a single observation with a static factory returning a one-pair aspect, such as `Aspect.Observe(mutator, callback)`.

### One flat list on the scope

There is no composite aspect. A resolution scope holds one flat, ordered list of pointcut/advice pairs:

- The order in which aspects are added is application order; earlier advice wraps closer to the source execution.
- A new scope layer precedes inherited layers.
- Duplicates are removed by advice reference; the first occurrence determines the position.
- Combining aspects concatenates their lists. A trace includes its clocks' pairs. Two traces sharing a clock contain the same advice instances, so the clock applies once.
- The advice instance is the key for its join-point state. No registration or declaration type is introduced.

### Instrumentation is advice; behavior stays configuration

- Counting, timing and observing do not change what an operator computes. They are advice only. The `Counting*` and `DurationMeasuring*` operators of all nine roles, with their `CountCalls` and `MeasureDuration` extensions, are removed. Tests that configure them migrate to aspects.
- Caching, limits, repetition, iterated and improvement-checking refiners and the dynamic evaluators change results or cost. They stay operators only.

### Operator budgets

Budgets stay configuration nodes. Their execution factory adds an aspect to the child scope that resolves the inner algorithm:

```csharp
var counter = new CountAccumulator();
var counting = Aspect.Create(Pointcut.Reference(ObservedOperator), CreateCountingAdvice(counter));
return scope => new OperatorBudgetAlgorithmExecution<…>(
    scope.With(counting).Resolve<…>(Algorithm), counter, MaximumCount);
```

`CreateCountingAdvice` replaces `CountedOperatorFactory`; each role's budget extension supplies that role's counting advice. The aspect exists only in the budget's child scope, so it counts only calls made inside the budgeted algorithm. If the algorithm does not reference the observed operator, nothing matches and the budget never stops. This is accepted.

### Removed

- Recipes, wrapper configurations used as advice, and the runtime `Advice<TExecution>` delegate.
- `AspectBuilder`, `Aspect.Create(declare)`, the private callback and rule subclasses, and the `Action<AspectBuilder>` overloads of `ResolutionScope.Create`, `CreateChildScope` and `With`.
- `Aspect.Collect` and lazy once-only declaration with its lock, recursion flag and retry logic.
- The wrapping operators' public static `CreateWrapperFactory(wrapper)` bridges. The protected hook stays for configured wrappers.
- The instrumentation operators listed above.

### Unchanged

- State ownership: factories by configuration reference, ancestor reuse without hoisting, and dependencies resolved in the owner's state with the requesting aspects. `ChildScopePolicy`, `CreateChildScopeWithoutAspects` and `WithAspectsFrom` remain.
- Controls project from the raw execution. Nothing is stored on failure, so a later resolution retries. Recursion is detected across a root state and its children.
- Join-point state lives in the owner state, weakly keyed by advice, so it is collectible in both directions.
- `AspectFactory`, per-trial aspects and `TrialAspect` remain, with their constraints moved to the new `Aspect`.
- `ElapsedTimeClock` captures its start time in `Declare`. It needs another trigger; this is deferred.

### Adding aspects to runs

A run only collects aspects; weaving happens later, when resolution builds the execution graph. `AlgorithmRun.Attach` therefore becomes `AddAspect`, and `ExperimentRun.AttachPerTrial` becomes `AddAspectPerTrial`. `GetAttached` follows the same naming; the draft proposes its name.

## First draft

The draft replaces the current types in place, so the diff shows what is replaced. It covers mutator and evaluator advice only:

- core types: the non-generic `Pointcut` base, `Advice`, `Advice<TRole>`, `Aspect` with `Create` and `Observe`, and the flat list on `ResolutionScope`, with the removals listed above;
- mutator and evaluator: advice bases, observation, counting and timing advice, the `createWrapperFactory` callbacks in their resolution extensions, removal of their static bridges and instrumentation operators;
- callers limited to these roles: both operator budgets with their mutator and evaluator extensions, `EvaluationClock`, `TraceAnalyzer`, the mutator and evaluator analyzer factories, `AlgorithmRun` and `ExperimentRun`.

`ElapsedTimeClock` lives beside `EvaluationClock`; until its trigger is decided, it captures its start time on construction. Code for the other roles stays untouched and stops compiling, including in the core project. The draft is accepted when the new and changed files compile without errors of their own and the shape looks right in review; only then are the other roles migrated and the tests run.

## Follow-up after implementation

- Glossary: `Aspect` and `Pointcut`, a new `Advice` entry and join point wording; remove recipe and composition wording.
- [Execution resolution](../docs/contributing/architecture/execution-resolution.md), which still describes the resolver before the execution-resolution redesign, the analyzer architecture page and the user guides on observability and operator budgets.
- API usage specs, samples and tests that use `AspectBuilder`, recipes or the instrumentation operators.
