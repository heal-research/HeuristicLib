# Operator state lifetimes

Status: requirements discussion. The defaults below are accepted design intent; no replacement API or implementation has been accepted. Published documentation continues to describe the current implementation.

## Agreed requirements

- Inner algorithms in a cycle start fresh by default on every turn. Reusing configuration references must not silently share their mutable execution state across those fresh activations.
- Users can explicitly opt selected operators into sharing across activations. An evaluation cache shared across all cycles and turns of one Cycle execution is the motivating example.
- Sharing is not hard-coded by operation or concrete operator type. Built-in and consumer-defined operators participate on the same terms.
- Configuration identity alone is insufficient to express sharing across fresh execution boundaries. Resolution order must not choose whether state is shared.

The mechanism for explicit sharing remains open. These requirements do not mandate automatic state ownership in the resolver; explicitly supplied mutable resources are now under consideration as an alternative.

## Existing behavior being reconsidered

The resolver currently reuses state already owned by ancestors. A child-first owner stays child-local when the parent resolves the configuration later. Therefore `Fresh` does not guarantee independence from ancestor-owned state. See [execution resolution](../docs/contributing/architecture/execution-resolution.md) and `ExecutorResolutionTests.ChildSelections_DoNotHoistOrChangeWhenParentLaterSelects`.

The current `ChildScopePolicy` applies to the inner activation as a whole. Selecting one such policy cannot express a shared cache alongside independently fresh termination state without additional composition.

## Candidate directions, not accepted

The three candidates being compared are 1.1, 1.2 and 2. The assessment below concerns user behavior and architectural feasibility, not a validated implementation. All proposed API sketches are illustrative.

For all three, a fresh inner turn means fresh private execution data, not a new population or discarded search result: Cycle can continue passing its current search state to the next turn. Calls within a turn reuse that turn's objects. Pausing and resuming the same execution must not silently reconstruct its objects. A separately started independent trial needs fresh objects and fresh resources unless the user deliberately supplies shared ones.

### 1. Fresh executors with explicitly supplied resources

The developer questioned whether the general sharing machinery is justified by the uncommon combinations it supports, and proposed independent executor construction with ordinary resource injection. This candidate reopens the assumption that arbitrary private operator state must remain shared across resolutions. No implementation has been accepted.

Each construction creates fresh private executor state and resolves children for the current advice context. Mutable fields belong directly to that executor unless its author deliberately uses a supplied resource. Stateless implementations may still reuse immutable objects. Configuration identity remains useful for observation selection without implying runtime state identity.

Ordinary fixed child references and materialized advice chains remain possible; an invocation context dispatcher is not required for this sharing model. The configuration/executor distinction remains, with two variants for supplying resources.

This could remove `GetScopedState`, ancestor state-owner selection and the corresponding implicit reuse contract. The resolver still checks types, detects construction cycles, resolves children and applies advice. Retained or Shared activation policies, cross-operation state reuse, state surviving reconstruction and related tests need explicit migration decisions rather than assumptions that behavior is unchanged.

The cost is that an operator must expose a suitable resource when sharing its state is a supported use case. Dynamic cache invalidation illustrates that a useful shared resource may need to include its related epoch metadata, not only a dictionary of entries.

#### 1.1. Live resources referenced by configurations

A caching evaluator without a supplied store creates a private store for each executor. With a supplied store, each executor retains that same reference. Counting advice and its supplied `CountAccumulator` demonstrate the existing resource-injection pattern.

A supplied resource crosses independent runs if the caller reuses the configuration containing it. A plain per-trial composition function can create a fresh resource and inject it into every participant of that trial. A factory called separately by every executor would not provide this within-trial sharing.

The current configuration rule would need a deliberate revision: an immutable reference can point to a mutable dependency. An immutable record containing such a reference no longer describes a graph of pure settings, and ordinary record copying retains the resource reference. Resource identity, ownership, disposal where applicable, concurrent access, and reuse across runs must be clear.

Formatting and persistence also need a defined boundary. Mutable objects can have serializable data, but a live resource is not automatically a portable construction recipe. Exporting settings, exporting which operators share a resource, and checkpointing the resource's current contents are different capabilities. Omitting the live resource from JSON without preserving its intended sharing would change the reconstructed execution's behavior.

**User experience.** Ordinary users still configure an algorithm and start runs from it. Reusing a configuration means reusing its settings; it does not automatically share its private execution data. In the motivating example, A and B receive the same caching-evaluator configuration containing one supplied store. Every turn constructs fresh A or B executors, fresh terminators and context-specific evaluation wrappers; those wrappers all retain the supplied store. Constructing the store inside a function called once per trial gives each trial its own cache. Constructing it outside that function deliberately shares across trials.

Illustrative composition:

```csharp
var cache = new EvaluationCache();
var evaluation = evaluator.Cached(store: cache);
var cycle = Cycle.Create(
    algorithmA.WithEvaluator(evaluation),
    algorithmB.WithEvaluator(evaluation));
```

These names and overloads are proposed, not existing APIs. A fresh-trial function would construct the store and this configuration graph together.

**Library simplification.** Executors keep counters, adaptive values and private caches in ordinary fields. Operator authors no longer coordinate a separately owned state object with each contextual executor. Configuration and executor types remain, as do child construction, operation checking, advice ordering and observation identity. The generic state store, ancestor ownership lookup and automatic cross-resolution private-state sharing can go. Old stateful authoring bases would need replacement or consolidation; merely keeping all existing state abstractions and changing their storage would not realize the full simplification.

**Feasibility.** This is the most direct migration candidate. `CachingEvaluator` already passes a cache to its executor, and counting advice already passes a `CountAccumulator` to its wrappers. The material behavioral change is independence between separately constructed executors. `RepeatedExperiment` currently repeats the same configuration reference, so supplied resources would also repeat; a fresh-resource-per-trial construction path must be deliberate. `GridExperiment` also uses configuration equality, so live resource identity must not accidentally become the definition of a distinct parameter setting. Resource-bearing records require an explicit equality and experiment-key decision.

#### 1.2. Pure configuration with resources supplied during runtime composition

Keep reusable configurations as pure settings and child definitions. Supply live resources when constructing the execution graph, outside those stored definitions. A per-run composition function can create one store and supply it to all participants of that run. The concrete construction API remains open; this variant does not assume a general resource registry or mandatory state-provider protocol.

The settings remain suitable for configuration export, but resource wiring performed by application code is not automatically captured in that export. Reconstructing the complete setup requires that composition code or an explicit description of the wiring. Standardized export of arbitrary sharing relationships would therefore still require some design.

A factory that merely creates new configurations containing live resource references is variant 1.1 with a controlled lifetime. It is not the pure-configuration separation described by 1.2.

**User experience.** Ordinary runs still use a reusable configuration directly. For shared caching, the user first configures A, B and their caching evaluator without a live store. At run setup, the user creates one store and supplies it to the selected caching-evaluator construction. That supply applies to every occurrence constructed for that run, including deferred Cycle turns. It changes neither the serialized configuration nor other runs. A new trial repeats this small setup step with a new store; omitting it leaves private caches as the default. The user must be able to identify the intended resource input without relying on resolution order or searching for a cache by its service type.

**Library simplification.** Private state follows the same executor-field model as 1.1, and the same automatic state-ownership machinery can disappear. The extra responsibility is connecting an explicit runtime resource to a configured component during construction. There is no automatic creation, ancestor ownership or reset policy for arbitrary private operator state. This connection must work equally for consumer-defined operators. It can happen at construction time, leaving ordinary direct calls and fully materialized wrapper chains during execution.

**Feasibility.** Feasible in principle, but less settled than 1.1: the resource-supply API is a real missing design, not a free consequence of pure configurations. Two independent caches, one cache reused across deferred turns, and a consumer-defined resource are the decisive examples. If those require a general service container, scope hierarchy or resource-lifetime policy language, this candidate has recreated much of the machinery it was meant to remove. Configuration export is cleaner, but a complete reproducible recipe still needs the supply relationships or application setup code. The current backlog already treats experiment persistence as unresolved; pure records alone do not establish a verified JSON round trip.

### 2. Runtime objects as the primary composition model

Make ordinary runtime objects the primary authoring and composition API. One operator or algorithm object holds its parameters, mutable execution state and execution methods, rather than requiring separate configuration and executor types. Fresh independent runs are obtained through construction functions or an explicitly defined operation that creates an unfitted or unstarted copy. Reusing an existing object intentionally reuses its state unless an operation specifies otherwise.

This is conceptually similar to scikit-learn's estimator model: constructor parameters and fitted attributes live on the same estimator, while cloning creates an unfitted estimator with the same parameters. The analogy does not imply adopting every scikit-learn lifecycle or composition convention. See the [estimator guide](https://scikit-learn.org/stable/developers/develop.html) and [clone documentation](https://scikit-learn.org/stable/modules/generated/sklearn.base.clone.html).

This route could remove more construction infrastructure, but requires a broader redesign of experiments, observation weaving and reusable configuration. Parameter/state distinctions and fresh-run semantics still exist even without parallel type families. Exporting a fresh-run recipe remains distinct from persisting a running object's state; neither capability follows automatically from combining the types.

**User experience.** Users construct callable algorithms and operators directly. A runtime mutator contains its initial settings and current adaptive strength; a runtime terminator contains its limit and counter. Its parameters can still be immutable while its execution data changes. Reusing an object intentionally reuses its data. To make Cycle turns independent, users provide construction functions for its inner algorithms rather than already-running objects that Cycle is expected to reset. A convenience API can package that construction, but it must not conceal whether it creates or reuses an object.

For the cache example, a trial-construction function creates one store and a Cycle. The Cycle receives two functions; each invocation creates a fresh A or B graph and its terminator, while passing the trial's existing store to the new evaluation wrapper. Each trial calls the outer construction function again. Experiments vary plain parameter values and invoke the function once per trial. Automatic deep cloning or a universal `Reset` contract is not assumed: either would reintroduce difficult decisions about which children and resources to copy or clear.

**Library simplification.** The mandatory configuration/executor pair and generic scoped-state layer can disappear for ordinary components. An author implements the named execution method on a class with fields and constructor-injected children. Multiple capabilities can be methods or interfaces of that same object. Typed operations, batching, deterministic randomness, cancellation, run lifecycle, trial scheduling and analysis still have responsibilities; they are not removed by merging component types. Optional persisted recipes or reusable parameter records may remain outside the execution core, but recreating a mandatory configuration type for every operator would reduce the benefit.

**Advice and graph construction.** Explicit runtime decorators preserve readable call stacks and child references. `OA(Op)` and `OA(IA(Op))` still use different wrapper objects; any common counter or cache is a separately supplied resource. A shared composite cannot acquire different fixed children for different callers. Retaining automatic pointcut-based weaving requires a construction path that sees every relevant object, including deferred children, and knows the active advice context. The same path must work for consumer-defined algorithms and operators. Direct composition is feasible, but equivalent automatic observation is the major unresolved boundary. A new factory framework with a second object hierarchy could consume the simplification.

**Feasibility.** Technically feasible as a broader product redesign, with the greatest potential reduction in authoring concepts and the widest migration. Purely configured grids, configuration-reference observation handles and arbitrary graph export need new user-facing counterparts. A promising version uses plain parameter data plus explicit construction functions, preserves the run host, and chooses how automatic advice is installed during construction. A claim that every current convenience remains unchanged while all construction machinery disappears would be unsupported.

### Common feasibility checks

- A small custom stateful operator should need ordinary fields, not a framework-owned state protocol. Consumer-defined operations and resources must participate without built-in cases in the resolver or composition layer.
- A Cycle with A and B should preserve its search-state handoff, give each turn fresh stopping data, and optionally share one cache for one trial. Reversing construction order must preserve those relationships.
- A shared outer counting advice and an inner-only advice must produce the intended fixed wrapper chains and one explicitly shared outer accumulator. Observation identity must not silently become state-sharing identity.
- Adaptive controls must reach the object actually invoked. `EvolutionStrategy` already resolves mutation and its strength control together, so moving that strength into the returned executor is plausible. Separately resolving several operation views must no longer be assumed to share private data; a component must return related views together or expose an explicit common resource.
- Retaining an existing object retains its child references and advice too. The current ability to retain arbitrary private state while reconstructing different child/advice graphs is intentionally not preserved automatically by these candidates. Any retained-activation replacement must state this limit.
- Shared cache contents and related validity data, such as dynamic-problem epochs, must belong to a coherent resource. Resource lifetime, concurrency and cleanup remain ordinary resource responsibilities rather than disappearing with the scoped-state layer.
- Fresh execution must remain distinct from pause/resume. Execution time, construction allocations and wrapper overhead require measurement after an accepted prototype; fewer concepts do not prove a speedup.

**Current recommendation.** Use 1.1 as the smallest coherent simplification to compare against the current implementation. Prefer 1.2 only if preserving pure reusable definitions justifies a small, concrete resource-supply API. Explore 2 if simplifying operator authoring itself is the larger goal and changes to experiment construction, persistence and automatic weaving are acceptable. This ranking is an assessment, not implementation acceptance.

### Earlier alternatives retained for comparison

These alternatives remain unaccepted. They retain generic state-sharing machinery and are separate from the explicit-resource and runtime-object directions above.

#### Declaration on the enclosing composition

Place the sharing declaration on the enclosing composition whose execution provides the lifetime. For example, a Cycle configuration could name the caching evaluator whose state survives all its turns. Independent executions of that Cycle would have independent shared state.

Declare the intended owner before any inner activation; state construction can remain lazy. This establishes sharing without requiring parent executor construction to happen before child construction. A new observation context continues to apply its own advice.

Illustrative syntax, not an existing or selected API:

```csharp
var cycle = CycleAlgorithm.Create(first, second) with
{
    SharedOperators = [cachedEvaluator]
};
```

The alternative of marking the operator itself as shared still needs a rule identifying the lifetime owner. Supplying an explicitly shared cache resource is another alternative, but does not by itself provide a general mechanism for arbitrary operator state.

#### Shared operator configuration

The developer proposed a reusable operator wrapper, analogous in configuration shape to `Named`, to avoid repeating sharing mechanics on each meta-algorithm. Its placement and semantics remain under discussion.

Illustrative syntax:

```csharp
var sharedEvaluation = evaluator.Cached().Shared();
var first = algorithmA with { Evaluator = sharedEvaluation };
var second = algorithmB with { Evaluator = sharedEvaluation };
var cycle = CycleAlgorithm.Create(first, second);
```

The wrapper would remain an operator of the wrapped operation, including consumer-defined operations. Its configuration would describe sharing rather than store a live cache or executor. A general resolution capability would select the requested state owner while retaining the caller's advice context; caching the first resolved executor would capture the first caller's children and advice.

One candidate default is sharing per top-level run, keyed by the shared wrapper's reference identity. Reusing that wrapper in several turns would share state; using it in independent runs would not. Separately created wrappers would establish separate sharing identities. These are recommendations, not accepted decisions. If sharing should end at a nested composition instead, the wrapper needs an explicit or otherwise unambiguous lifetime boundary. That boundary need not be specific to Cycle.

Sharing only the selected configuration's state must not implicitly give its children the same long lifetime. This would require changing the current rule that builds children in the selected owner's state. Multiple operation views also need a deliberate way to retain one sharing identity without enumerating built-in operations.

The wrapper expresses opt-in and ownership. It does not settle whether runtime behavior uses contextual executor graphs over separate state or persistent executors with contextual child calls.

## Open decisions

- Whether to retain generic sharing of arbitrary private operator state at all, or adopt fresh executor construction with optional explicit resources.
- If resources are supplied through configuration, whether repeated runs intentionally reuse them, with per-run independence obtained by creating resources during per-trial composition.
- Whether configuration export must reconstruct sharing relationships as well as settings, and whether checkpointing live runtime state is a separate requirement.
- Whether to retain separate configuration and executor types, or adopt runtime objects with explicitly defined construction and fresh-copy semantics.
- Which current sharing-dependent workflows need supported resource APIs, including caches, budgets, adaptive controls and operators exposing several operations.
- Whether the declaration belongs on the enclosing composition, on a shared operator reference, or in another explicit runtime composition mechanism.
- For a shared wrapper, which lifetime it selects and whether sharing identity belongs to that wrapper or to its underlying configuration.
- Whether selecting a composite shares only its own mutable state or also the state of its descendants. The current recommendation is selection of its own state only, with descendants requiring their own opt-in.
- Whether sharing for one cycle, as distinct from the entire Cycle execution, is needed initially and how that boundary is expressed.
- How nested sharing declarations interact and whether an inner composition may override an inherited sharing choice.
- How the existing Retained and Shared activation policies relate to the new defaults.
- Whether executors remain contextual objects around separate scoped state, or become persistent objects receiving contextual child calls. Choosing a sharing lifetime does not settle this representation.

## Representative behavior to evaluate

- Two algorithms use one caching evaluator across several turns: cache data survives while unselected stopping state starts fresh on every turn.
- Reversing resolution order preserves the selected sharing.
- Reusing the same terminator configuration in separate fresh turns does not combine their stopping counters unless that sharing is explicitly requested.
- Another execution starts with independent cache data when using the default private cache or a newly supplied resource. Reusing a supplied store intentionally retains its data across executions under the explicit-resource candidate.
- Advice remains local to each calling context even when the selected operator state is shared.
- A shared composite with an unselected stateful child exposes the chosen descendant-sharing rule without relying on an earlier construction order.
