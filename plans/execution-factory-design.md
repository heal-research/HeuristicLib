# Typed execution factories: concrete design

Status: C1 design draft, 2026-09-27. The selected direction is option C from [container and aspect framing](container-and-aspect-framing.md#case-5-a-shared-composite-retains-its-dependency-bindings), implemented through candidate 1 from the [comparison](execution-bindings-design-investigation.md). The user has selected that direction and the graph vocabulary recorded below. The remaining contract details and semantic choices are proposals for review, not implemented behavior. The [work plan](execution-bindings-and-shared-state.md) owns proof, migration and validation packages, including separate naming and behavior commits.

This document specifies a design rather than another alternatives survey. Code blocks show proposed authoring contracts; they have not been compiled against a migrated library. The earlier standalone lambda check establishes target typing only. C2 must compile the complete examples and exercise their behavior.

## Agreed naming family

Decision, 2026-09-27: use graph vocabulary for the common contracts and execution vocabulary for role-specific runtime types. The user selected `IConfigurationNode` and `IExecutionNode`; ordinary users continue to compose `IMutator`, `IOperator`, concrete mutators and algorithms. They do not need a general graph API. Implement the rename with the resolution/factory rework, in a separate behavior-preserving commit before the behavior change.

| Current name or concept | Agreed name | Migration boundary |
| --- | --- | --- |
| `IExecutionConfiguration`, including its generic form | `IConfigurationNode`, including its generic form | Naming commit; preserve existing members and semantics. |
| `IExecutionInstance` | `IExecutionNode` | Naming commit. |
| `IOperatorInstance`, `IMutatorInstance<...>`, `IAlgorithmInstance<...>` and other role execution contracts | `IOperatorExecution`, `IMutatorExecution<...>`, `IAlgorithmExecution<...>` and corresponding `...Execution` contracts | Naming commit; keep generic arities and constraints. |
| `MutatorInstance<...>`, `WrappingMutatorInstance<...>`, `IterativeAlgorithmInstance<...>` and corresponding runtime bases/types | `MutatorExecution<...>`, `WrappingMutatorExecution<...>`, `IterativeAlgorithmExecution<...>` and corresponding `...Execution` types | Naming commit; also migrate matching extension classes, filenames and authoring examples. |
| Runtime type parameters and nested implementation names | `TExecution` and `Execution` in the authoring examples | Naming commit where they describe these runtime objects; do not rename unrelated uses of `Instance`, including singleton properties. |
| `IOperator`, `IMutator<...>`, `IAlgorithm<...>` and concrete configuration types | Retain their domain names without `Node` | No naming change. |
| Prepared typed factory | `ExecutionFactory<TExecution>` | Introduced by the factory/resolution commit. |
| `CreateExecutionInstance(...)` | `CreateExecutionFactory()` with the reviewed run type parameters | Factory/resolution commit, because this changes what is returned and when state is initialized. |
| Resolution and declaration APIs | Retain `ResolutionScope`, typed scope views, `ResolutionScopeBuilder`, `Resolve`, `ResolveOptional`, `TryResolve`, `CreateChildScope` and `Decorate` | Names retained; behavioral/API extensions remain subject to their design review. |
| Source selection and setup contributions | Retain `NodeSelector<TConfiguration>`, `IExecutionModule` and `Install` | Names retained. |
| Compatibility and run lifecycle | Retain `ExecutionSignature`, `Fits`, `AlgorithmRun`, `ExperimentRun`, `Attach`, `AttachPerTrial` and `GetAttached` | Names and responsibilities retained. |

In prose, a configuration node belongs to the configuration graph; an execution node is the resolved callable object in the execution graph. Use role-specific forms such as *mutator execution* and *algorithm execution*. An execution node is not one invocation, an entire run or the persistent resolver record. Its identity need not identify the persistent state it accesses. Keep *execution state* distinct from public *search state*.

`Node` appears on the common contracts, not every derived role: use `IMutatorExecution`, not `IMutatorExecutionNode`. No shared `INode`, public child-enumeration contract or universal `Execute` method follows from this naming. Configuration-node identity remains configuration-object reference identity, not one identity per caller path. Node selectors select source configurations; advice applies at operations on their execution nodes.

Binding remains a descriptive word for constructing scope-specific dependencies and observations; it is not the public type family. `IExecutionBinding`, bare `IExecution`, bare `IConfiguration` and definition-based names are not selected. This does not change state-sharing policy or approve the remaining proposed control, child-scope or protected factory-hook APIs.

The examples below use the agreed target names. Links and explicit descriptions of existing source retain current filenames/API names until the implementation migration. The [naming commit boundary](execution-bindings-and-shared-state.md#agreed-commit-boundaries) keeps the rename separate from the return-type and resolution changes.

## 1. What changes for each audience

| Audience | Proposed change |
| --- | --- |
| Configuring and running algorithms | None: compose configurations, attach modules and resolve/start normally. No state-store, binding or factory management. |
| Stateless and ordinary stateful leaf authors | Existing operation overrides and `CreateInitialState()` remain. Framework bases implement preparation and binding. |
| Explicit leaf authors | Return a factory; construct the reusable raw leaf once outside its lambda when it has no scope-dependent children. |
| Composite authors | Prepare persistent fields and derived configurations once; return a lambda that resolves typed children and creates the execution node. Operations keep their existing role signatures. |
| Algorithm authors | Same split as composites. Iterative bases continue resolving interceptors. Search progress remains in the invocation. |
| Meta-algorithm and budget authors | Use explicit fresh or retained child scopes; never retain decorated child instances in shared data. |
| Resolver and instrumentation authors | Own logical identity, dependency selection, binding caches, generated-decoration identities and failure publication. |

An execution node is the concrete object on which `Mutate`, `Evaluate` or `RunStreamingAsync` is called. It may be a scope-specific binding over persistent execution data. The descriptive terms *execution record*, *logical domain*, *binding context* and *construction frame* below name implementation responsibilities, not new public objects ordinary authors must construct. The execution record is the persistent owner; it must not also be called an execution node. Its eventual internal CLR name remains an implementation choice.

## 2. Core and role contracts

Use one public factory method, `CreateExecutionFactory`, replacing `CreateExecutionInstance`. Preparation is synchronous, scope-free and performed once for the selected execution record. Its result is a typed delegate invoked during resolution, never on each operator call.

```csharp
public delegate TExecution ExecutionFactory<out TExecution>(ResolutionScope scope)
    where TExecution : class, IExecutionNode;

public interface IConfigurationNode<out TExecution> : IConfigurationNode
    where TExecution : class, IExecutionNode
{
    ExecutionFactory<TExecution> CreateExecutionFactory();
}

// Members on ResolutionScope; these replace the old creation overloads.
public TExecution Resolve<TConfiguration, TExecution>(
    TConfiguration configuration,
    Func<TConfiguration, ExecutionFactory<TExecution>> prepare)
    where TConfiguration : class, IConfigurationNode
    where TExecution : class, IExecutionNode;

public TExecution Resolve<TExecution>(IConfigurationNode<TExecution> configuration)
    where TExecution : class, IExecutionNode;
```

The marker `IConfigurationNode`, `Fits`, typed scope views, `ResolveOptional` and `TryResolve` keep their responsibilities. A consumer-defined role can use the generic overload without library registration. Do not introduce a built-in role switch. Add the reference-type constraint to the nongeneric-run configuration contract consistently with the existing resolver; audit direct implementers during migration.

For a mutator, the run types still arrive as method type parameters:

```csharp
public interface IMutator<TCandidate> : IOperator
{
    ExecutionFactory<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>>
        CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

// Body of the existing role-specific Resolve extension:
scope.Resolve(mutator, static target => target.CreateExecutionFactory<TSearchSpace, TProblem>());
```

`typed.Resolve(mutator)` and every operation signature stay the same. Algorithms make the equivalent change to both `IAlgorithm<TCandidate>` and `IAlgorithm<TCandidate, TSearchState>`. Bound authoring bases validate their run types before preparing or binding children, then adapt the delegate's result with the same checked variance bridge used today. The core records the prepared instance contract and checks compatible reuse; a request for an incompatible closed role must fail, not create a second state under the same selected source. The proof must cover contravariant operator interfaces and invariant algorithm search states rather than assuming all generic delegates can be cast identically.

Calling `CreateExecutionFactory()` directly creates another preparation; invoking its delegate directly bypasses resolution. These are authoring hooks, not the user workflow. No compatibility `CreateExecutionInstance` method remains after migration. The delegate is runtime construction data, not a persisted configuration strategy, so it does not replace the value strategies required by guideline § 4.8.

Configuration-only validation moves to preparation. Validation that genuinely depends on resolved children happens while binding. Neither phase may execute an operator, draw random values or advance an algorithm. Binding constructors only wire dependencies and binding-local data; they do not reset persistent state.

## 3. Identity, ownership and resolution

### Internal records

| Record | Contents and owner |
| --- | --- |
| Logical domain | Parent logical domain, direct source selections by configuration reference, local declarations. Root/explicit child scopes establish domains. |
| Execution record | Original source reference, owning domain, prepared typed factory or preparation fault, pinned dependency selections, retained child-domain slots. State is retained by its factory, not an untyped public property bag. |
| Binding context | Immutable ordered declaration layers affecting this path, including contextual placement of retained internal child scopes. No mutable ambient current scope. |
| Construction frame | Execution record, current binding context and typed resolution adapter; a generated decoration additionally has one predecessor override. Represented to authors by `ResolutionScope`. |
| Binding cache entry | Raw and completed typed bindings, or a binding fault, for an execution record within one binding view/context. The view owns this cache. |
| Generated decoration occurrence | One source execution record plus one declaration identity; prepared wrapper, persistent advice data and a private domain for additional children. Its predecessor is supplied per binding. |

The resolver may erase types inside heterogeneous caches as it does today, but checks them when recovering entries. Role operations remain statically typed. These records do not constitute a generic invocation or service-injection API.

### Direct resolution versus dependency resolution

1. Direct `scope.Resolve(M)` consults that logical domain's pinned selection, then existing ancestor selections. Decorations do not stop state lookup. On a miss, create the execution record locally; never populate an ancestor merely to share it with siblings.
2. Remember a direct selection, including a reused ancestor selection, so subsequent resolutions do not switch identity when another cache is populated later. Distinct configuration references never merge because their records compare equal.
3. The factory for G receives a construction frame. Its `scope.Resolve(M)` first consults G's recorded dependency for that source reference. If selected already, bind that exact execution record for the frame's context; do not rerun C's direct selection.
4. A new dependency is selected through the frame's logical domain, using the same local/ancestor rules, then pinned to G. Fixed dependencies resolve on first binding. Deferred dependencies retain this frame explicitly; dynamically repeated independent executions use child domains as described below.
5. Build/cache the typed raw binding, apply its declaration chain, then return the completed binding. Different observer contexts can require different bindings even if no declaration matches G itself: its descendants may match.

This yields the following collision policy:

| Sequence | Required identity |
| --- | --- |
| C resolves M before P does | C owns M_child. P later owns M_parent. |
| P builds G using M_parent | G permanently selects M_parent for that dependency. |
| C resolves the ancestor G | G's C binding calls M_parent through C's observation context. |
| C directly resolves M again | Still M_child. Rebinding G does not overwrite C's direct selection. |

Two M states are consequently reachable from C through different logical graphs. This is deliberate dependency continuity, not two states in one direct cache slot. The alternative, substituting M_child into G, would preserve only G's own fields and silently change its existing execution graph.

The logical domain of an already-shared G remains its original one; C supplies observation context rather than adopting G's dependency ownership. A previously unselected dependency resolved directly through G's retained frame belongs to that original domain, even if first triggered by a C binding. This explicitly extends the original execution graph; it is not a direct `C.Resolve(M)` request. New per-invocation children must use a fresh child domain and stay there. Include this distinction in the late-resolution tests and review: otherwise the phrase "no hoisting" hides an unresolved policy for delayed dependencies.

Repeated resolution of the same source in one frame means the same logical dependency. Two independent children using the same configuration reference require distinct child domains; call order is not a dependency identifier. Recreating a derived configuration inside every binder would create another reference: authors must prepare it once. Existing guidance allowing explicit children on execution instances becomes a rule about binding-local children, not shared persistent data.

### Cache and lifetime rules

- Cache by execution record and full binding context, not configuration plus its own matching declarations. A child-only match can change a parent's binding requirements.
- An empty child scope with an identical full context may reuse the ancestor binding. A changed context conservatively rebinds; do not add dependency-completeness optimizations before measurement.
- Stateful leaf bases can reuse their one raw leaf instance while decoration chains differ. Stateless bases can keep returning `this`. Composite raw bindings are view-local.
- A long-lived execution record must not own a strong dictionary of every short-lived observation context and binding. Views own binding caches; records own persistent state/dependency selections and explicitly retained logical child domains. Fresh Cycle/Pipeline child views must be collectible when their invocation releases them.
- Generated occurrences need both source and declaration lifetimes. Use declaration-owned weak-key storage, such as `ConditionalWeakTable<ExecutionRecord, DecorationOccurrence>`: a surviving declaration must not retain every source it once observed, and a surviving source must not retain expired child declarations. An occurrence may reference its key record; the storage must support that relationship without turning it into a strong-key cache. Views/active bindings retain the occurrences they use. Include both lifetime directions in collection tests.
- Stable declaration identities, not captured callback equality, select advice occurrences. Installing the same module twice in one builder retains current idempotence; two separate declarations remain two occurrences.
- Sharing data does not make calls concurrent-safe. Preserve existing serialization requirements. Do not add locks, asynchronous construction or parallel resolution as part of this change.

## 4. Authoring examples

The changes below keep the real run type parameters visible. Imports and unchanged containing-type members are omitted where the source link provides them; no fictitious reduced role signatures stand in for production signatures.

### Stateless and stateful leaves

`NoChangeMutator<TCandidate>` continues to override `MutateCandidate`; ordinary `StatefulMutator<..., TState>` subclasses continue to override `CreateInitialState` and `Mutate`. Only base implementation changes:

```csharp
// In StatelessMutator<TCandidate, TSearchSpace, TProblem>:
public sealed override ExecutionFactory<IMutatorExecution<TCandidate, TSearchSpace, TProblem>>
    CreateExecutionFactory() => _ => this;

// In StatefulMutator<TCandidate, TSearchSpace, TProblem, TState>:
public sealed override ExecutionFactory<IMutatorExecution<TCandidate, TSearchSpace, TProblem>>
    CreateExecutionFactory()
{
    var execution = new Execution(this, CreateInitialState());
    return _ => execution;
}
```

`CreateInitialState` therefore runs once per execution record, not once per observer context. Framework-managed leaf state still must not retain configurations, scopes, child execution instances or child-bound delegates. An explicit stateful leaf such as GaussianMutator can also create its raw instance in preparation, so its current strength survives every decorated binding without a separate empty state wrapper.

### A stateful non-terminal: predefined candidates

[PredefinedCandidatesCreator](../src/HeuristicLib/Operators/Creators/PredefinedCandidatesCreator.cs) currently combines the fallback binding and `currentCandidateIndex` in one object. The proposed method is:

```csharp
public ExecutionFactory<ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem>>
    CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
    where TRunSearchSpace : class, ISearchSpace<TCandidate>
    where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
{
    var state = new State();
    return scope => new Execution<TRunSearchSpace, TRunProblem>(
        state, scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(CreatorForRemainingCandidates), PredefinedCandidates);
}

private sealed class State
{
    public int CurrentCandidateIndex;
}
```

The existing instance constructor takes `State state` in addition to the existing fallback and candidate values. Its `Create` body replaces `currentCandidateIndex` with `state.CurrentCandidateIndex`; its call to `creatorForRemainingCandidates.Create(...)` is unchanged. Consuming seeds through a parent and then a child binding advances one cursor and observes only the fallback calls that actually occur.

Cost to this author: one private state class, one constructor parameter and one lambda. No public state type, execution-record handle, new generic dimension or changed operation signature.

### A consumer-defined role with two different children

This complete role/configuration sketch uses ordinary repository role interfaces. It does not need a new overload in the library's role-resolution extensions:

```csharp
public interface IProbeExecution<TCandidate, TSearchSpace, TProblem> : IExecutionNode
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    int Calls { get; }
    IReadOnlyList<ObjectiveVector> Probe(IReadOnlyList<TCandidate> input,
        IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public sealed record Probe<TCandidate, TSearchSpace, TProblem>
    : IConfigurationNode<IProbeExecution<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required IMutator<TCandidate> Mutator { get; init; }
    public required IEvaluator<TCandidate> Evaluator { get; init; }

    public ExecutionFactory<IProbeExecution<TCandidate, TSearchSpace, TProblem>> CreateExecutionFactory()
    {
        var state = new State();
        return scope =>
        {
            var typed = scope.For<TCandidate, TSearchSpace, TProblem>();
            return new Execution(state, typed.Resolve(Mutator), typed.Resolve(Evaluator));
        };
    }

    private sealed class State { public int Calls; }

    private sealed class Execution(State state,
        IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator,
        IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator)
        : IProbeExecution<TCandidate, TSearchSpace, TProblem>
    {
        public int Calls => state.Calls;

        public IReadOnlyList<ObjectiveVector> Probe(IReadOnlyList<TCandidate> input,
            IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var mutated = mutator.Mutate(input, random, searchSpace, problem);
            var result = evaluator.Evaluate(mutated, random, searchSpace, problem);
            state.Calls++;
            return result;
        }
    }
}
```

Before the change, preparation and the lambda body were one `CreateExecutionInstance(scope)` method, and `Calls` was an instance field. Afterwards the two child-resolution expressions and typed calls are identical. `scope.Resolve(probe)` still infers the returned role. A consumer observing `Probe` supplies its own typed wrapper, using the same decoration facility as built-in roles.

### Wrapping and multi bases

Keep child resolution centralized, but move the protected hook outside per-binding construction. On `WrappingMutator<TCandidate>`:

```csharp
public ExecutionFactory<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>>
    CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
    where TRunSearchSpace : class, ISearchSpace<TCandidate>
    where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
{
    var wrap = CreateWrapperFactory<TRunSearchSpace, TRunProblem>();
    return scope => wrap(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildMutator));
}

protected abstract Func<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>,
    IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>>
    CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
    where TRunSearchSpace : class, ISearchSpace<TCandidate>
    where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
```

A stateless wrapper returns `child => new Execution<TRunSearchSpace, TRunProblem>(child, setting)`. A stateful wrapper creates its shared cache/counter first, then returns `child => new Execution<...>(child, cache)`. For example, [CachingEvaluator](../src/HeuristicLib/Operators/Evaluators/CachingEvaluator.cs) moves its `MemoryCache` allocation out of the binding instance's field initializer into preparation. Its transient batch lists stay invocation-local.

Multi bases apply the same pattern with the existing ordered typed child collection. Matching execution bases and protected child properties remain. This replaces `WrapExecutionInstance`, rather than adding another optional authoring route alongside it. The additional delegate is construction-only and must be included in allocation measurements.

### Algorithms and interceptors

Algorithm role methods return `ExecutionFactory<IAlgorithmExecution<...>>`. Keep one protected hook on iterative bases which prepares a constructor receiving the newly resolved interceptor:

```csharp
// On IterativeAlgorithm<TSelf, TCandidate, TSearchState>:
protected abstract Func<ResolutionScope,
    IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>?,
    IterativeAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>>
    CreateIterationFactory<TRunSearchSpace, TRunProblem>()
    where TRunSearchSpace : class, ISearchSpace<TCandidate>
    where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;

public override ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>>
    CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
{
    var create = CreateIterationFactory<TRunSearchSpace, TRunProblem>();
    return scope =>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>();
        return create(scope, typed.ResolveOptional(Interceptor));
    };
}
```

The bound iterative base offers the nongeneric protected hook and validates the bridge before any preparation; mirror today's supported run-type relationships. C2 must prove both base variants compile. Do not implement the bound variant by resolving an interceptor early and capturing it in the prepared factory.

For [HillClimber](../src/HeuristicLib/Algorithms/LocalSearch/HillClimber.cs), the replacement override is:

```csharp
protected override Func<ResolutionScope,
    IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, SingleSolutionState<TCandidate>>?,
    IterativeAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, SingleSolutionState<TCandidate>>>
    CreateIterationFactory<TRunSearchSpace, TRunProblem>() => (scope, interceptor) =>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return new Execution<TRunSearchSpace, TRunProblem>(interceptor,
            typed.Resolve(Evaluator), typed.Resolve(Creator), typed.Resolve(Mutator),
            typed.ResolveOptional(Refiner), Direction, MaxNeighbors, BatchSize);
    };
```

Its existing nested instance and step methods need no state class. Iteration position and previous search state already belong to `RunStreamingAsync`'s enumerator. The longer protected return type is the visible cost of keeping interceptor wiring in the base; do not introduce a new public delegate for every topology unless the proof shows a material readability benefit.

[GeneticAlgorithm](../src/HeuristicLib/Algorithms/Evolutionary/GeneticAlgorithm.cs) uses the same hook, but prepares its effective mutator before the lambda:

```csharp
protected override Func<ResolutionScope,
    IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, PopulationState<TCandidate>>?,
    IterativeAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, PopulationState<TCandidate>>>
    CreateIterationFactory<TRunSearchSpace, TRunProblem>()
{
    var effectiveMutator = MutationRate >= 1.0 ? Mutator : Mutator.AppliedAtRate(MutationRate);
    return (scope, interceptor) =>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem, PopulationState<TCandidate>>();
        return new Execution<TRunSearchSpace, TRunProblem>(interceptor,
            typed.Resolve(Evaluator), typed.Resolve(Creator), typed.Resolve(Crossover),
            typed.Resolve(effectiveMutator), typed.Resolve(Selector), typed.ResolveOptional(Terminator),
            typed.ResolveOptional(Refiner), PopulationSize, MaximumGenerations, Elites);
    };
}
```

The derived rate-wrapper configuration is an ordinary selectable source node with stable identity for this logical GA. It is not a generated instrumentation wrapper. Rebinding does not recreate that configuration or its state.

### Initialization requiring a resolved child

Do not add the previously sketched three-callback helper in the first implementation. No inspected production constructor establishes a need for binding-time initialization from a live child. Creating an initial population is invocation work and stays in execution; values available on a child configuration can be prepared normally.

For a future controller that snapshots a child's *current* strength, distinguish snapshot-at-first-operation from snapshot-at-first-binding. The former can use a persistent initialization flag and a typed control reference supplied to each binding; a rebound controller keeps the original snapshot. The latter is not promised by the initial scope-free preparation API. If required by an actual consumer, design and prove a once-only initializer with separate failure semantics before extending the contract. Do not hide initialization and partial failures in an ad hoc `state ??=` binder.

## 5. Child scopes, Cycle and Pipeline

Keep `CreateChildScope` as a fresh logical domain per call, inheriting applicable declarations and existing ancestor state. "Fresh scope" still does not guarantee fresh state when an ancestor already owns the source. That is true of today's Cycle reset flag and must not silently change during this work.

Add these advanced members to the existing `ResolutionScope`, not a second public scope abstraction:

```csharp
public ResolutionScope GetOrCreateChildScope(object key);
public ResolutionScope GetOrCreateChildScope(object key, Action<ResolutionScopeBuilder> declare);
```

Keys compare by reference. In a construction frame they belong to the execution record; on a standalone scope they belong to its logical domain. The first call creates the retained logical child domain and snapshots its declarations. Subsequent calls return a view over that domain with the requesting frame's binding context. They do not rerun the declaration callback. Use the same declaration-producing code for every use of a key; changing captures is not reconfiguration. A key allocated inside each binder would defeat retention. Distinct child slots need distinct stable key objects; the resolver never infers keys from call order.

Cycle's factory remains simple:

```csharp
public override ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>>
    CreateExecutionFactory<TRunSearchSpace, TRunProblem>() =>
    scope => new CycleAlgorithmExecution<TAlgorithm, TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(
        scope, Algorithms, MaximumCycles, NewExecutionInstancesPerCycle);
```

Remove its dictionary of decorated child instances. Inside each Cycle binding:

```csharp
private IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> ResolveAlgorithmExecution(TAlgorithm algorithm)
{
    var child = NewExecutionInstancesPerCycle
        ? scope.CreateChildScope()
        : scope.GetOrCreateChildScope(algorithm);
    return child.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);
}
```

Reuse mode preserves the existing algorithm-reference key behavior, including the same algorithm appearing twice in the list. Separate Cycle execution records have separate retained-domain tables. Reset mode creates a fresh domain for every actual child activation, including repeated references. Do not key reset domains just by cycle index: two invocations can have the same index. Existing `random.Fork(cycleCount).Fork(algorithmIndex)` placement remains unchanged.

Pipeline likewise returns `scope => new PipelineAlgorithmExecution<...>(scope, Algorithms)`. Its iterator retains the binding frame and calls `CreateChildScope()` for each stage activation as today. Its state handoff and `random.Fork(index)` are unchanged. An earlier stage's local children never become visible to a later sibling stage unless deliberately owned by an ancestor.

An operator budget prepares its accumulator and one private key, then reuses its declared child domain:

```csharp
var duration = new DurationAccumulator();
var childKey = new object();
return scope =>
{
    var child = scope.GetOrCreateChildScope(childKey, builder =>
        builder.Decorate(ObservedOperator, source => MeasuredOperatorFactory(source, duration, TimeProvider)));
    return new OperatorDurationBudgetAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(
        child.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), duration, MaximumDuration);
};
```

The accumulator belongs to that budget execution record; the declaration identity belongs to its retained domain. A second binding adds the requesting outer observations without resetting the budget or reinstalling its measurement. Count budgets follow the same pattern. Algorithm-duration budgets keep their stopwatch/duration locals in each invocation iterator, as their existing implementation does.

## 6. Decoration construction and advice state

Merely changing configuration factories cannot preserve state in generated wrappers. With outer A and new inner B, the required chain changes from `A(M)` to `A(B(M))`. A must retain its persistent advice state while its predecessor changes. It is therefore incorrect either to pin A's predecessor as an ordinary source dependency or to create another A state for every entire chain.

Propose changing `ResolutionScopeBuilder.Decorate(source, wrap)` to the following explicit wrapping contract while retaining its call shape:

1. `wrap` receives the original source configuration, not the previous generated wrapper configuration. Call it once per source execution record and declaration identity; prepare the resulting wrapper once. The source selection and observation attribution refer to that same original source.
2. For each binding, construct the chain in the established order. Bind each generated occurrence using a frame in which resolution of its source argument means the already-built predecessor binding.
3. That special predecessor edge changes per context. Other dependencies of the generated wrapper are ordinary pinned dependencies. Its persistent data stays with the occurrence.
4. A frame may be retained for deferred resolution. The predecessor override is immutable and belongs to that wrapper's frame, not an entry temporarily installed in a global `underConstruction` map. Do not propagate it into an unrelated child's own frame; that child's source dependency follows its own identity rules.
5. Generated wrappers are not additional node-selector targets. Explicit configured wrappers are ordinary sources with their own state, dependencies and selectable identities.

Generated advice's additional dependencies require their own ownership rule. Give each occurrence a private logical domain. Its parent is the more deeply nested of the source-owning domain and declaring domain when they are comparable in the logical tree. If they are incomparable because a retained domain is being viewed through a different declaration path, use the declaring domain. Select existing ancestor children through that parent; create missing children only in the occurrence's private domain and pin them there. The source/predecessor edge bypasses this lookup. The rule depends on stable logical identities, not whichever observer context first requests a binding.

For example, P owns M and C declares `W(M, X)`: W may reuse C's already-selected X, but a missing X is created privately for W and is never inserted into P or C. Another sibling declaration gets its own occurrence and private X. Conversely, a root declaration applied to a child-owned M uses a private domain below M's owner, so it can reuse that owner's existing X. Later ancestor creation of X does not replace a pinned private X. Two source execution records get separate occurrence domains even for the same declaration. This is a proposed advice-ownership policy and must be tested alongside inherited declarations and retained internal domains; ordinary configured wrappers continue using the normal source-node rules.

This is an intentional breaking change for callbacks that inspect the previous generated wrapper's type/settings. Existing observing/counting/measuring adapters use the parameter as their wrapped child, so their composition is retained through the predecessor edge. Audit all callbacks, including Experimental and consumer examples. Configuration transformation (`source with { ... }`) belongs before resolution; it is not this wrapping contract. Require a wrapper to retain/resolve its supplied source as the predecessor; returning the source unchanged and obvious construction cycles receive diagnostics. General semantic compliance remains an authoring contract, not something reflection or a construction stack can prove.

Ordering is still innermost-first: configuration-origin before module-origin, deeper effective binding-context layer before shallower, then declaration sequence. Determine depth from the current declaration path, not the retained logical domain's original depth. A budget child D rebound below C has effective path `P -> C -> D`, even if D was originally created under P.

Test both entry and completion order. Reusing A's state does not mean using the same A object: parent calls remain `A_parent(M_parentBinding)` and child calls become `A_child(B_child(M_childBinding))`; both A bindings advance the same A data, and both M bindings advance the same M data. Each operation reaches A once. With three parent and two child calls, A reports five and B reports two. Analyzer accumulators remain caller-owned and may intentionally aggregate different source execution records as well.

A wrapper which keeps its own mutable counter moves it into preparation, just like a stateful source composite. Two independent declarations get separate counters even if their callbacks are equal. No predecessor swap mutates an existing binding.

## 7. Specialized capabilities

Handwritten role wrappers cannot automatically implement an unknown extra interface. The existing observing mutator already hides `IAdaptableMutationStrengthInstance`, disabling EvolutionStrategy's adaptation. The factory itself does not fix that. Do not claim arbitrary extra-interface preservation.

Propose separating the existing strength control from `Mutate` into an operation-free interface, provisionally `IMutationStrengthControl`, with `double CurrentMutationStrength { get; set; }`. GaussianMutator's raw leaf offers this control over its persistent data. EvolutionStrategy stores two typed references: the decorated `IMutatorExecution` for calls, and the control for adaptation. No operation is called through an undecorated reference.

Use one advanced generic construction overload to select such a control from the selected source's raw binding:

```csharp
public TExecution Resolve<TConfiguration, TExecution, TControl>(
    TConfiguration configuration,
    Func<TConfiguration, ExecutionFactory<TExecution>> prepare,
    Func<TExecution, TControl?> selectControl,
    out TControl? control)
    where TConfiguration : class, IConfigurationNode
    where TExecution : class, IExecutionNode
    where TControl : class;

// Within EvolutionStrategy's returned binding lambda:
var mutator = scope.Resolve(Mutator,
    static target => target.CreateExecutionFactory<TRunSearchSpace, TRunProblem>(),
    static source => source as IMutationStrengthControl, out var strength);
```

The result and control use the same pinned execution record and context; the selector runs on its cached raw binding after complete resolution succeeds. It must be a side-effect-free projection, not a second construction hook. This is explicit construction-time projection of one requested source, not an ambient type-keyed service lookup. Do not multiply every role extension over controls; advanced authors can use this generic form, while normal `typed.Resolve` is unchanged.

Generated observations preserve access to that source control without implementing its interface. Explicit configured wrappers do not automatically inherit their child's control: they must deliberately expose a suitable control from their own raw binding, or report none. Automatic unwrapping could adapt the wrong child of a multi-operator composition. A consumer-defined control participates identically. Extra capabilities which themselves execute work need a declared typed role/adapter and observation semantics; they cannot be projected as an unobserved shortcut.

This overload, control separation and configured-wrapper policy are a review point, not a settled feature. C2 must prove inference at this exact call site, optional absence, custom consumer controls and the child-first collision. If rejected, supply a concrete typed alternative before claiming capability preservation or rolling out the new resolver.

## 8. Failures, iterators and resources

### Failure publication

Reserve the logical selection before preparation so a repeated request cannot allocate another logical state after failure. Use explicit states, not null as an initialization flag:

| Failure | Proposed outcome |
| --- | --- |
| Preparation/validation throws | Execution record stores the original fault. Later requests for it rethrow without calling preparation again. |
| A dependency fails during binding | The dependency's selected identity remains pinned. The parent context's binding is faulted; no completed parent is published. Already completed sibling dependencies remain valid. |
| Raw binder or decoration binder throws | Cache a fault for that execution record/context. Other completed contexts remain usable. Prepared persistent state is retained; a new context may attempt its own binding. |
| Decoration callback/preparation throws | Fault that source/declaration occurrence; do not repeatedly allocate advice data on another context. |
| Retained child declaration throws | Reserve/fault that owner's key. Do not rerun the callback and partially redeclare modules. |
| Recursive construction returns to active source/occurrence construction, including through another context/domain | Diagnose a dependency cycle. A changing context is not an escape. Only the explicit generated predecessor edge may resolve an already-built chain prefix. |
| Operation or observer callback throws | Keep existing operation semantics; construction records do not replay or roll back the operation. |

This sticky construction-failure policy is a behavior change from rebuilding after a miss. It prevents partial initialization from silently becoming a second logical execution. It provides no rollback of arbitrary user side effects. Recover by creating a genuinely independent execution scope/run; an ordinary child that inherits the same faulted execution record is not a retry mechanism. `TryResolve` retains its documented exception-to-reason behavior; it is not a hidden retry API. Include exact failure identity and preparation-count tests.

Track active synchronous construction explicitly across frames: re-entering an active execution record or occurrence fails even if `CreateChildScope` or `GetOrCreateChildScope` changes the binding context. Also detect repeating the same source configuration/compatible creation contract through fresh domains during that unfinished construction, which could otherwise allocate an endless sequence of new execution records. Completed predecessor bindings are the narrow decoration exception. Sequential child activations after construction has finished are valid; this does not prohibit Cycle from invoking the same completed algorithm repeatedly or attempt to detect arbitrary recursion during execution.

### Invocation ownership

An active iterator keeps its original binding, child references, RNG forks, previous search state and local timers. Rebinding does not transfer or clone that iterator. Pausing/resuming `AlgorithmRun` continues the same enumerator; observers introduced through another binding affect calls begun through that binding, not an already-suspended invocation. Starting an invocation on another binding is a separate invocation, not resume. Shared node state does not authorize concurrent invocations.

Prepared factories retain persistent resources for the existing logical execution lifetime. Bindings do not dispose shared resources when replaced or collected. Invocation-local resources remain in the iterator/operation's existing disposal path. The resolver currently has no general disposal contract; this proposal does not invent one. Audit `CachingEvaluator`'s `MemoryCache` and external consumer resources explicitly. If safe ownership would require a new cleanup protocol, treat that as a reviewed extension before migrating that case, not an automatic `IDisposable` cast and dispose.

## 9. Required proof and review decisions

| ID | Proposed choice to review | Evidence required before rollout |
| --- | --- | --- |
| D1 | Preserve G's pinned M even when direct C resolution selects another M; keep original logical ownership for delayed dependencies. | Collision, late dependency and no-hoisting tests; clear direct-versus-composite behavior. |
| D2 | `CreateWrapperFactory` / `CreateIterationFactory` prepare once and receive typed children later. | Full compilation of agnostic/bound bases, custom roles, representative algorithms; author ceremony comparison. |
| D3 | Retained child domains keyed explicitly, returning contextual views. | Both Cycle modes, repeated source references, two Cycle nodes, two invocations, Pipeline and budget composition. |
| D4 | Decoration callback receives original source; generated occurrence has a per-binding predecessor, private extra-child domain and weak-key lifetime. | Stateful A/B chain insertion, deferred predecessor, extra-child isolation, unrelated child, attribution, contextual depth, timing order and both collection directions. |
| D5 | Operation-free controls projected during resolution. | EvolutionStrategy/Gaussian with observation, consumer control, explicit wrapper absence/forwarding, inference. |
| D6 | Sticky preparation/occurrence faults and context-local binding faults. | Failure injection at each phase; no repeated preparation or partial publication. |
| D7 | No first-binding child-dependent initialization helper; paused iterators keep original bindings. | Audit remaining constructors; pause/resume and exception/disposal lifecycle tests. |

The factory direction is already selected. These proposals make it concrete enough to review, but none of the unexecuted proof or cost claims is a completed result. Keep ordinary public guides and canonical glossary descriptions on current behavior until the corresponding implementation lands.
