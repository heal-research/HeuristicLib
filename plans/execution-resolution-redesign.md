# Execution-resolution redesign

Status: final design, 2026-10-13. Step 2 is implemented in the working tree, uncommitted, pending review. This is the only active plan for replacing the execution resolver. It replaces the candidate recorded in `68ad810c` with a smaller design; the [differences](#changes-from-the-earlier-candidate) are listed below. Decided on 2026-10-13: the resolver has no node cache; advice receives the execution it wraps directly; algorithms create no operators of their own; `Aspect` replaces `IExecutionModule`; analyzers are aspects; behavior changes 1 to 7 are accepted; `NodeSelector`, `Aspect`, `AspectBuilder` with `Advise` and `Install`, `Advice<TExecution>`, the wrapping bases' `CreateAdvice` hook, `AspectFactory` and "advice chain" are the chosen terms. No questions remain open; steps 3–5 await approval. Typed advice, package 5 of [container and aspect framing](container-and-aspect-framing.md), stays on hold. [Execution bindings and shared state](execution-bindings-and-shared-state.md) and [execution factory design](execution-factory-design.md) record the production baseline, `c6957cbc`, and its history.

## Objective

Make the resolution system itself substantially simpler, as well as reducing the burden on operator, algorithm and extension authors. A small public API over complicated internal ownership machinery does not meet this objective. Neither does moving the machinery into fewer classes.

HeuristicLib is alpha. Backward compatibility, preservation of existing implementation and existing type boundaries are not selection criteria. Current behavior and tests supply evidence; distinguish important user capabilities from incidental policies before retaining either.

Keep ordinary typed operations, explicit dependencies, private state initialized once and immutable constructed child references. Do not introduce role registries, universal invocation, ambient operation contexts or compatibility shims.

## Why production is complicated

The production resolver at `c6957cbc` spends about 810 lines in ten types (`ResolutionScope`, `ExecutionSharingScope`, `RetainedChildScope`, `ExecutionBindings`, `NodeConstruction`, `WrappedNodes`, `ExecutionPreparation`, `WrapperRegistration`, `PreparedWrapper`, `InternalComposition`). Most of it serves four mechanisms that this design makes unnecessary:

- **Keyed child retention** keeps a child's state and declarations under a key, then rebuilds a scope for each requesting context. It exists because every child scope also creates new state: a budget adding wrappers, or Cycle keeping per-algorithm state across activations, had to keep that state alive somewhere.
- **Per-context node caches** with fault records and unchanged-parent lookup exist to return the same node object again. Shared state does not need that.
- **Pinned dependency tables** remember what each preparation selected, although the selection is already determined by where the preparation's state lives.
- **Source interception** exists because a generated wrapper is a configuration containing its target, built through ordinary child resolution. The wrapper therefore finds its target by resolving it, which would apply the same wrapper again; the resolver redirects that call to the already-built chain. Internal compositions reuse the redirection with isolation flags, internal-source checks and weak references.

## Design

A resolution scope combines its **state**, which decides which state each configuration uses, with an **aspect**, which decides which advice its executions receive.

- **State.** The scope's state keeps the factory each configuration created, and with it the execution state that factory captured. A configuration already held by a parent scope's state uses that factory; otherwise the factory is created once. A factory resolves its own dependencies **in the state that holds it**, with the aspect of the scope that asked. This replaces dependency pinning: an earlier or later selection elsewhere cannot change which state a composite uses, while new advice still reaches its children.
- **Aspect.** An aspect pairs a `NodeSelector` with advice for the configurations it selects. Following the composite pattern, an aspect declares any number of such pairs and may install other aspects; a scope always applies exactly one aspect, combined from everything it received.
- **Advice.** Advice wraps an execution that has already been built. It is prepared once for each aspect and target and then applied to the target's execution in every construction. Advice never resolves its target, so nothing loops and nothing needs redirecting. The advice applied to one configuration in one scope forms its **advice chain**.

Every resolution constructs new nodes; nothing caches them. Nodes are cheap views over shared state, in the manner of a flyweight.

### Deriving scopes

| Operation                        | State                           | Aspect                 | Used for                                      |
| -------------------------------- | ------------------------------- | ---------------------- | --------------------------------------------- |
| `ResolutionScope.Create(aspect)` | New root state                  | The given aspect       | Runs and standalone resolution                |
| `scope.CreateChildScope(aspect)` | New state under the current one | Current plus the given | Fresh stages, activations, contenders         |
| `scope.With(aspect)`             | Same state                      | Current plus the given | Adding advice without new state, e.g. budgets |

Overloads taking `Action<AspectBuilder>` remain for the first two.

### Core sketch

The complete resolver, as a sketch rather than final code:

```csharp
public sealed class ResolutionScope
{
    private readonly State state;
    private readonly Aspect aspect;

    (TExecution Raw, TExecution Advised) Construct<TConfiguration, TExecution>(
        TConfiguration configuration, ExecutionRole<TConfiguration, TExecution> role)
    {
        var (factory, owner) = state.GetOrAdd(configuration, role);
        using var guard = state.EnterConstruction(configuration);
        var raw = factory(new ResolutionScope(owner, aspect));
        var advised = raw;
        foreach (var matching in aspect.Matching(configuration))
            advised = owner.GetOrAddAdvice(matching, configuration, role)(advised);
        return (raw, advised);
    }

    private sealed class State(State? parent)
    {
        private readonly Dictionary<IConfigurationNode, Delegate> factories = new(ReferenceEqualityComparer.Instance);
        private readonly ConditionalWeakTable<Aspect, Dictionary<IConfigurationNode, Delegate>> advice = new();
        private readonly HashSet<IConfigurationNode> constructing = parent?.constructing ?? new(ReferenceEqualityComparer.Instance);

        public (ExecutionFactory<TExecution>, State) GetOrAdd<TConfiguration, TExecution>(
            TConfiguration configuration, ExecutionRole<TConfiguration, TExecution> role)
        {
            for (var state = this; state is not null; state = state.parent)
                if (state.factories.TryGetValue(configuration, out var found))
                    return (found as ExecutionFactory<TExecution> ?? throw IncompatibleRole(configuration, found), state);
            var created = role.Factory(configuration);
            factories.Add(configuration, created);
            return (created, this);
        }

        public Advice<TExecution> GetOrAddAdvice<TConfiguration, TExecution>(
            Aspect aspect, TConfiguration target, ExecutionRole<TConfiguration, TExecution> role)
        {
            var prepared = advice.GetOrCreateValue(aspect);
            if (!prepared.TryGetValue(target, out var found))
                prepared.Add(target, found = role.Advice(aspect.AdviceFor(target)));
            return (Advice<TExecution>)found;
        }
    }
}
```

`State` is a private class of `ResolutionScope`, not a public concept. It must be a separate object because a factory's construction scope combines the state of the scope that holds the factory with the aspect of the scope that asked. Beyond its factory table it has three duties: falling back to the parent's state, keeping advice for exactly as long as both the state and the aspect live, and sharing one recursion guard across a root and all its children.

`ExecutionRole<TConfiguration, TExecution>` replaces the preparation lambdas that role resolve extensions pass today. Each role supplies one small role class with two methods: `Factory`, which closes the run types and calls `CreateExecutionFactory`, and `Advice`, which obtains the advice of a wrapper of that role. Custom roles supply their own role class; there is no registry.

No other storage exists: no node cache, retained children, dependency tables, composition tables, stored failures or weak references outside the advice table.

## Aspects and advice

- **Advice comes from wrappers.** Every wrapping base, such as `WrappingMutator`, already splits its work into preparing state once and wrapping a supplied child execution; 26 wrappers implement that hook today. The hook returns `Advice<TExecution>`, a delegate from the inner execution to the wrapping execution. The base's ordinary factory resolves its configured child and applies the same advice, so an explicitly configured wrapper behaves as before.
- **The recipe chooses the advice.** `AspectBuilder.Advise` receives a selector and a recipe that maps each selected configuration to a wrapper of the same role, such as `source => source.CountCalls(counter)`. The resolver uses only the wrapper's advice; the wrapper's own child reference matters only when the wrapper is configured explicitly. The target can be any configuration, including a composite such as a `PipelineMutator`.
- **Ordering.** An aspect keeps advice declared with `Advise` and aspects added with `Install` in two ordered lists and applies the declared list first. Adding an aspect to a scope places its parts before the existing ones in each list. This yields today's order without depth counters or sorting: declared advice inside installed aspects, later layers inside earlier ones, earlier declarations inside later ones. Installing the same aspect twice applies it once.
- **Lifetimes.** Prepared advice lives in the state that holds its target, weakly keyed by aspect. It disappears with the state or with the aspect, so a long-lived source never retains a discarded scope's aspect, and a live aspect never retains discarded fresh state. A deferred node retains its construction scope and therefore everything it needs.

## Parameter logic belongs to the algorithm

Decided on 2026-10-13: an algorithm resolves only its configured parameters and creates no operators of its own. Behavior derived from a parameter belongs in the algorithm's execution code. The execution graph therefore matches the configuration the user supplied, apart from aspect advice.

The genetic algorithm's mutation rate is the example. The algorithm resolves `Mutator` and applies the rate in its iteration step, with the same `WeightedRouter` that `ChooseOneMutator` uses:

```csharp
private readonly WeightedRouter<IMutatorExecution<TCandidate, TSearchSpace, TProblem>?> mutationRouter = WeightedRouter.AtRate(mutator, mutationRate);

offspring = mutationRouter.Route(offspring, random, (selected, batch) =>
    selected is null ? batch : selected.Mutate(batch, random, problem.SearchSpace, problem));
```

The rate logic exists once, observers of the mutator count only the offspring it actually mutates, and the resolver needs no mechanism for it. `AppliedAtRate` stays what it is today: `ChooseOne[source, NoChange]`, a helper for users building configurations. NSGA2 and ALPS follow the same pattern.

The developer guidelines state the rule; nothing enforces it. An algorithm that resolves a configuration it builds itself makes that configuration visible to node selectors, so selection can differ from what the user configured. That mismatch is an accepted cost of removing the feature. The rejected alternatives are recorded in the [developer backlog](developer-backlog.md#internally-derived-configurations-as-selectable-sources).

## Aspects replace execution modules

Decided on 2026-10-13: `IExecutionModule` is removed. A module does three things today, and `Aspect` covers each:

- **Grouping.** A module bundles several declarations, such as an analyzer observing several boundaries; an aspect is a composite.
- **Installing once.** A module is installed at most once per builder, by reference; `Install(aspect)` does the same.
- **Outer placement.** A module's wrappers sit outside configuration-level wrappers, so observers see the fully configured operator and duration budgets do not measure observer callbacks. `Install(aspect)` places an aspect outside the advice declared with `Advise`.

A module that depends on another, such as a trace reading its clocks, becomes an aspect that installs the clock aspect; installing once keeps one clock for several traces. Runs attach aspects with one `Attach(aspect)`.

**Analyzers are aspects**, decided on 2026-10-13. An analyzer is an aspect that also owns results: it declares one or several pieces of advice, observing one or more operators, and records what they report. This follows AOP, where aspects may hold state, and keeps the earlier decision that an analyzer is the installable unit rather than an object exposing one. The `IAnalyzer` marker carries no behavior and is removed; `AccumulatingAnalyzer` and `TraceAnalyzer` derive from `Aspect`:

```csharp
public sealed class TraceAnalyzer<TResult> : Aspect
{
    protected override void Declare(AspectBuilder aspect)
    {
        foreach (var clock in clocks)
            aspect.Install(clock);
        aspect.Observe(boundary, observation => Record(observation));
    }

    public TResult Result => ...;
}
```

`Aspect` is therefore an abstract class. A subclass declares its advice in `Declare`, which runs once per aspect; the result is kept, so advice identity stays stable wherever the aspect is installed. `Aspect.Create(declare)` returns an aspect backed by that callback, for budgets and ad-hoc use.

**Per-trial aspects** use `AspectFactory<TInput, TAspect>`, decided on 2026-10-13. It replaces `TrialModule` and lives in the execution namespace: the factory depends only on its input, while `ExperimentRun.AttachPerTrial`, `GetAttached` and `TrialAttachment` keep everything trial-specific. It stays a named object because `GetAttached` finds results by the factory's reference. `TrialAnalyzer.Create` becomes redundant and is removed.

```csharp
var quality = AspectFactory.Create((GeneticAlgorithm<RealVector> trial) =>
    trial.TracePopulationQuality(clocks: [Clock.FromEvaluations(trial.Evaluator)]));
var run = algorithm.Repeat(2).CreateRun(problem, random).AttachPerTrial(quality);
await run.CompleteAsync();
foreach (var (trial, analyzer) in run.GetAttached(quality)) ...
```

## Authoring

- **Leaf operators and algorithms:** unchanged. `ExecutionFactory<TExecution>(ResolutionScope scope)` keeps its current shape, and algorithms keep writing `scope =>`.
- **Wrappers:** each wrapping base's hook returns `Advice<TExecution>` instead of `WrapperExecutionFactory<TExecution>`. Concrete wrappers such as `CountingMutator` keep their bodies: `inner => new Execution<TRunSearchSpace, TRunProblem>(inner, Counter, Metric)`.
- **Composites:** multi bases and `CompositeExecutionFactory` are unchanged; composites are configured operators, not advice.
- **Migrations:** the observing wrappers and Dynamic Racing's performance-tracking evaluator move onto their role's wrapping base. Algorithms get a wrapping base for algorithm observation. Module implementations become aspects.
- **Generic bases shared across roles** remain [rejected](developer-backlog.md#typed-operator-invocation).

## How existing operators look

- **Leaf operators** such as `SwapMutator`: unchanged. Leaf nodes stay the same object because their factory returns the node it prepared.
- **Wrappers and composites** such as `CountingMutator` and `PipelineMutator`: unchanged apart from the hook's return type.
- **Algorithms** such as the genetic algorithm: unchanged factories; GA, NSGA2 and ALPS apply their mutation rate in the iteration step instead of resolving `composedAs:`.
- **Operator budgets** build their aspect once:

  ```csharp
  var counter = new CountAccumulator();
  var counting = Aspect.Create(aspect =>
      aspect.Advise(ObservedOperator, source => CountedOperatorFactory(source, counter)));
  return scope => new OperatorBudgetAlgorithmExecution<...>(
      scope.With(counting).Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), counter, MaximumCount);
  ```

  Rebuilding a budget under a child scope with a new observer keeps the inner algorithm's state and the single declaration, and the observer sees the new calls.

- **Cycle** resolves each activation through `scope.CreateChildScope()` in fresh mode and through its own construction scope in reuse mode; no keys remain. Reconstructed under a new observer, its activations use Cycle's state with the requesting scope's aspect, while the original Cycle node keeps its own.
- **Configuring, running and analyzing** algorithms: unchanged. Users who wrote their own modules write aspects instead.

## Removed feature lines

These are deleted with the redesign rather than migrated. Removing internal compositions does not depend on the new resolver and can be the first implementation package.

Internal compositions:

- `Execution/Resolution/InternalComposition.cs`.
- `ResolutionScope.Resolve(configuration, prepare, composedAs)` and both `Resolve(mutator, composedAs)` overloads in the mutator role extensions.
- `ExecutionBindings.BindComposition`; the composition tables, `internalSource`, `IsInternal` and the internal-source check in `ExecutionSharingScope`; the composition table in `ExecutionPreparation`; the internal exclusion in `WrapperRegistration.Matches`; the related remarks on `NodeSelector` and `ResolutionScopeBuilder.Wrap`.
- The derived `rated` configurations in GA, NSGA2 and ALPS, replaced by rate dispatch in their execution code.
- `ExecutionFactoryResolutionTests.Compositions.cs`, nine composition tests. `AlpsMutationCompositionTests` and the rate cases in `MutatorSelectionTests` keep their behavioral assertions: selectors see only configured mutators, observers count only mutated offspring, seeded results match an explicit `AppliedAtRate`. `NodeSelectionSpecs` stays as it is.
- Documentation: the internal-composition section, component row and failure row in [execution resolution](../docs/contributing/architecture/execution-resolution.md); the glossary entry and the mentions in the configuration-selection and wrapper-registration entries; the two paragraphs in the [developer guidelines](../docs/contributing/developer-guidelines.md), replaced by the rule above.

Execution modules:

- `Execution/IExecutionModule.cs` and `ResolutionScopeBuilder.Install(IExecutionModule)`.
- The module implementations in analysis tracing (`Analyzer.Traces`, `Clocks`, `TraceAnalyzer`, the five observation adapters), `TrialModule`, `ExperimentRun`, `AlgorithmRun` and the Experimental `DynamicProblem`, which become aspects; `IAnalyzer` and `TrialAnalyzer.Create`.
- Eight test files that implement or use modules.
- Documentation: the glossary entry, [analyzers](../docs/contributing/architecture/analyzers.md), [execution nodes](../docs/contributing/architecture/execution-nodes.md), [layering](../docs/contributing/architecture/layering.md), and the user guides on [experiments](../docs/guide/execution/experiments.md) and [observability and analysis](../docs/guide/execution/observability-and-analysis.md).

## Where today's parts go

- `ExecutionSharingScope` → the scope's private `State`, without retained children, composition tables, markers and weak declaring parents.
- `ExecutionPreparation`, `PreparedWrapper`, `NodeConstruction` → removed. Factories and advice are stored directly; failures are not stored.
- `ExecutionBindings` → removed. Ordering moves into `Aspect`; construction moves into `ResolutionScope`.
- `RetainedChildScope`, `GetOrCreateChildScope` → removed. Callers use their own scope, `With` or `CreateChildScope`.
- `WrappedNodes` → removed. Advice receives the execution it wraps as its argument.
- `WrapperRegistration` → `Aspect`, a composite.
- `InternalComposition`, `Resolve(..., composedAs)` → removed; algorithms apply parameter logic in their execution code.
- `IExecutionModule`, `IAnalyzer` → `Aspect`; analyzers derive from it.
- `TrialModule`, `TrialAnalyzer` → `AspectFactory<TInput, TAspect>`.
- `ResolutionScopeBuilder` → `AspectBuilder`; `Wrap` → `Advise`.
- `WrapperExecutionFactory<TExecution>` → `Advice<TExecution>`.
- Preparation lambdas in role resolve extensions → one `ExecutionRole` class per role.
- Construction frame → an ordinary `ResolutionScope`: the owner's state and the requester's aspect.

Unchanged: `NodeSelector`, `ExecutionFactory`, `CompositeExecutionFactory`, the configuration and execution marker interfaces, typed scopes (`For`) and `ExecutionSignature`.

## Controls and type compatibility

- Controls project from the raw node, while operations use the advised chain. Projection runs after construction succeeds; an absent capability stays absent.
- A stored factory keeps the role of its first selection; delegate covariance turns compatibility into a type test. A later request for an incompatible role fails clearly, and so does a recipe result that is not a wrapper of the target's role.
- Selector and recipe types remain independent; the checks sit at the erased role boundary, not at operation time.

## Failures

| Failure                                           | Result                                                                     |
| ------------------------------------------------- | -------------------------------------------------------------------------- |
| Factory preparation, recipe or advice preparation | Propagates; nothing is stored, so a later resolution tries again.          |
| Construction, including a selector predicate      | Propagates; nothing was published, so a later resolution constructs again. |
| Recipe result not a wrapper of the target's role  | Fails when the advice is prepared.                                         |
| Structural recursion                              | Detected by configuration across a root state and all its child states.    |
| Control projection                                | Propagates; the constructed nodes are discarded.                           |
| Operation or advice callback                      | Normal invocation failure; resolution is not involved.                     |

`TryResolve` converts `InvalidOperationException` into a reason and propagates other exception types. The resolver gives no disposal or concurrency guarantees.

## Behavior changes

Each change follows from the design. The current tests that pin the old behavior are rewritten deliberately.

Decided on 2026-10-13: no node cache. Resolution runs once per run, or once per activation in a meta-algorithm, while operator execution dominates runtime. Shared state lives in the stored factories, so caching nodes would only preserve object identity and failure timing. Changes 1 to 7 are accepted.

1. **Node identity.** Before: resolving the same configuration twice in one scope returns the same node. After: composites, wrappers and algorithms get new nodes over the same state; leaf nodes stay the same object; observers report the same counts. Affects R1 identity assertions.
2. **Failures are not stored.** Before: preparation, wrapper and construction failures were stored and rethrown. After: a later resolution tries again. Affects R13 failure-timing assertions.
3. **Cycle reuse mode.** Before: `Cycle_RetainedSlotsUseReferenceIdentityAndBelongToTheirOwner` keeps the algorithm's state per Cycle owner and separate from a direct resolution of the same algorithm in the same scope, and sibling algorithms do not share dependencies they first select. After: within one state, the same algorithm reference has one state, whether reached through either Cycle owner or directly, and siblings share common dependency references.
4. **Budget inner state.** Before: a budget's inner algorithm lives in the budget's retained child. After: it lives in the budget's state and shares state with other uses of the same reference there. The budget's advice still applies only to calls through the budget.
5. **Advice is a single-child wrapper of the target's role.** Before: a recipe could return any configuration of the role that resolves its source, such as a composite containing it. After: anything else fails. All eight production recipes already return single-child wrappers.
6. **Advice has no children of its own.** Before: a generated wrapper could resolve additional children (R8), with private selection and self-exclusion rules. After: advice wraps only its target; real work belongs in the configuration, or the advice builds a helper privately during preparation. No production recipe resolves additional children.
7. **Algorithms own their parameter logic.** Before: GA, NSGA2 and ALPS resolved their mutator through a hidden `ChooseOne[mutator, NoChange]` composition. After: they dispatch in their iteration step and create no operators of their own. Observations do not change; seeded results, including rate 0, must match.

Users who want separate state pass value-equal distinct configurations (`algorithm with { }`) or use a fresh child scope, such as Cycle's fresh mode or a Pipeline stage.

## Open questions

None. The remaining implementation packages await approval.

## Changes from the earlier candidate

- Child scopes still belong to their callers. The earlier candidate left the resulting state loss open; separating state from aspects resolves it without retention.
- Advice receives the execution it wraps as its argument. The earlier candidate passed a supplied `inner` through every factory, with an `IExecutionFactory<T>` interface and the `Create`, `FromChild` and `FromChildren` helper family; this design leaves ordinary factories unchanged.
- Remembered dependency selections, the provisional node cache, helper tables, stored failures and the wrapper ancestry rule are removed rather than carried forward as open policies.
- Internal compositions are removed; algorithms apply parameter logic in their execution code.
- `Aspect` replaces `IExecutionModule`.

## Sequence

1. **Approve the design.** Stop for approval.
2. **Remove internal compositions.** On the current resolver, move the mutation rate into GA, NSGA2 and ALPS and delete the [internal-composition feature line](#removed-feature-lines), including its tests and documentation. Verify seeded parity and selector behavior. This package is independent of the new resolver and stops for review.
3. **Implement the core.** Replace the remaining production types with `ResolutionScope`, its private `State`, `Aspect` and the role classes; change the wrapping bases' hook to `Advice<TExecution>`; replace `IExecutionModule` with `Aspect`; migrate the observing wrappers, the racing evaluator, analyzers, Cycle and both budgets. Rewrite the retention, node-identity, failure-timing and additional-child tests deliberately; keep the shared-state, advice, ordering and algorithm-result assertions.
4. **Integrate consumers and documentation.** Migrate every repository caller without shims; update analyzers and code fixes, API usage specs, samples, the user guides, the glossary entries that describe preparations, bindings, wrapper registrations, wrapper chains, execution modules and analyzers, and the [execution resolution](../docs/contributing/architecture/execution-resolution.md) page.
5. **Review size, cost and behavior.** Compare resolver size and caller code with `c6957cbc`. Measure construction, repeated resolution, changed aspects, fresh stages and cheap operation calls. Complete collection, seeded streaming and whole-solution validation.

Follow `AGENTS.md` and `test/README.md`: focused core checks during development, then core, API usage and affected Experimental consumers; representative scenarios and whole-solution validation near completion. Build and test work is sequential.

## Step 2 review record — 2026-10-13

Scope: GA, NSGA2 and ALPS resolve their configured mutator normally and apply the rate at the existing mutation call through an execution-owned `WeightedRouter`, using the same step RNG. Each execution constructor creates a router holding the resolved mutator and a null pass-through choice; the execution retains only that router for mutation. Certain routes pass the original offspring to their handler without random draws. The internal-composition feature line and its nine test methods are removed from the current resolver. `AppliedAtRate` keeps its existing configuration behavior; `NodeSelectionSpecs` is unchanged.

Initial public API decision: the user selected making the existing sealed `WeightedBatchDispatcher` public, including its constructor and three `Dispatch` overloads. This lets Experimental and external algorithm authors share the existing batch-routing implementation. No friend-assembly access or separate rate helper was added. Its XML contract, authoring documentation and an API usage spec describe the exposed behavior.

Parity: 27 seeded cases, nine each for GA, NSGA2 and ALPS, compare every generation with explicit `AppliedAtRate` configuration (or the existing direct shortcut). Rates are `0`, `0.35`, `0.5`, `-0.25`, `NaN`, negative infinity, `1`, `1.5` and positive infinity. The mutator consumes the batch RNG, and the evaluator draws from it afterward, so the checks cover mutation routing and the downstream random stream even at rate zero. ALPS also compares ages. Broad selectors see only configured mutators; observers count only mutated offspring; zero, negative and NaN rates do not invoke the source, including `NoChangeMutator.Instance`. A paused GA retains its original mutator binding when another context adds an observer.

Validation is sequential, using `AGENTS.md` commands:

| Check                                            | Result                                                                                                      |
| ------------------------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| Restore and Release build                        | Passed; existing build warnings remain.                                                                     |
| Focused suites                                   | Passed: mutation selection 39, weighted operators 39, GA 1, NSGA2 1, current resolver 46, ALPS mutation 18. |
| Complete core, API usage and Experimental suites | Passed: 2,534, 197 and 239 cases respectively.                                                              |
| Whole solution, one test module at a time        | Passed: 2,996 cases, zero failures or skips.                                                                |
| Whitespace, style and analyzer verification      | All three passed; each reported workspace-loading warnings.                                                 |
| Published documentation build                    | Passed.                                                                                                     |
| Changed Markdown Prettier check                  | Passed after formatting the four changed files flagged by the initial check.                                |
| Reference inventory and Git checks               | No live references to removed composition members; no agent staging or unstaging.                           |

Initial build failures were corrected before the successful validation: `CS1525: Invalid expression term 'public'` and `CS1002: ; expected` from a missing `Matches` expression; `CS0246: The type or namespace name 'UnrestrictedSearchSpace<>' could not be found` and consequent conversion errors from a test-only type used in the API usage spec. The spec supplies its own search space through the public contract. The initial Prettier check reported `Code style issues found in 4 files. Run Prettier with --write to fix.`; those four changed Markdown files were formatted before the passing check.

Review follow-up: dispatcher construction belongs to each GA, NSGA2 and ALPS execution constructor, rather than execution factory creation. Repeated validation passed: Release build, all 144 focused cases, the 2,996-case solution run, all three .NET format checks and the documentation build. Markdown emphasis follows `_italics_` and `**bold**`, documented in developer guidelines § 9.8 and consistent with Prettier and the [GitLab handbook](https://handbook.gitlab.com/docs/markdown-guide/); [CommonMark](https://spec.commonmark.org/0.31.2/#emphasis-and-strong-emphasis) accepts both delimiter forms. An audit of 62 first-party Markdown files normalized 19 italic phrases in three files, verifying unchanged semantic syntax trees before writing. The initial follow-up Prettier check reported `Code style issues found in 2 files. Run Prettier with --write to fix.`; both files were formatted. The dispatcher name is unchanged; naming alternatives remain review suggestions.

Router follow-up, decided on 2026-10-13: `WeightedBatchDispatcher` becomes `WeightedRouter<TChoice>`. "Route" describes the purpose for one input or many, whereas "dispatcher" suggests threads and "chooser" suggests a call that returns a choice. The router takes its choices at construction, as `WeightedItemSampler<T>` does, which removes the operator count, the per-call count check and the algorithms' separate choice arrays. `WeightedRouter.AtRate(rate)` routes to `true` or `false` and holds the NaN weight rule that the three `AppliedAtRate` helpers also use. A choice that receives every input gets the original inputs, which replaces the algorithms' direct-call shortcut; a certain choice draws no random numbers, so seeded results are unchanged. Handlers are plain capturing lambdas, and the state-passing overloads are gone: one closure per routing call is accepted against the design goal of avoiding delegate creation, because routing already allocates its batch arrays per call. The internal `WeightedDispatcher<TOperator>` duplicated `WeightedItemSampler<T>`; `ChooseOneSelector` and `ChooseOneReplacer` now sample from it directly. Following developer guidelines § 8.6, the `WeightedRouter` companion offers `Create(choices)` and `Create(choices, weights)` beside `AtRate(rate)`, so callers never spell the choice type. Validation passed: Release build; core, API usage and Experimental suites with 2,543, 199 and 239 cases; the 3,007-case solution run; all three .NET format checks; the documentation build and Prettier on the changed Markdown.

Mutator ownership follow-up, approved on 2026-10-13: `WeightedRouter.AtRate<TChoice>(choice, rate)` returns `WeightedRouter<TChoice?>` for a reference choice, using the existing rate weights and null as the second choice. GA, NSGA2 and ALPS initialize it with their resolved mutator. The mutator constructor parameter is used only in that initialization; handlers receive the selected execution, so no separate mutator capture or algorithm-owned choices array remains. The boolean `AtRate(rate)` overload retains its decision-oriented use. Eight new unit cases cover reference identity, null routing and extreme-rate RNG behavior; the API usage spec exercises inference with a resolved mutator. Validation passed: Release build; 161 focused cases; complete core, API usage and Experimental suites with 2,551, 199 and 239 cases; the 3,015-case solution run with no failures or skips; all three .NET format checks and the documentation build. The 27 seeded algorithm parity cases and observer assertions still pass. Changes remain uncommitted, and the user's staged content is preserved.

Changed files (35, including deletions, the router and ownership follow-ups, and the new usage spec):

- Algorithms: `src/HeuristicLib/Algorithms/Evolutionary/GeneticAlgorithm.cs`, `src/HeuristicLib/Algorithms/Evolutionary/NSGA2.cs`, `src/HeuristicLib.Experimental/Algorithms/Evolutionary/ALPSGeneticAlgorithm.cs`.
- Resolver: `src/HeuristicLib/Execution/NodeSelector.cs`, `src/HeuristicLib/Execution/ResolutionScope.cs`, `src/HeuristicLib/Execution/ResolutionScopeBuilder.cs`, `src/HeuristicLib/Execution/Resolution/ExecutionBindings.cs`, `src/HeuristicLib/Execution/Resolution/ExecutionPreparation.cs`, `src/HeuristicLib/Execution/Resolution/ExecutionSharingScope.cs`, `src/HeuristicLib/Execution/Resolution/WrapperRegistration.cs`; deleted `src/HeuristicLib/Execution/Resolution/InternalComposition.cs`.
- Operators: added `src/HeuristicLib/Operators/WeightedRouter.cs`; deleted `src/HeuristicLib/Operators/WeightedBatchDispatcher.cs` and `src/HeuristicLib/Operators/WeightedDispatcher.cs`; `src/HeuristicLib/Operators/Mutators/IMutator.cs`, `src/HeuristicLib/Operators/Mutators/ChooseOneMutator.cs`, `src/HeuristicLib/Operators/Creators/ChooseOneCreator.cs`, `src/HeuristicLib/Operators/Crossovers/ChooseOneCrossover.cs`, `src/HeuristicLib/Operators/Refiners/ChooseOneRefiner.cs`, `src/HeuristicLib/Operators/Selectors/ChooseOneSelector.cs`, `src/HeuristicLib/Operators/Replacers/ChooseOneReplacer.cs`.
- Tests: `test/HeuristicLib.Tests/Execution/MutatorSelectionTests.cs`, `test/HeuristicLib.Tests.Experimental/Algorithms/AlpsMutationCompositionTests.cs`, `test/HeuristicLib.Tests/Operators/ChooseOneOperatorTests.cs`; added `test/HeuristicLib.Tests.ApiUsageSpecs/Operators/WeightedRouterSpecs.cs`; deleted `test/HeuristicLib.Tests/Execution/ExecutionFactoryResolutionTests.Compositions.cs`.
- Documentation: `docs/contributing/architecture/execution-resolution.md`, `docs/contributing/architecture/operator-implementation.md`, `docs/contributing/developer-guidelines.md`, `docs/guide/glossary.md`, `docs/guide/fundamentals/invariants-and-validation.md`, `plans/execution-resolution-redesign.md`, `plans/execution-factory-design.md`, `plans/container-and-aspect-framing-scope-review-prompt.md`, `plans/developer-backlog.md`.

Deferred: steps 3–5 remain unstarted. Execution modules, retained children, source interception for generated wrappers and node caches remain in the current resolver. Step 2 is pending review and uncommitted. The user's staged review changes are preserved; follow-up edits are left in the working tree without agent staging or unstaging.

## Validation scenarios

Use [R1-R16](execution-bindings-and-shared-state.md#acceptance-matrix-for-the-implementation) as a scenario inventory, adjusted by the accepted behavior changes. Reuse existing coverage first.

| Area                    | Cases                                                                                                                                                                                                                         |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Shared state            | Reference identity versus value-equal configurations; parent/child/sibling order without hoisting; outer/inner counts 5/2; old nodes keep their advice.                                                                       |
| Owner's state           | An earlier child-local M does not replace G's M or its control; deferred resolution from a retained construction scope uses G's state and original aspect.                                                                    |
| Advice                  | Declared/installed order, layer and sequence; unmatched and overlapping selectors; advice prepared once per aspect and target; composite targets; recipe results that are not wrappers of the role fail.                      |
| Callers                 | Cycle fresh and reuse modes, repeated references and invocations; Pipeline stages; racing contenders; both budgets rebuilt under a new observer, including clock/trace order. RNG forks, cancellation and disposal unchanged. |
| Mutation rates          | GA, NSGA2 and ALPS match their previous seeded results, including rate 0; observers of the mutator count only mutated offspring; broad selectors see only configured mutators.                                                |
| Aspects for analysis    | Analyzers, traces with shared clocks, experiment trials and the dynamic-problem update apply through aspects; installing an aspect twice applies it once; installed aspects sit outside declared advice.                      |
| Controls and roles      | Control from the raw node despite advice; absent capability stays absent; incompatible run types fail clearly; custom external roles and wrappers.                                                                            |
| Failures and collection | Repeated preparation and construction after failures; recursion across child states; both collection directions; live deferred nodes retain their scope.                                                                      |

## Sources

- Production baseline: `c6957cbc` on `container-and-aspect-framing`; the earlier candidate in `68ad810c`.
- [Developer guidelines](../docs/contributing/developer-guidelines.md), [design goals](../docs/contributing/design-goals.md), [requirements](../docs/contributing/requirements.md) and [glossary](../docs/guide/glossary.md).
- [Current resolution](../docs/contributing/architecture/execution-resolution.md), [factory design](execution-factory-design.md) and [current acceptance evidence](execution-bindings-and-shared-state.md).
- Rejected alternatives: [developer backlog](developer-backlog.md#discussed-tried-and-rejected).
