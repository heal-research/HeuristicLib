# Execution bindings: design investigation

Status: design comparison recorded on 2026-09-27. Candidate 1, the typed factory, is now the selected option-C direction in the [design-work plan](execution-bindings-and-shared-state.md), superseding the old option-A decision. The detailed public API and migration are not approved or implemented. Read alongside the [scope cases](container-and-aspect-framing.md#package-4c-scope-examples-for-discussion). Full authoring sketches have not been compiled; a standalone compiler check verified return-lambda target typing for the typed delegate, including a generic instance. Performance and lifecycle claims require the later proofs.

Follow-up direction: design candidate 1 concretely, particularly preserving ordinary stateless/stateful leaf authoring. Review non-terminal operator and algorithm contracts and justify any child-dependent initialization facility. This comparison remains supporting evidence; the linked design-work plan owns the active sequence and the later migration review.

The [concrete factory design draft](execution-factory-design.md) now supplies the C1 contracts, authoring examples and proposed ownership rules. Its detailed proposals replace this document's exploratory sketches as the next review subject; they remain unimplemented until reviewed and proved.

The later [graph naming decision](execution-factory-design.md#agreed-naming-family) selects `IConfigurationNode`, `IExecutionNode` and role-specific `...Execution` types. This comparison retains the source names and exploratory snippets used during the investigation. Use the agreed names for new design work; naming and factory/resolution behavior will be migrated in separate commits.

## Finding

The strongest candidate is a typed factory that prepares persistent execution data once, then constructs typed bindings for different observation scopes. A second credible approach makes existing execution objects explicitly rebindable. Replaying today's factories with a state store is a smaller-looking change, but needs a substantially stronger factory discipline than it initially suggests.

None can transparently rebind every existing authored object while preserving arbitrary private mutable fields and captured dependencies. Calling the same object with the same arguments follows its same child fields. Constructing another object runs its constructor and field initializers. Copying fields duplicates scalar state, shares references indiscriminately and leaves captured delegates pointing at old objects. Under the exclusions on ambient scope, proxies and rewriting, some author cooperation is necessary for composites.

That cooperation can be hidden completely from ordinary configuration users and from authors using the stateless/stateful leaf bases. Keeping it invisible to arbitrary composite authors is not a guarantee these candidates can make. This is a real tradeoff against the requested ideal, not an implementation detail to postpone.

## Source facts driving the design

- [ResolutionScope](../src/HeuristicLib/Execution/ResolutionScope.cs) currently caches complete decorated instances by configuration reference. A matching decoration stops ancestor lookup. Its `underConstruction` entries support decoration re-entry, not persistent dependency recording.
- [StatefulMutator](../src/HeuristicLib/Operators/Mutators/BaseClasses/StatefulMutator.cs) already separates ordinary state into `TState`, but initializes it on every factory call. [StatelessMutator](../src/HeuristicLib/Operators/Mutators/BaseClasses/StatelessMutator.cs) returns itself and needs no additional raw instance.
- [PredefinedCandidatesCreator](../src/HeuristicLib/Operators/Creators/PredefinedCandidatesCreator.cs) is a concrete stateful composite: its mutable candidate index and its resolved fallback creator currently live together.
- [GeneticAlgorithm](../src/HeuristicLib/Algorithms/Evolutionary/GeneticAlgorithm.cs) constructs `Mutator.AppliedAtRate(MutationRate)` inside its instance factory. Replaying that factory also changes a source-wrapper configuration reference unless its identity is preserved separately.
- [CycleAlgorithm](../src/HeuristicLib/Algorithms/Composition/CycleAlgorithm.cs) retains its creating scope and, in reuse mode, a cache of child execution instances. [PipelineAlgorithm](../src/HeuristicLib/Algorithms/Composition/PipelineAlgorithm.cs) also retains a scope for delayed construction.
- [AlgorithmRun](../src/HeuristicLib/Execution/Runs/AlgorithmRun.cs) retains one live async enumerator across pauses. [IterativeAlgorithm](../src/HeuristicLib/Algorithms/BaseClasses/IterativeAlgorithm.cs) keeps progress, iteration position and dependencies in that enumeration.
- [OperatorDurationBudgetAlgorithm](../src/HeuristicLib/Algorithms/Control/OperatorDurationBudgetAlgorithm.cs) allocates its accumulator during instance creation. [AlgorithmDurationBudgetAlgorithm](../src/HeuristicLib/Algorithms/Control/AlgorithmDurationBudgetAlgorithm.cs) instead keeps its duration inside an invocation's iterator. These require different lifetimes.
- [EvolutionStrategy](../src/HeuristicLib/Algorithms/Evolutionary/EvolutionStrategy.cs) tests its resolved mutator for `IAdaptableMutationStrengthInstance`. The current [observation wrapper](../src/HeuristicLib/Analysis/Tracing/Observations/MutatorObservation.cs) implements only the ordinary mutator interface, already hiding that capability.

## Common ownership model

All three candidates need more than a configuration-to-state dictionary. The following are internal descriptive terms, not proposed public interfaces.

| Responsibility | Proposed owner and lifetime |
| --- | --- |
| Configuration settings | Existing reusable configuration; no mutable run data |
| Logical source identity | A node selected by configuration reference within a logical resolution domain; compatible execution signature checked |
| Persistent mutable data | That logical node; initialization is independent of the number of bindings |
| Derived configuration references | Prepared logical construction data, stable across rebinding |
| Selected child identities | The logical node's dependency records, including selected ancestor nodes |
| Typed child references | One binding, constructed for its observation context |
| Deferred child reuse cache | Logical child identities/domains, not decorated execution objects |
| Advice state | A separately owned advice node associated with a declaration identity and logical source |
| Analyzer accumulator | Caller-owned analyzer, which may aggregate several independent source states |
| Progress, RNG forks, local timers, enumerator | The invocation, retaining the binding with which it started |
| Resource ownership | Existing explicit persistent or invocation owner; binding replacement adds no disposal contract |

The resolver needs two internal relationships: where logical state is selected, and which declarations affect a binding. A scope passed explicitly to construction can carry both internally. This does not require ambient state or a second public scope API, but it is a substantial change to what `ResolutionScope` implements.

### The dependency collision that a simple state store misses

Consider this sequence:

1. Child C resolves M and owns M_child.
2. Parent P later resolves G, creating G_parent with M_parent.
3. C resolves G and shares G_parent's logical execution.

Replaying `C.Resolve(M)` inside G would pick M_child. G would keep its own counter but silently switch the state of its existing dependency.

Recommended continuity rule: preserve G_parent's recorded dependency M_parent and construct a C-observed binding of it. Direct `C.Resolve(M)` still returns M_child. Consequently, the binding cache is keyed by logical node and observation context, not just configuration and scope.

This preserves both child-first ownership and the reused composite's dependency continuity. It also means that two logical instances of the same configuration can be reachable through different dependencies in C. The existing one-cache-entry-per-configuration intuition no longer describes every object reachable from a scope.

Other coherent policies are to reject this collision or explicitly permit dependency substitution. Neither should be smuggled in as an optimization. Substitution weakens the requirement to preserve the composite's existing logical child state. The continuity rule needs acceptance before implementation.

### Construction and cache rules

- Direct resolution first selects or reuses a logical node using reference identity, local ownership and ancestor visibility. Equal configuration values never merge distinct sources.
- Rebinding an existing node follows its recorded logical dependencies. Its explicitly passed construction scope supplies the new observation context while preserving those identities.
- Ordinary repeated resolution returns the cached binding for that node/context. A differing context can build another binding without initializing another node.
- Compatible bindings may be reused when the resolver can establish compatibility, including deferred dependencies. Do not infer compatibility just because a selector rejects the composite itself.
- Stateless leaf execution may still return its configuration. A leaf whose raw execution object needs no new child bindings can retain that object and receive different observation wrappers.
- Generated decoration links are distinguished from source dependencies. Explicit configured wrappers remain source nodes with their own identities.
- Arbitrary dynamically selected dependencies require stable logical slots or a defined dynamic-domain operation. Inferring slots from call order is not robust. Ordinary stable source references cover eager fixed graphs; repeated/deferred local domains need explicit framework support.

## Candidate 1: prepare once, bind through a typed factory

Preparation creates persistent state and stable construction data. It returns a factory which accepts a resolution scope and creates a typed execution binding. Preparation has no scope with which to resolve children. Child wiring belongs to the returned factory.

Illustrative core contract:

```csharp
public delegate TInstance ExecutionFactory<out TInstance>(ResolutionScope scope)
    where TInstance : class, IExecutionInstance;

public interface IExecutionConfiguration<out TInstance> : IExecutionConfiguration
    where TInstance : class, IExecutionInstance
{
    ExecutionFactory<TInstance> CreateExecutionFactory();
}
```

Role configurations still take the run's types as method type arguments. For example, the mutator factory returns `ExecutionFactory<IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem>>`, with the existing search-space/problem constraints. Operation contracts such as `Mutate` do not change. A consumer-defined role uses the same generic resolver mechanism; there is no built-in-role enumeration.

The resolver calls preparation once for the chosen logical node, retains the factory, and invokes it with an explicit construction scope for each necessary binding. A delegate here is an execution-time construction object, not persisted configuration behavior. A named delegate is only an illustrative shape; an equivalent covariant contract is possible.

### Representative authoring change

Consider a consumer-defined `IProbeInstance` with a typed `Probe` operation. Its configuration has a mutator and an evaluator child. In the following abbreviated sketches, `MutatorInstance`, `EvaluatorInstance`, `Candidates`, `Rng`, `Space`, and `Problem` stand for concrete closed repository types; the production role's generic arity does not disappear.

Current shape:

```csharp
public IProbeInstance CreateExecutionInstance(ResolutionScope scope)
{
    var typed = scope.For<RealVector, Space, Problem>();
    return new Instance(typed.Resolve(Mutator), typed.Resolve(Evaluator));
}

private sealed class Instance(MutatorInstance mutator, EvaluatorInstance evaluator) : IProbeInstance
{
    private int calls;
    public int Calls => calls;

    public IReadOnlyList<ObjectiveVector> Probe(Candidates input, Rng random, Space space, Problem problem)
    {
        calls++;
        var candidates = mutator.Mutate(input, random, space, problem);
        return evaluator.Evaluate(candidates, random, space, problem);
    }
}
```

Proposed shape:

```csharp
public ExecutionFactory<IProbeInstance> CreateExecutionFactory()
{
    var state = new State();
    return scope =>
    {
        var typed = scope.For<RealVector, Space, Problem>();
        return new Instance(state, typed.Resolve(Mutator), typed.Resolve(Evaluator));
    };
}

private sealed class State { public int Calls; }

private sealed class Instance(State state, MutatorInstance mutator, EvaluatorInstance evaluator) : IProbeInstance
{
    public int Calls => state.Calls;

    public IReadOnlyList<ObjectiveVector> Probe(Candidates input, Rng random, Space space, Problem problem)
    {
        state.Calls++;
        var candidates = mutator.Mutate(input, random, space, problem);
        return evaluator.Evaluate(candidates, random, space, problem);
    }
}
```

For this composite: one additional private data type, no additional role generic parameters, one factory replacing the existing factory, one binding lambda, one constructor parameter, and still exactly two child-resolution expressions. There is no extra operation delegate in this variant. `IProbeInstance` remains an ordinary consumer-defined nominal role. It needs its own typed observation adapter if observed, just as any other role does.

For ordinary `StatefulMutator<..., TState>` subclasses: zero author changes to `CreateInitialState` or `Mutate`. The base prepares state once and supplies the binding factory internally. A stateless base can return a factory that yields `this`; the implementation should avoid introducing unnecessary allocations for that case.

One-time work, such as creating GeneticAlgorithm's rate wrapper or a budget accumulator, belongs in preparation. A budget's internal child-scope declaration must also have stable identity and shared accumulated state across its bindings. Re-running the original public declaration callback and treating its result as a new registration is insufficient; logical child scopes and their binding views need an explicit implementation.

### Initialization that needs a resolved child

The simple scope-free preparation form cannot directly initialize persistent state from a resolved child's capability, value or resource. Today's explicit constructor can do that. Silently restricting all authors to child-independent initialization would weaken the extension model.

A possible framework-managed form separates three construction steps: bind the typed dependencies, initialize the persistent owner once using the first dependencies, then construct the execution binding. Subsequent bindings resolve the same logical dependencies under their new observation context and reuse the initialized owner. A conceptual helper could return the same typed factory:

```csharp
return ExecutionFactory.Create(
    dependencies: scope => ResolveChildren(scope),
    initialize: children => new State(ReadInitialSetting(children.Mutator)),
    bind: (state, children) => new Instance(state, children.Mutator, children.Evaluator));
```

`ResolveChildren` returns an explicitly typed pair, such as a named tuple of mutator and evaluator instances. This is construction-only dependency grouping, not type-keyed auto-wiring or generic operation invocation. The helper, rather than each operator author, would own the one-time initialization/fault state machine. Stable derived configurations are still created outside these callbacks once per logical preparation. Child wiring still appears once.

This form adds a typed dependency tuple/carrier and three construction callbacks instead of the simple binder lambda. It needs a proof that failed first dependency construction, failed owner initialization and failed later binding have distinct correct outcomes. Initialization may read the first child bindings but must not retain them as scope-independent operational dependencies. That restriction remains an ownership convention. Candidate 2 can instead preserve the result of the original constructor without this additional factory shape.

This variant makes candidate 1 plausible for child-dependent initialization; it is not a demonstrated general solution. The proof must include it before claiming that candidate 1 replaces all explicit authoring paths. Whether one factory shape can express both the simple and advanced cases without an unnecessarily large API remains open.

Concrete hypothetical use case: an adaptation controller snapshots a shared mutator's current mutation strength when the controller is first created, then limits subsequent changes relative to that baseline. A configured starting strength of 0.10 may already have adapted to 0.04 in the shared execution instance; 0.04 is the requested baseline. Rebinding the controller after the mutator reaches 0.02 must retain 0.04. This is a consumer-extension example, not a current controller implementation. GaussianMutator's mutable strength and EvolutionStrategy's runtime adaptation are existing mechanisms, but the inspected constructors did not establish a current need for the proposed three-callback helper.

Creating an algorithm's initial candidate/population through resolved children is a different case: that work belongs to the invocation and already occurs during execution. Likewise, data obtainable from child configurations does not justify requiring resolved children during persistent initialization. Do not add the general helper solely on the strength of hypothetical extension cases; first decide whether the use case is wanted and whether initialization must happen at binding time rather than first execution.

### Algorithm authoring follow-up

Algorithm configurations would return typed factories, while resolution would still return fully constructed `IAlgorithmInstance` objects with the existing operation methods. HillClimber's ordinary instance methods can remain unchanged: its fields hold children/settings, while its public progress and iteration position live in the active iterator. No empty shared state class is needed. Interceptor resolution must happen inside each binding; the iterative authoring base can continue centralizing that resolution, but its protected construction hook also needs to return a factory rather than capture an already-resolved interceptor during preparation.

Pipeline's factory can likewise produce a new instance capturing the supplied construction scope. Cycle's reuse mode needs more work because its current dictionary stores decorated child instances. One possible resolver-owned authoring shape is a retained child-scope operation keyed by algorithm configuration reference: reset mode uses `scope.CreateChildScope()`, reuse mode uses a provisional `scope.GetOrCreateChildScope(algorithm)`, then both resolve through that child scope normally. The retained logical child scope belongs to the Cycle logical node, while each returned view carries the current binding's observation context. This is an unimplemented API sketch requiring ownership tests, not ordinary scope-global memoization. It would allow removing the instance dictionary without exposing logical-node handles to Cycle authors.

### Persistent behavior owner instead of a data holder

The shared object need not be passive data. The same factory can create a persistent `Core` with ordinary mutable fields and a role-specific method:

```csharp
sealed class Core
{
    private int calls;
    public Result Probe(Input input, MutatorInstance mutator, EvaluatorInstance evaluator) { /* typed work */ }
}

sealed class Instance(Core core, MutatorInstance mutator, EvaluatorInstance evaluator) : IProbeInstance
{
    public Result Probe(Input input) => core.Probe(input, mutator, evaluator);
}
```

This adds a forwarding method and two explicit child arguments to the core method, but permits ordinary mutable fields without pretending they can be copied. It is an authoring form of the same ownership model, not an independent resolver architecture. A core must not retain the passed scope-specific children across invocations. A closure-based form can avoid a named state type but needs typed operation adapters and additional captured delegates; it is less attractive as the default without evidence.

Strengths: explicit one-time initialization; stable derived configurations; typed custom roles; simple leaf authoring unchanged; no resolution during ordinary calls; one canonical construction contract can replace the existing one.

Weaknesses: factory contract migration across roles and algorithms; some cooperation from composite authors; persistent dependency/domain machinery is still needed; child-dependent initialization needs the additional construction form above; constructors invoked by the binder must be repeatable; capabilities remain a separate issue. Neither C# typing nor this contract prevents a binder from acquiring resources or a state object from capturing an old child.

## Candidate 2: explicitly rebind an existing execution object

Keep the current creation contract, but allow a raw execution object to create another typed binding of itself:

```csharp
public interface IRebindable<out TInstance> where TInstance : class, IExecutionInstance
{
    TInstance Rebind(ResolutionScope scope);
}
```

The resolver retains the raw object separately from decorated results. A composite implements `Rebind` by constructing another instance with the same state/core and newly bound children. The resolver provides the same explicit dependency-preserving construction scope as candidate 1.

For the two-child example, the additional method has this shape:

```csharp
public IProbeInstance Rebind(ResolutionScope scope)
{
    var typed = scope.For<RealVector, Space, Problem>();
    return new Instance(state, typed.Resolve(mutatorSource), typed.Resolve(evaluatorSource));
}
```

The instance must retain stable source configuration references or a binding recipe. Its original factory and `Rebind` must share construction code to avoid duplicating the two child-resolution expressions. Moving that common code into a recipe gradually converges on candidate 1.

An ordinary mutable leaf can remain the original raw object; only its observation wrappers change. A composite with mutable fields still needs shared data or a persistent behavior owner. `MemberwiseClone` does not remove this requirement.

For the example: one rebinding interface implementation and method, a shared state/core object if not already present, and retained source references or a recipe. Naively it adds a second copy of the child wiring. Factoring the wiring avoids duplication but adds another construction helper. No per-operation resolver is necessary.

Strengths: less immediate change to configuration factories; natural opt-in migration; preserves existing raw leaf objects and their private fields; custom roles can implement the generic contract.

Weaknesses: two construction paths; more contracts exposed on execution objects; raw versus decorated identity matters; state-preservation mistakes remain author responsibilities; source configurations/recipes must be retained. Opaque objects cannot safely be assumed to be leaves merely because initial construction observed no children: they may resolve children later.

As an optional capability, this cannot fulfill the full requirement for every custom composite. It needs either a trustworthy leaf/completeness declaration or rejection when required observation cannot be proved. As a mandatory contract for every composite it is a credible full-model candidate, but loses much of the incremental-adoption advantage. The state/domain/capability issues below still apply.

## Candidate 3: replay factories with resolver-owned state

Keep `CreateExecutionInstance(scope)` and add a state acquisition operation, conceptually:

```csharp
var state = scope.GetState(this, static () => new State());
return new Instance(state, typed.Resolve(Mutator), typed.Resolve(Evaluator));
```

The state key is the chosen logical source node, not just the state type or requesting scope. The same recorded dependency identities are necessary. Stateful bases could hide the change; a custom composite adds one state type and one acquisition call while keeping its factory signature.

The problem is everything outside `GetState`: constructors, generated configuration wrappers, resource acquisition, budget accumulators and declarations all run again. The actual GeneticAlgorithm rate-wrapper construction is a counterexample to treating this as pure child rewiring. Authors would need to distinguish one-time preparation from repeatable construction throughout the factory. Making a prepared construction object contain those values solves it, but approaches candidate 1 through a less visible contract.

Strengths: smallest factory-signature change; straightforward for disciplined simple factories; no per-operation lookup.

Weaknesses: strongest reliance on conventions, hidden construction replay, awkward separation of mutable data from stable generated configurations, and poor default safety for arbitrary authored factories. It is plausible for a bounded subset, not the recommended general design.

## Lifecycle and observations common to viable candidates

### Deferred construction

For an already-created logical Cycle or Pipeline, preserve its original logical resolution domain when selecting future child state. Apply the new binding's observer context to the resulting child binding. Selecting deferred state from the requesting child scope instead would change the original creating-scope semantics and could expose child-local state through a shared parent composite.

Cycle reuse mode retains one logical child domain/node per algorithm reference. Existing child nodes remain pinned; previously unresolved children use the original logical domain on first use. Each view can cache a different decorated binding of that same child. Reset mode creates a new logical child domain per visit, still allowing eligible ancestor reuse. Pipeline creates the corresponding stage domains for each invocation. RNG fork positions remain unchanged.

These domain handles are execution infrastructure owned by compositions. They do not require ordinary leaf authors to learn about a state store. Storing an old decorated child in the persistent cache would defeat the entire design.

### Paused execution

Resolve produces a binding, not an enumerator. A live enumerator keeps its original binding, nested enumerators, progress, RNG and local resources. Resuming the existing run resumes that same enumerator. Creating another binding neither restarts it nor migrates it.

A future invocation may use the new binding. Moving an already-paused invocation to a new observer topology would require a separate explicit continuation/step protocol; ordinary captured async iterators do not provide that through state sharing. The recommendation is to preserve active bindings, consistent with the plan's requirement for defined resume behavior. Shared persistent state adds no concurrency guarantee.

### Advice, declaration identity and ordering

Rebuild the applicable chain from the undecorated binding. Do not put B around the completed A-wrapped parent object: that can change ordering and duration isolation. For same-origin parent A and child B, the existing depth order gives child `A_child(B(M_childBinding))` alongside parent `A_parent(M_parentBinding)`, with shared M state and shared intended A state. Each successful child call reports to B and A once.

Inherited declarations retain identity and provenance. Generated wrapper references are not advice identities. An explicit configured wrapper remains a source node. Ordinary observations can bind a new thin wrapper to the same callback/accumulator. Stateful advice needs the same one-time preparation versus repeatable binding discipline as source execution.

Advice state has its own scope ownership: reuse already-existing applicable ancestor advice state, and keep child-created advice state local. Do not store every child-only advice object on the shared ancestor source or retain all short-lived source nodes in a root registration cache. Analyzer accumulators remain explicitly shared caller objects independently of this selection.

Intrinsic child-scope declarations, such as budget instrumentation, need stable logical identity but an explicitly defined position in each new binding's scope hierarchy. A plausible rule reuses their logical declaration while placing its view beneath the requesting binding, preserving inherited declaration provenance and relative internal nesting. This must be checked against nested budgets and child-local declarations before claiming that the current depth sort alone specifies all cross-binding order. Clock-before-trace ordering and observer exclusion from duration budgets are required outcomes.

### Capabilities need a separate decision

A handwritten wrapper implementing `IMutatorInstance` cannot automatically also implement every unknown interface implemented by its child. State storage does not solve nominal interface visibility.

Two viable directions are:

- Separate typed control capabilities, such as current mutation strength, from the executable role. During construction, resolve the decorated operation endpoint and its control handle; operations continue through the decorated endpoint. This requires an API change and preserves no automatic `is IAdaptive...` behavior.
- Supply a typed capability-preserving facade for a richer role. It forwards operations through the decorated endpoint and control members to the correct persistent owner. A consumer-defined richer role supplies its own adapter; the resolver does not register a built-in list.

A raw unwrapping API is insufficient: the current adaptable-strength interface also inherits the executable mutator role, so calls through that raw object could bypass observers. Blanket arbitrary-interface transparency remains infeasible under the stated exclusions. The proof must include a consumer-defined richer capability before calling this requirement satisfied.

### Failures, enforcement and resources

Reserve logical ownership before preparation. If preparation fails, retain a faulted node if initialization must never run twice implicitly. If preparation succeeds but binding/advice fails, retain the initialized state and its ownership; do not recreate it on a later resolve. Failed first binding must not publish a partially constructed shared composite whose missing dependencies later come from an unrelated requesting scope. Failures of an immutable binding context can remain cached; deliberately retrying requires a defined policy.

This is at-most-once initialization per logical node, not rollback or transactional exactly-once external side effects. Successful child nodes created before an outer failure also need a documented retention policy. None of these candidates can infer how to undo arbitrary user construction effects.

Compiler checks can enforce typed roles, factory return types and explicit dependency arguments. Roslyn guardrails may flag mutable fields in declared binding classes or state capturing execution objects. Transitive ownership, delegate capture, external resource lifetime and binding repeatability still need conventions and tests. There is no proposal to ban all mutable execution fields: persistent cores can have them; binding-local scratch may also be mutable if losing it cannot change logical execution or break concurrency.

Binding disposal must not dispose shared state. Existing explicit resource/iterator ownership remains responsible. Scopes do not acquire a universal disposal contract as part of this work.

## Acceptance and cost assessment

| Case | Necessary condition; current confidence |
| --- | --- |
| Siblings without ancestor state | Separate logical domains; design-level pass |
| Three outer and two inner calls to M | One M node, two chains, shared A accumulator; design-level pass |
| Reused G holding M | Reconstruct G's typed child calls using its recorded M identity; design-level pass for cooperating factories |
| Child first, parent later | Pin existing nodes and accept the dependency-collision policy; decision required |
| Deferred Cycle/Pipeline | Separate logical domains from observation contexts and replace decorated-instance caches; needs lifecycle proof |
| Stateful wrapper and consumer-defined role | Generic construction machinery plus handwritten typed role operations; authoring sketch provided, not compiled; child-dependent initialization needs the extended factory proof |
| Paused enumeration | Preserve original live binding; no live transfer promised |
| Failed preparation/binding/advice | Explicit fault/publication policy; needs focused proof |
| No matching observation/repeated resolution | No callbacks or duplicate initialization; binding cache, conservative compatible reuse |
| Rich custom capability | Typed controls or preserving facade required; not solved by the core alone |
| Cheap calls and short cycles | No measured result yet |

Expected cost categories, not measured results: logical-node/dependency records; one prepared factory or persistent core per logical node; a binding per necessary observation context; wrapper instances per applied chain; and separate deferred-domain/binding caches. Data-holder operation code performs ordinary typed child calls with an additional state reference. Persistent-core and callback variants can add calls/delegates; inlining and actual cost are unproven.

Measurements should use a fixed pre-change implementation with its behavior and limitations recorded. The revised option-A behavior was not fully implemented and is no longer a prerequisite; do not build it solely as a benchmark baseline. Compare semantically equivalent cases directly and report new rebinding capability cases separately. Measure cold and repeated resolution, allocations, retained memory after many scopes, unobserved and observed cheap operators, stateful two-child composites, and both Cycle modes with short operations. Scope caches must not accidentally retain every historical child binding through a long-lived ancestor.

## Selected direction and review boundary

Candidate 1 is selected: typed one-time preparation plus scope-specific binding, with ordinary stateful leaf authoring hidden behind bases and shared data or a persistent core for advanced composites. C1 in the design-work plan now completes its concrete contracts before C2 proves them. Candidates 2 and 3 remain comparison evidence, not additional public construction paths. Child-dependent initialization needs an explicit scope decision; do not add a general helper merely to cover an unaccepted hypothetical extension.

Before a proof, agree the authoring concession, dependency-collision continuity rule, original-binding pause behavior and capability direction. Selecting candidate 1 does not prove it meets every requirement; the concrete design and C2/C3/C4 evidence remain necessary. If a requirement cannot be met, revisit that conflict explicitly rather than silently returning to option A. No design can promise complete transparency to arbitrary existing authors, arbitrary interface forwarding and active iterator migration under all the present exclusions.

Adoption would explicitly revise developer guidelines sections 4.1, 4.3, 4.4, 4.7, 4.12, 4.14 and 4.17, the operator authoring guide, and the glossary's execution-state/instance/scope/decoration descriptions. The canonical documents remain unchanged until a design is accepted. No new public names, mandatory state interfaces, factory replacements or blanket field restrictions are adopted by this investigation.
