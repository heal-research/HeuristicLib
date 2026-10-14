# Execution-resolution redesign

The aspect, advice and recipe parts of this plan are superseded by the [aspect redesign](aspect-redesign.md), decided on 2026-10-14 and not yet implemented. The working tree still implements them as recorded here. State, scope, Cycle and budget-placement decisions remain valid.

Status: final design, 2026-10-13. Step 2 is accepted and committed at `103a2b0f`. Step 3 is implemented in the working tree, uncommitted, pending review; steps 4–5 remain deferred. This is the only active plan for replacing the execution resolver. It replaces the candidate recorded in `68ad810c` with a smaller design; the [differences](#changes-from-the-earlier-candidate) are listed below. Decided on 2026-10-13: the resolver has no node cache; advice receives the execution it wraps directly; algorithms create no operators of their own; `Aspect` replaces `IExecutionModule`; analyzers are aspects; behavior changes 1 to 7 are accepted; `Pointcut`, `Aspect`, `AspectBuilder` with `Install`, `Advice<TExecution>`, the wrapping bases' `CreateWrapperFactory` hook, `AspectFactory` and "advice chain" are the chosen terms. Review discussions are listed under Open questions; step 3 is implemented, uncommitted, pending review; steps 4–5 remain deferred. Typed advice, package 5 of [container and aspect framing](container-and-aspect-framing.md), stays on hold. [Execution bindings and shared state](execution-bindings-and-shared-state.md) and [execution factory design](execution-factory-design.md) record the production baseline, `c6957cbc`, and its history.

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

A resolution scope combines its **state**, which decides which state each configuration uses, with an ordered collection of **aspects**, which decides which advice its executions receive.

- **State.** The scope's state keeps the factory each configuration created, and with it the execution state that factory captured. A configuration already held by a parent scope's state uses that factory; otherwise the factory is created once. A factory resolves its own dependencies **in the state that holds it**, with the aspect of the scope that asked. This replaces dependency pinning: an earlier or later selection elsewhere cannot change which state a composite uses, while new advice still reaches its children.
- **Aspect.** An aspect pairs a `Pointcut` with advice for the configurations it selects. An aspect declares any number of such pairs and may install other aspects. A scope holds an immutable, ordered collection of zero, one or more actual aspects; it creates no synthetic empty or combined aspect.
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

```csharp
(TExecution Raw, TExecution Advised) Construct<TConfiguration, TExecution>(
    TConfiguration configuration,
    Func<TConfiguration, ExecutionFactory<TExecution>> prepareFactory,
    Func<IConfigurationNode, Advice<TExecution>> prepareAdvice)
{
    var (factory, owner) = state.GetOrAdd(configuration, prepareFactory);
    var raw = factory(new ResolutionScope(owner, aspects, adviceDeclarations));
    var advised = raw;
    foreach (var declaration in adviceDeclarations.Where(item => item.Matches(configuration)))
        advised = owner.GetOrAddAdvice(declaration, configuration, prepareAdvice)(advised);
    return (raw, advised);
}
```

The private `State` retains successful factories by configuration reference and advice by weak declaration identity plus target reference. It reuses ancestor factories and constructs dependencies in the factory owner's state. A root and its children share recursion detection. No node cache, dependency-pinning table, retained child slots or failures are stored.

Operator and algorithm resolution extensions supply typed factory preparation callbacks and typed advice preparation functions. Custom execution contracts supply the same pair; no `ExecutionRole` classes or registry are required. A wrapping base's one protected `CreateWrapperFactory` hook returns `WrapperExecutionFactory<TExecution>`; the resolution extension calls the base's public static `CreateWrapperFactory(wrapper)` bridge and obtains `Advice<TExecution>` from the returned constructor's `Invoke` method. Ordinary `ExecutionFactory` and `CompositeExecutionFactory` contracts stay unchanged.

A scope snapshots its actual aspect inputs. Inputs in one call form one layer in supplied order; a later `With` layer precedes existing layers. Each aspect caches one ordered declaration snapshot. Scope collection expands compositions depth-first at their installation position; the first occurrence of an aspect determines its position, with reference-based sharing and cycle deduplication. These are declaration snapshots, not execution-node caches.

## Aspects and advice

Superseded by the [aspect redesign](aspect-redesign.md): advice wraps executions through per-role advice bases instead of recipes producing wrapper configurations, an aspect stores pointcut/advice pairs without compositions, and the scope holds one flat list. Ordering and the weak lifetime of per-target advice state are kept there.

## Parameter logic belongs to the algorithm

Decided on 2026-10-13: an algorithm resolves only its configured parameters and creates no operators of its own. Behavior derived from a parameter belongs in the algorithm's execution code. The execution graph therefore matches the configuration the user supplied, apart from aspect advice.

The genetic algorithm's mutation rate is the example. The algorithm resolves `Mutator` and applies the rate in its iteration step, with the same `WeightedRouter` that `ChooseOneMutator` uses:

```csharp
private readonly WeightedRouter<IMutatorExecution<TCandidate, TSearchSpace, TProblem>?> mutationRouter = WeightedRouter.AtRate(mutator, mutationRate);

offspring = mutationRouter.Route(offspring, random, (selected, batch) =>
    selected is null ? batch : selected.Mutate(batch, random, problem.SearchSpace, problem));
```

The rate logic exists once, observers of the mutator count only the offspring it actually mutates, and the resolver needs no mechanism for it. `AppliedAtRate` stays what it is today: `ChooseOne[source, NoChange]`, a helper for users building configurations. NSGA2 and ALPS follow the same pattern.

The developer guidelines state the rule; nothing enforces it. An algorithm that resolves a configuration it builds itself makes that configuration visible to pointcuts, so selection can differ from what the user configured. That mismatch is an accepted cost of removing the feature. The rejected alternatives are recorded in the [developer backlog](developer-backlog.md#internally-derived-configurations-as-selectable-sources).

## Aspects replace execution modules

Decided on 2026-10-13: `IExecutionModule` is removed. A module does three things today, and `Aspect` covers each:

- **Grouping.** A module bundles several declarations, such as an analyzer observing several boundaries; an aspect is a composite.
- **Installing once.** A module is installed at most once per builder, by reference; `Install(aspect)` does the same.
- **Outer placement.** A module's wrappers sit outside configuration-level wrappers, so observers see the fully configured operator and duration budgets do not measure observer callbacks. `Install(aspect)` places an aspect outside the advice declared with `Install`.

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

**Per-trial aspects** use `AspectFactory<TInput, TAspect>`, decided on 2026-10-13. It replaces `TrialModule` and lives in the execution namespace: the generic-result factory derives from `AspectFactory<TInput>`, which bounds experiment dictionary keys; the factory depends only on its input, while `ExperimentRun.AttachPerTrial`, `GetAttached` and `TrialAspect` keep everything trial-specific. It stays a named object because `GetAttached` finds results by the factory's reference. `TrialAnalyzer.Create` becomes redundant and is removed.

```csharp
var quality = AspectFactory.Create((GeneticAlgorithm<RealVector> trial) =>
    trial.TracePopulationQuality(clocks: [Clock.FromEvaluations(trial.Evaluator)]));
var run = algorithm.Repeat(2).CreateRun(problem, random).AttachPerTrial(quality);
await run.CompleteAsync();
foreach (var (trial, analyzer) in run.GetAttached(quality)) ...
```

## Authoring

- **Leaf operators and algorithms:** unchanged. `ExecutionFactory<TExecution>(ResolutionScope scope)` keeps its current shape, and algorithms keep writing `scope =>`.
- **Wrappers:** each wrapping base has one protected `CreateWrapperFactory` hook returning `WrapperExecutionFactory<TExecution>`. Concrete wrappers such as `CountingMutator` keep their bodies: `inner => new Execution<TRunSearchSpace, TRunProblem>(inner, Counter, Metric)`.
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
      aspect.Install(ObservedOperator, source => CountedOperatorFactory(source, counter)));
  return scope => new OperatorBudgetAlgorithmExecution<...>(
      scope.With(counting).Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), counter, MaximumCount);
  ```

  Rebuilding a budget under a child scope with a new observer keeps the inner algorithm's state and the single declaration, and the observer sees the new calls.

- **Cycle** defaults to `ChildScopePolicy.Fresh`, creating a child scope per activation. `Retained` keeps caller-owned child state per inner algorithm reference in its prepared factory; `Shared` resolves through its own construction scope. Retained views contain no aspects and receive the requesting aspects through `WithAspectsFrom` on each activation. Reconstructed nodes use the new context, while an original node retains its own.
- **Configuring, running and analyzing** algorithms: unchanged. Users who wrote their own modules write aspects instead.

## Removed feature lines

These are deleted with the redesign rather than migrated. Removing internal compositions does not depend on the new resolver and can be the first implementation package.

Internal compositions:

- `Execution/Resolution/InternalComposition.cs`.
- `ResolutionScope.Resolve(configuration, prepare, composedAs)` and both `Resolve(mutator, composedAs)` overloads in the mutator role extensions.
- `ExecutionBindings.BindComposition`; the composition tables, `internalSource`, `IsInternal` and the internal-source check in `ExecutionSharingScope`; the composition table in `ExecutionPreparation`; the internal exclusion in `WrapperRegistration.Matches`; the related remarks on `Pointcut` and `ResolutionScopeBuilder.Wrap`.
- The derived `rated` configurations in GA, NSGA2 and ALPS, replaced by rate dispatch in their execution code.
- `ExecutionFactoryResolutionTests.Compositions.cs`, nine composition tests. `AlpsMutationCompositionTests` and the rate cases in `MutatorSelectionTests` keep their behavioral assertions: selectors see only configured mutators, observers count only mutated offspring, seeded results match an explicit `AppliedAtRate`. `NodeSelectionSpecs` stays as it is.
- Documentation: the internal-composition section, component row and failure row in [execution resolution](../docs/contributing/architecture/execution-resolution.md); the glossary entry and the mentions in the configuration-selection and wrapper-registration entries; the two paragraphs in the [developer guidelines](../docs/contributing/developer-guidelines.md), replaced by the rule above.

Execution modules:

- `Execution/IExecutionModule.cs` and `ResolutionScopeBuilder.Install(IExecutionModule)`.
- The module implementations in analysis tracing (`Clocks`, `TraceAnalyzer`, the five observation adapters), `TrialModule`, `ExperimentRun`, `AlgorithmRun` and the Experimental `DynamicProblem`, which become aspects; `IAnalyzer` and `TrialAnalyzer.Create`.
- Eight test files that implement or use modules.
- Documentation: the glossary entry, [analyzers](../docs/contributing/architecture/analyzers.md), [execution nodes](../docs/contributing/architecture/execution-nodes.md), [layering](../docs/contributing/architecture/layering.md), and the user guides on [experiments](../docs/guide/execution/experiments.md) and [observability and analysis](../docs/guide/execution/observability-and-analysis.md).

## Where today's parts go

- `ExecutionSharingScope` → the scope's private `State`, without retained children, composition tables, markers and weak declaring parents.
- `ExecutionPreparation`, `PreparedWrapper`, `NodeConstruction` → removed. Factories and advice are stored directly; failures are not stored.
- `ExecutionBindings` → removed. Ordering moves into `Aspect`; construction moves into `ResolutionScope`.
- `RetainedChildScope`, `GetOrCreateChildScope` → removed. Callers use their own scope, `With` or `CreateChildScope`; caller-owned retained state uses `CreateChildScopeWithoutAspects` and `WithAspectsFrom`. No keyed retention table is added to the resolver.
- `WrappedNodes` → removed. Advice receives the execution it wraps as its argument.
- `WrapperRegistration` → `Aspect`, an individual rule or a composition.
- `InternalComposition`, `Resolve(..., composedAs)` → removed; algorithms apply parameter logic in their execution code.
- `IExecutionModule`, `IAnalyzer` → `Aspect`; analyzers derive from it.
- `TrialModule`, `TrialAnalyzer` → `AspectFactory<TInput, TAspect>`.
- `ResolutionScopeBuilder` → `AspectBuilder`; `Wrap` → `Install`.
- `WrapperExecutionFactory<TExecution>` remains the child-taking constructor returned by the protected `CreateWrapperFactory` hook; resolution adapts it to `Advice<TExecution>`.
- Factory and advice preparation are gathered in role resolution extensions; standalone preparation-strategy classes are removed.
- Construction frame → an ordinary `ResolutionScope`: the owner's state and the requester's aspect.

The configuration-selection contract is unchanged; its public name is now `Pointcut` (formerly `NodeSelector`). `ExecutionFactory`, `CompositeExecutionFactory`, the configuration and execution marker interfaces, typed scopes (`For`) and `ExecutionSignature` are unchanged.

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
3. **Cycle state modes.** The boolean reset option is replaced by `ChildScopePolicy`: `Fresh` (default) creates child-local state per activation; `Retained` keeps Cycle-owned child-local state per inner algorithm reference; `Shared` uses the Cycle's owning state, including sibling and direct uses of the same reference. All child states reuse ancestor-owned factories. Retained child state belongs to the prepared Cycle and receives the requesting aspects on each activation; it does not retain the first observer. This supersedes the initially accepted single reuse policy.
4. **Budget inner state.** Before: a budget's inner algorithm lives in the budget's retained child. After: it lives in the budget's state and shares state with other uses of the same reference there. The budget's advice still applies only to calls through the budget.
5. **Advice is a single-child wrapper of the target's role.** Before: a recipe could return any configuration of the role that resolves its source, such as a composite containing it. After: anything else fails. All eight production recipes already return single-child wrappers.
6. **Advice has no children of its own.** Before: a generated wrapper could resolve additional children (R8), with private selection and self-exclusion rules. After: advice wraps only its target; real work belongs in the configuration, or the advice builds a helper privately during preparation. No production recipe resolves additional children.
7. **Algorithms own their parameter logic.** Before: GA, NSGA2 and ALPS resolved their mutator through a hidden `ChooseOne[mutator, NoChange]` composition. After: they dispatch in their iteration step and create no operators of their own. Observations do not change; seeded results, including rate 0, must match.

Users who want separate state pass value-equal distinct configurations (`algorithm with { }`) or use a fresh child scope, such as Cycle's fresh mode or a Pipeline stage.

## Open questions

The review follow-up choices are approved and implemented in the working tree; validation passed and user review is pending. No material design questions remain open. Steps 4-5 remain deferred.

## Changes from the earlier candidate

- Child scopes still belong to their callers. The earlier candidate left the resulting state loss open; separating state from aspects resolves it without retention.
- Advice receives the execution it wraps as its argument. The earlier candidate passed a supplied `inner` through every factory, with an `IExecutionFactory<T>` interface and the `Create`, `FromChild` and `FromChildren` helper family; this design leaves ordinary factories unchanged.
- Remembered dependency selections, the provisional node cache, helper tables, stored failures and the wrapper ancestry rule are removed rather than carried forward as open policies.
- Internal compositions are removed; algorithms apply parameter logic in their execution code.
- `Aspect` replaces `IExecutionModule`.

## Sequence

1. **Approve the design.** Stop for approval.
2. **Remove internal compositions.** On the current resolver, move the mutation rate into GA, NSGA2 and ALPS and delete the [internal-composition feature line](#removed-feature-lines), including its tests and documentation. Verify seeded parity and selector behavior. This package is independent of the new resolver and stops for review.
3. **Implement the core.** Replace the remaining production types with `ResolutionScope`, its private `State`, `Aspect` and the role classes; use the wrapping bases' protected `CreateWrapperFactory` hook and named wrapper factory; replace `IExecutionModule` with `Aspect`; migrate the observing wrappers, the racing evaluator, analyzers, Cycle and both budgets. Rewrite the retention, node-identity, failure-timing and additional-child tests deliberately; keep the shared-state, advice, ordering and algorithm-result assertions.
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

## Step 3 review record � 2026-10-13

Implemented in the working tree, uncommitted, pending review. Step 2 is accepted at `103a2b0f`; its historical review records above are preserved.

Scope: `ResolutionScope` now owns a private reference-keyed `State` containing successful prepared factories, weakly aspect-keyed advice and a recursion guard shared across child states. Dependencies construct in the factory owner's state with the requesting aspect. `Aspect` and `AspectBuilder` retain stable declaration identities, apply declared advice before installed aspects and deduplicate installations by reference. `With` shares state; child scopes create local state without hoisting. Node caches, retained child slots, pinned dependency tables, source interception, wrapper preparation records and stored failures are deleted rather than relocated.

Role-specific `ExecutionRole` classes prepare factories and obtain `Advice<TExecution>` through the single-child wrapping bases' `CreateAdvice` hook. Consumer-defined roles require no registry. Explicit wrappers resolve their configured child normally; aspect advice receives its current inner execution directly. Raw controls remain separate from advised operations. The observing wrappers use matching wrapping configuration/execution bases, including the new algorithm pair. Analyzers, clocks, trial attachments, Cycle, both operator budgets, Dynamic Racing and directly affected Experimental implementations use aspects. `NodeSelector`, ordinary/composite factory contracts, typed scopes and `ExecutionSignature` retain their contracts.

Accepted behavior changes 1�6 are reflected in identity, retry, Cycle reuse, direct budget sharing and invalid-advice tests. Coverage retains owner-state collisions and controls, deferred and paused contexts, advice ordering/overlap, composite targets, clock-before-trace ordering, RNG forks, cancellation, disposal and both collection directions. Added cases prove shared Cycle sibling dependencies and budget advice local to calls through the budget. Step 2's mutation routing and all 27 seeded GA/NSGA2/ALPS parity cases remain covered.

Minimal dependent integrations completed early: the execution-factory analyzer recognizes preparation of a role factory's own source; analyzer/code-fix fixtures, architecture probes, affected API usage specs and C# callers compile against the new contracts. Experimental rank analysis now has an aspect base for its typed reads, and its Python interop caller follows it. Broader tooling/API review and published guide, glossary and architecture migration remain step 4 work. Comparative size/performance measurement remains step 5 work.

Validation used the `AGENTS.md` commands:

| Check                                            | Result                                                                                                                                                                                                                            |
| ------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Release baseline and implementation builds       | Passed; baseline had 99 warnings and no errors. Existing warnings remain on recompilation; the new aspect warnings were corrected without suppressions. The final incremental Release build passed with zero warnings and errors. |
| Focused resolver/scope/topology/deferred checks  | Passed: 41 resolver, 26 scope, 20 topology and 20 deferred cases; both Cycle analysis scenario cases passed.                                                                                                                      |
| Complete core, API usage and Experimental suites | Passed: 2,548, 199 and 239 cases.                                                                                                                                                                                                 |
| Whole solution, `--max-parallel-test-modules 1`  | Passed: 3,012 tests, zero failures or skips.                                                                                                                                                                                      |
| Whitespace, style and analyzer verification      | All three passed; each reported workspace-loading warnings.                                                                                                                                                                       |
| Published documentation build                    | Passed.                                                                                                                                                                                                                           |
| Changed Markdown Prettier check                  | Passed after formatting this updated plan.                                                                                                                                                                                        |
| Final Git/index checks                           | Diff whitespace clean; branch and HEAD remain at the handover checkpoint; index empty and changes unstaged.                                                                                                                       |

The first whole-solution run exposed one remaining obsolete Cycle scenario expectation: `[1,1,2,2,3,3]` instead of the accepted shared dependency sequence `[1,2,3,4,5,6]`. All three trace assertions were updated, the focused scenario passed and the whole solution was rerun successfully. Initial line-ending and import-order check failures were corrected in the affected files. No unresolved validation failures remain. The updated plan passed Prettier after formatting. No staging, history or ref operations were performed.

### Review follow-up: aspect organization

At the user's request, the shared aspect infrastructure (`Advice`, `Aspect`, `AspectBuilder`, `AspectFactory`, `NodeSelector` and `Observation`) now lives in `Execution/Aspects`, retaining the `HEAL.HeuristicLib.Execution` namespace. The existing architecture checks reflect that grouping. Renamed primary types have matching filenames. Analysis implementations remain with their owning concepts.

Cycle reuse was checked against the approved state-sharing behavior; no isolation-policy change was made. The names and representation of the preparation adapters, and a possible AOP name for `NodeSelector`, remain discussion topics. This organization change does not adopt those proposals.

Follow-up validation passed: Release build (95 existing warnings, zero errors), all 2,548 core tests, all 199 API usage tests, all three .NET format checks, the changed-Markdown Prettier check and Git diff whitespace checks. Format checks reported the existing workspace-loading warning. The branch, HEAD and empty index are unchanged; all edits remain unstaged.

### Review follow-up: Pointcut naming

The user accepted `Pointcut` as the AOP name for configuration selection. `Pointcut.cs`, `PointcutTests.cs` and `PointcutSpecs.cs` replace their corresponding selector filenames; declarations, all C# callers, the glossary entry and direct documentation references use the new name. Reference identity, matching, composition and advice behavior are unchanged. Historical review records above retain the names used at their checkpoints.

The user's staged changes and unrelated workspace changes are preserved. The follow-up changes are unstaged. The Python adapter's remaining `IModuleSet` name was confirmed as a migration remnant; its removal is a discussion recommendation, not part of this naming edit. Broader published architecture migration remains deferred to step 4.

Pointcut follow-up validation passed: Release baseline (99 existing warnings, zero errors), Release build after the rename (95 existing warnings, zero errors), all 18 focused pointcut tests, the complete 3,012-test solution run with no failures or skips, all three .NET format checks, the published documentation build, changed-Markdown Prettier checks and Git diff whitespace checks. Format checks reported the existing workspace-loading warning. The user's Git index was verified unchanged by its SHA-256 hash; HEAD and the branch remain at the handover checkpoint.

### Approved follow-up: state modes and simpler preparation

The user approved the following revisions to step 3. They are implemented in the working tree, uncommitted, pending review; earlier review records remain historical.

- Cycle exposes reusable `ExecutionStateMode` choices: `Fresh` (default child state per activation), `Retained` (Cycle-owned child state per inner algorithm reference) and `Shared` (the Cycle's owning state). Child states continue to reuse ancestor-owned factories. Retained state must receive each requesting aspect context without retaining the first observer. Other meta-algorithms keep their existing behavior.
- Resolution scopes hold an explicit ordered collection of aspects, with zero, one or many inputs to `Create`, `CreateChildScope` and `With`. Declared/installed ordering, reference installation deduplication and once-only declarations remain unchanged. Synthetic empty/combined aspects are removed.
- Factory/advice bridging moves into operator and algorithm resolution extensions, including directly affected Experimental contracts. Standalone `ExecutionRole` classes are removed. Wrapping bases use one callable `PrepareWrapper` hook; ordinary configured construction and aspect advice use the same prepared child-taking constructor.
- Trace constructors receive immutable observation-aspect lists. Separate recording state breaks the construction cycle; no post-construction setter remains. Observation capture types move to `Analysis/Observations`.
- AlgorithmRun keeps its list and reference hash set, with a comment documenting insertion-order set behavior. ExperimentRun keys per-trial aspects by a bounded `AspectFactory<TAlgorithm>` contract and exposes `TrialAspect` associations. The unused Python `IModuleSet` interface is removed.

Review completion, 2026-10-14: all approved follow-up changes are implemented, uncommitted, pending review. Cycle's retained states belong to its prepared factory and are keyed by inner configuration reference; they contain no aspects. `CreateChildScopeWithoutAspects` and `WithAspectsFrom` are public state/context operations, so the same ownership can be authored outside the core assembly without a resolver-owned retention table. Multiple aspect inputs form one layer in supplied order; later layers keep the existing precedence.

The trace now owns separate recording state and a constructor-supplied immutable observer list. Observation and algorithm observation captures are in `Analysis/Observations`, avoiding an Algorithms-to-Analysis dependency. The list/hash-set pair on AlgorithmRun is documented as an insertion-ordered reference set. The Python interface and its unused result-reader argument are removed.

Minimal dependent integrations: all directly affected C# callers and authoring examples use the new contracts; the execution-factory analyzer accepts only preparation of a resolution callback's own source and still diagnoses direct child factories in wrapper preparation or deferred callbacks. Architecture fixtures recognize the public wrapper hook and current private declaration types without weakening layer checks. The active design, glossary and directly affected operator, meta-algorithm and experiment documentation are updated. Broader guide, tooling/API migration remains step 4 work; comparative size/performance study remains step 5 work.

Validation used the `AGENTS.md` commands:

| Check                                        | Result                                                                                                                                                                                       |
| -------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Release baseline                             | Passed, zero warnings and errors.                                                                                                                                                            |
| Release implementation builds                | Passed. Recompilation reports existing analyzer warnings; new source warnings were corrected without suppressions. The final incremental Release build passed with zero warnings and errors. |
| Focused state/scope/analyzer checks          | Passed: 25 deferred/state cases, 30 scope cases in the complete core run, 20 analyzer, 11 code-fix and 64 architecture cases.                                                                |
| Complete core/API usage/Experimental         | Passed: 2,559 / 199 / 239 tests.                                                                                                                                                             |
| Whole solution, sequential modules           | Passed: 3,024 tests, zero failures or skips, including 27 scenario cases.                                                                                                                    |
| Whitespace/style/analyzer verification       | All three passed; workspace-loading warnings remain.                                                                                                                                         |
| Published documentation build                | Passed.                                                                                                                                                                                      |
| Changed-Markdown Prettier and Git whitespace | Passed, including the final review-note update.                                                                                                                                              |
| Git authorization                            | No staging, history or ref commands performed. Existing staged blob entries are preserved; follow-up edits remain unstaged.                                                                  |

New coverage distinguishes all three state modes, retained reference identity and owner isolation, rebinding under a new observer, collection of a retired observer, ancestry reuse, ordered multiple aspects, cyclic/shared installation and narrow preparation diagnostics. Existing controls, deferred/paused contexts, budgets with clock/trace ordering, RNG forks, cancellation/disposal, racing and all 27 seeded GA/NSGA2/ALPS mutation-rate parity cases still pass.

Initial migration checks exposed the algorithm capture's old namespace dependency, two boolean fixture assignments, outdated reflection/probe expectations and missing explicit type arguments in one new negative analyzer fixture. They were corrected and the affected checks rerun. No unresolved validation failures remain. Historical review records above retain their checkpoint terminology and results.

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

### Approved follow-up: wrapper factories, state policy and trace ownership

Approved on 2026-10-14, after the state-mode/preparation review:

- Rename the choice to `ExecutionStatePolicy` and Cycle's setting to `StatePolicy`. One prepared `ExecutionScopeSelector` owns activation scope selection and retained child scopes by configuration reference. Cycle nodes share that selector across bindings; retained scopes keep no requesting aspects. Other meta-algorithms retain their existing behavior.
- Restore `WrapperExecutionFactory<TExecution>` and name the authoring hook `CreateWrapperFactory`. The public rename was compiled and its 20 topology tests passed before changing accessibility. The hook is protected; a public static `CreateWrapperFactory(wrapper)` bridge supports typed resolution callbacks without exposing an overridable preparation hook. All core and Experimental wrappers and consumer fixtures use the named delegate.
- Move shared trace construction into public `TraceAnalyzer<TResult>.Create` factories and inline recording storage into the trace. Its private constructor initializes storage before building the immutable observer list, so callbacks capture the trace directly. `Recording` and `Analyzer.Traces.cs` are removed. A consumer-defined cooling-role API usage spec covers direct trace creation.
- Make `Aspect.Declarations` private. Keep declaration collection and its carrier internal because they coordinate core builder/resolver machinery, while consumer aspects declare through `AspectBuilder`. Each `AdviceDeclaration` retains one pointcut/recipe registration's identity for independent once-only preparation per target.
- Rename the scope's flattened snapshot to `adviceDeclarations` and document that it caches ordered declarations, including installed aspects. Prepared delegates remain in the owning state's weak declaration cache. Correct the advice helpers' constraint indentation in core and Experimental role contracts.

Broader published architecture migration and comparative measurement remain deferred; earlier review records above describe their original checkpoints.

Follow-up validation, 2026-10-14:

- `dotnet restore` and `dotnet build --configuration Release --no-restore` passed. Recompiled projects retain existing warnings; no build errors or warning suppressions were introduced.
- Focused checks passed: 20 topology factory cases before tightening accessibility, 49 role-contract cases, 20 execution-factory analyzer cases and 9 pointcut/API usage cases including the consumer-defined trace.
- `dotnet test --solution ./HEAL.HeuristicLib.slnx --configuration Release --no-restore --max-parallel-test-modules 1` passed all 3,025 cases, with zero failures or skips. The first complete core run exposed the analyzer's missing named-wrapper-factory recognition; it was corrected before the focused analyzer check and final solution run.
- All three `dotnet format` verification commands passed; they reported the existing workspace-loading warning. The published documentation build, changed-Markdown Prettier check and `git diff --check` passed.

No Git mutation commands were run in this follow-up. The staged diff differed from the initial read-only snapshot during execution; those index changes were left intact.

### Approved follow-up: child scope naming and Cycle helpers

The user selected `ChildScopePolicy` on 2026-10-14. Cycle exposes it as `ScopePolicy`, and the prepared runtime selector is `ChildScopeSelector`. Every single-child/list and concrete/interface `CycleWith` overload accepts `scopePolicy`, after `maximumCycles`, defaulting to `ChildScopePolicy.Fresh`. Explicit policies use the same init setting as direct construction. All in-repository code and directly affected guides use this naming; earlier review records retain their checkpoint names.

The existing repeated-child theory exercises all three policies through `CycleWith`, and the inference usage spec spells both retained and shared policies. Default-policy composition examples retain their existing calls.
