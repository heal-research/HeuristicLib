# HeuristicLib fundamental architecture review

Date: 2026-11-04.

Status: architectural analysis and recommendations. This document records the review requested by the developer; it does not record acceptance of a replacement architecture or authorize implementation.

The review evaluates the current source together with the accepted end state of the branch plan, assuming that plan is completed. It preserves the distinction between source findings, recommendations and architectural hypotheses requiring validation. The existing [architecture simplification analysis](architecture-simplification-analysis.md) is separate and unchanged.

**HeuristicLib has a small optimization model surrounded by an increasingly general execution framework. The largest simplification opportunity is to reduce what that framework promises.**

The configuration/executor split is worth preserving. So are typed operations, immutable candidate flow, explicit randomness and composition. The complexity comes mainly from combining those with contextual decoration, implicit capability discovery, multiple construction views and sophisticated sharing rules.

Reducing the glossary alone would conceal that complexity. A substantial improvement requires changing some guarantees.

The baseline is the accepted end state of [the operator-state-lifetimes plan](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04), alongside the source, executable examples and [rejected approaches](developer-backlog.md#discussed-tried-and-rejected). Consequently, resource handles, observation points, run setup and per-trial aspect setup are treated as already removed. Those removals are already part of the branch, not additional recommendations below. Remaining open design questions are still open.

**The fundamental model can be explained in one paragraph.**

> A problem defines admissible candidates and how to evaluate them. An algorithm searches by composing typed operators. Immutable configurations describe that composition. A run constructs executable objects with explicit dependencies and private state, executes them, and publishes progress and results.

The essential concepts behind that explanation are:

| Concept                             | Essential responsibility                                                       |
| ----------------------------------- | ------------------------------------------------------------------------------ |
| Candidate and evaluated candidate   | A proposed solution and its assessed quality.                                  |
| Problem, search space and objective | Evaluation, admissibility and what “better” means.                             |
| Operator                            | One meaningful operation, such as creation, mutation, evaluation or selection. |
| Algorithm                           | Search orchestration using operators and other algorithms.                     |
| Configuration and executor          | Reusable description versus executable realization.                            |
| Run and search state                | One execution and its public progress values.                                  |

These are conceptual groups, not a proposal for six universal interfaces.

Some distinctions genuinely matter. Search-space membership and objective ordering are different responsibilities. Pareto dominance and a total ranking are different semantics. Public progress and private execution state have different purposes.

Conversely:

- A meta-algorithm is an algorithm that uses algorithms.
- An encoding organizes a representation and its associated components.
- An experiment coordinates independent runs.
- Retry is a particular composition.
- A trace is a particular analysis.

None needs to expand the mandatory execution vocabulary.

The glossary also mixes optimization, infrastructure, analysis, machine learning and legacy terminology. Readers should not need all of those vocabularies to understand the core. However, reorganizing documentation is only useful after reducing the underlying obligations.

**The ordinary algorithm implementations already demonstrate this small model.**

The execution loop in [GeneticAlgorithm.cs](../src/HeuristicLib/Algorithms/Evolutionary/GeneticAlgorithm.cs) essentially creates, evaluates, selects, crosses, mutates and replaces. It calls retained typed collaborators. [HillClimber.cs](../src/HeuristicLib/Algorithms/LocalSearch/HillClimber.cs) has similarly direct logic.

That is a good foundation. The redesign should make constructing and extending those collaborators almost as understandable as calling them.

The principles to retain are:

1. **Reusable descriptions produce independent executions.** Reusing a configuration must not accidentally share mutable state across trials.
2. **Composition uses meaningful contracts.** Dependencies are visible children with ordinary methods.
3. **Ownership determines state lifetime.** Calls own locals; executors own execution fields; runs own retained analysis results.
4. **Public progress contains domain data.** It is neither a service container nor a complete execution checkpoint.
5. **Behavior-affecting dependencies are explicit.** Randomness, cancellation, external data and mutable adaptation need identifiable owners.
6. **Preparation establishes runtime relationships.** Repeated computation follows already-bound object references.
7. **Extensibility does not require universal interoperability.** A custom operation should be usable without implementing every optional framework service.

The last principle is a deliberate change from the current direction.

**The clearest cause of complexity is construction changing construction policy.**

Consider [OperatorCountBudgetAlgorithm](../src/HeuristicLib/Algorithms/Control/OperatorCountBudgetAlgorithm.cs). It appears to wrap an algorithm with a work limit. During construction, however, it calls `scope.AddAspect(...)`. Its counting can affect other components in the same scope, including callers outside its child algorithm.

That creates a chain of additional rules:

- Advice cannot be installed after affected executors have been resolved.
- Creating child scopes restricts later changes.
- Advice installed by a configuration must exclude that configuration itself.
- Explicitly shared configurations need private construction scopes.
- A construction failure can invalidate the entire scope tree.
- Sharing distinguishes the raw executor from the wrappers applied at each use.

These are visible in [ResolutionScope](../src/HeuristicLib/Execution/ResolutionScope.cs). The [step 1 decisions](operator-state-lifetimes.md#step-1-decisions-accepted-on-2026-11-04) explicitly trace the special scope for a shared executor to problems involving shared budgets.

The implementation is answering real contradictions in the contract. Cleaning up its classes will not remove those contradictions.

**The strongest recommendation is that constructing a component must not change the construction rules of its siblings.**

A budget should establish a counter, connect counting to specified work, and use that counter to stop its child. An enclosing composite can construct those collaborators together. It should not modify an ambient policy encountered by whichever sibling happens to build next.

Automatic instrumentation can remain available, but its placement must be determined before constructing the affected objects.

There is an important caveat: a general preparation pass is not free. Current construction hooks can discover children procedurally. Automatically inspecting every graph would require declared dependencies, restrictions on hooks or additional metadata. First establish fixed construction rules and explicit composition, then justify any automatic preparation facility against concrete examples.

**Sharing should mean sharing an executable object with stable children.**

Suppose two algorithms share a caching evaluator. That evaluator contains a reference to its underlying evaluator.

A second caller can wrap the cache to observe its own requests. It cannot give the existing cache a different underlying evaluator without changing the shared object.

That ordinary object model suggests a simpler contract:

- A shared executor retains its fields, children and internal instrumentation.
- A wrapper at a particular use observes or modifies that use.
- Observation inside the shared component is configured where that component is constructed.
- Fresh algorithm activations construct fresh private state.
- Objects intentionally retained across activations are passed by reference.

This gives a straightforward explanation for an important case:

> A cycle owns one shared cache. Each child activation gets a fresh budget counter. All activations record into one run-owned trace.

Those are three ownership relationships. They do not require three interchangeable lifetime mechanisms.

The guarantee to give up is **arbitrary context-specific decoration of the internals of an already shared object**. The current design already limits that behavior, but explains the limitation through resolution rules. Stable object relationships would make it inherent and predictable. Sharing the completed executable object would also change the current per-request decoration guarantee; this is an architectural tradeoff, not a behavior-preserving cleanup.

Sharing data independently from behavior may still require a separate collaborator—for example, a cache store shared by two evaluator objects. That should be a concrete domain dependency when needed, rather than a universal private-state management system.

**The operator protocol is the second major simplification target.**

The present design combines several ambitions:

- Candidate-only configuration slots.
- Implementations specialized for particular problems and search spaces.
- Named runtime methods such as `Mutate` and `Evaluate`.
- Generic retry, routing, counting and timing.
- Arbitrary consumer-defined operations.
- Several operations sharing one executor.
- Controls preserved through transparent wrappers.

The combination drives `IOperation`, invocation structs, wrapping executors, routing executors and construction-hook verification. [Mutation.cs](../src/HeuristicLib/Operators/Mutators/Mutation.cs) makes that supporting protocol concrete. [ConstructionHooks.cs](../src/HeuristicLib/Execution/ConstructionHooks.cs) uses cached reflection to enforce one construction hook across operation interfaces.

These mechanisms cannot all disappear while every guarantee remains unchanged. Candidate-only configuration slots also deliberately omit run types; specialized compatibility is restored through preflight and construction checks, rather than being fully established at compile time.

Two changes deserve consideration.

First, **make ordinary typed participation the basic extension contract**. A custom component should describe its settings, construct its executor and implement its meaningful operation. Universal wrapping and routing should be additional capabilities an operation opts into. The advanced protocol must remain equally available to library and consumer code.

This is interface segregation applied to authoring: a component should implement the services it needs.

Second, investigate **binding stable problem dependencies when executors are constructed**. An illustrative runtime contract could be:

```csharp
// Design sketch, not an implemented or validated API.
interface IMutationExecutor<TCandidate>
{
    IReadOnlyList<TCandidate> Mutate(
        IReadOnlyList<TCandidate> parents,
        IRandomNumberGenerator random);
}
```

A specialized implementation would retain its typed problem and search-space references in private fields. This is ordinary constructor injection.

It could reduce:

- Generic parameters throughout executor interfaces.
- Authoring arity ladders.
- Run-type propagation through decorators.
- Repeated problem/search-space parameters.

This is a promising architectural hypothesis, not a proven replacement. It changes whether one executor can be invoked against several problem instances. It must demonstrate dynamic problems, transformed problems, specialized operators and observation metadata. Construction-time compatibility checks would still be necessary.

Keep randomness explicit per invocation: retry, routing and deterministic forks depend on it.

Do not revive the rejected universal `Invoke(input, context)` layer underneath named interfaces. The repository already tried that combination and found additional forwarding and overhead; see [typed operator invocation](developer-backlog.md#typed-operator-invocation). Binding dependencies earlier is a different proposal.

**Runtime control discovery should become a narrow domain concern.**

[EvolutionStrategy](../src/HeuristicLib/Algorithms/Evolutionary/EvolutionStrategy.cs) requests an optional `IMutationStrengthControl`, then changes mutation strength using its success rule. Supporting that through arbitrary decorators contributes to the general [ExecutorControls](../src/HeuristicLib/Execution/ExecutorControls.cs) mechanism.

The underlying requirement is specific: an adaptive algorithm needs a meaningful adaptation capability.

Possible explicit designs include an adaptive-mutator contract or an adaptation policy owning the strength used by mutation. Both have costs, but those costs stay close to the behavior.

A wrapper choosing among several mutators cannot necessarily offer one meaningful mutation strength. Forwarding a discoverable interface does not establish substitutability.

Stop promising that arbitrary composition transparently preserves arbitrary capabilities. Require each composition to expose the domain capabilities it actually preserves. That is a stronger application of Liskov substitution than universal forwarding.

**Keep run-owned results, but simplify how collectors participate.**

The completed branch correctly distinguishes execution state from collected results:

- A private budget can restart every turn.
- A trace can span the whole run.

Trying to give both the same lifetime would recreate an already rejected problem.

The unnecessary coupling is between collection and automatic placement. [Instrument](../src/HeuristicLib/Execution/Results/Instrument.cs) exposes result creation and aspect creation. The custom histogram in [InstrumentSpecs](../test/HeuristicLib.Tests.ApiUsageSpecs/Analysis/InstrumentSpecs.cs) consequently implements result construction, aspect construction and recorder construction for a small collection operation.

A simpler responsibility split is:

- An immutable analysis description creates a run-owned collector.
- The collector records typed observations and exposes results.
- Placement connects operation boundaries to that recording method.
- Automatic placement installs the same connection that explicit composition uses.

This need not become three new public interfaces. The aim is fewer obligations for someone writing a collector.

Keep observation at meaningful calls and yielded states. Replacing aspects with an event bus would introduce another routing and subscription system.

Clocks, retention, snapshots and projections belong to analysis. They should not be prerequisites for understanding execution. Also, [Moment.cs](../src/HeuristicLib/Analysis/Tracing/Moment.cs) currently constructs an immutable keyed collection for each retained sample. A layout established once per trace could store readings in fixed positions or typed columns. That is a concrete representation improvement, although its performance benefit needs measurement.

**The internal systems deserve different dispositions.**

| System or responsibility                    | Recommendation                                                                             |
| ------------------------------------------- | ------------------------------------------------------------------------------------------ |
| Typed domain operations and candidate flow  | **Keep as core.**                                                                          |
| Configuration-to-executor construction      | **Keep and narrow.** Construct dependencies, validate compatibility and initialize fields. |
| Construction identity and cycle checks      | **Keep internally.** A small preparation table is reasonable.                              |
| Mutable state ownership                     | **Keep as core.** Express through ordinary objects and enclosing owners.                   |
| Fresh activations and explicit sharing      | **Keep with simpler semantics.** Stable shared objects; fresh private fields.              |
| Random stream assignment                    | **Keep explicit.** Determinism requires logical assignment, not merely a seed.             |
| Run lifecycle, cancellation and streaming   | **Keep.** These own real execution responsibilities.                                       |
| Run-owned result binding                    | **Keep when analysis is used.** Bind direct recorder references before calls.              |
| Construction-time mutation of advice policy | **Remove.** This causes many secondary rules.                                              |
| Pointcuts and automatic decoration          | **Optional preparation facility.** Keep out of the basic authoring contract.               |
| Universal operation protocol                | **Narrow or make opt-in.** Retaining its guarantees means retaining its cost.              |
| Arbitrary control discovery                 | **Replace or constrain.** Prefer explicit semantic capabilities.                           |
| Invariant implication and composition       | **Optional validation.** Useful diagnostics, not the execution foundation.                 |
| Operator recommendations                    | **Keep small.** Configuration defaults, separate from compatibility.                       |
| Clocks and trace retention                  | **Analysis features.**                                                                     |
| Experiments and grids                       | **Orchestration over ordinary runs.**                                                      |
| Serialization and parameter discovery       | **Integration/preparation tooling.**                                                       |

“Optional” means a dependency and learning boundary. Moving the same mechanisms into another assembly would not itself simplify anything.

Run lifecycle deserves particular care. Retaining an iterator for pause/resume is a real responsibility. Starting a new executor from the last search state is a different operation. A generic state-machine framework would not eliminate that distinction.

Likewise, experiments need stable trial identity, deterministic random forks, concurrency limits and failure isolation. They can provide those by coordinating ordinary runs.

**Serialization needs an explicit contract beyond records.**

The accepted branch temporarily allows delegates inside attached analysis definitions. Therefore, assuming the branch is complete does not make every resulting configuration persistable.

There are two additional issues:

1. **Type construction:** serialized data must identify concrete implementations, required values and external problem/data references.
2. **Graph identity:** two equal records and two references to one record can have different sharing and observation semantics.

A JSON round trip that duplicates a shared node can change execution. A `with` replacement can also leave reference-selected instrumentation pointing at the old child.

Guarantee a persistable optimization description and bind arbitrary runtime callbacks separately during preparation. Persistable analysis descriptions can be added where there is a concrete requirement. This deliberately revisits the current decision to put all attachments inside algorithm wrappers.

Identity can be represented in the serialized graph and resolved to object references before execution. It need not appear in runtime dispatch.

Also challenge the [parameter-grid proposal](configuration-parameters-and-experiment-persistence.md)'s prohibition on reflection during parameter discovery. Cold-path serializer metadata or reflection does not violate efficient typed execution. Requiring every component author to maintain a parallel property schema may create another framework without solving a runtime problem. That document remains a proposal, rather than part of the assumed completed branch.

**The performance requirement needs a precise boundary.**

Ordinary operator calls already use retained references; they do not generally resolve dependencies per candidate.

However, [CycleAlgorithm](../src/HeuristicLib/Algorithms/Composition/CycleAlgorithm.cs) creates a child scope and resolves an algorithm during each turn. Observed traces also perform keyed work while recording.

If “no lookup during the run” includes fresh turns, known child configurations need prepared typed construction functions. A turn would invoke a factory that creates fresh fields and captures already-bound shared dependencies.

That adds preparation machinery, so it should earn its cost. A useful rule is:

> No dependency discovery or framework dispatch on repeated operation paths. Prepare known activations in advance; treat genuinely new configurations as new preparation boundaries.

Intentional evaluation caches and problem-data indexes remain legitimate algorithmic work.

Also distinguish a stopping criterion from a hard work limit. Current budgets check after yielded states and can overshoot. A strict evaluation cap requires admission control at the evaluation boundary, including semantics for batches and concurrent calls. Simplifying construction cannot erase that behavioral distinction.

**SOLID and KISS support reducing guarantees, not maximizing interface decomposition.**

For this library, apply them as follows:

- **Single responsibility:** constructing objects should not also mutate sibling instrumentation policy.
- **Open/closed:** consumer components extend small contracts without requiring library changes.
- **Liskov substitution:** compositions expose only capabilities whose meaning they preserve.
- **Interface segregation:** implementing a custom operation should not require implementing routing, observation and control discovery.
- **Dependency inversion:** algorithms depend on domain operations; optimization does not depend on trace storage or experiment metadata.
- **KISS:** ownership and stable object references should explain behavior without construction-order exceptions.

Microsoft’s framework guidance explicitly cautions that excessive abstractions harm usability and recommends testing abstractions through several concrete implementations and consumers. That is particularly relevant here. [Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/abstractions-abstract-types-and-interfaces)

The sklearn comparison is useful because of its consistent extension obligations: parameters describe an estimator, fitting establishes learned state, and composition follows recognizable conventions. Its cloning model reinforces the separation between reusable settings and execution state. HeuristicLib’s explicit configuration/executor split is a good way to express that separation in C#. [Estimator development guide](https://scikit-learn.org/stable/developers/develop.html)

Sklearn also has substantial metadata-routing machinery. Its lesson is to keep advanced mechanisms outside the ordinary mental model and constrain their reach. [Metadata routing](https://scikit-learn.org/stable/metadata_routing.html)

**Prove a smaller architecture with a deliberately difficult, bounded prototype before rewriting the library.**

It should demonstrate:

1. A GA and hill climber using the same ordinary operators.
2. Fresh child activations with one shared cache and fresh local budgets.
3. One trace spanning those activations.
4. Counting cache requests versus actual evaluations through explicit placement.
5. An adaptive mutator under timing and under a choice of mutators.
6. A consumer-defined operation and a custom collector.
7. Independent concurrent trials, including partial failure.
8. Serialization and parameter replacement preserving intended graph identity.

Judge it by removed obligations: no construction-order-dependent advice, no self-adder exception, no special scope needed because a budget mutates its surroundings, and no mandatory aspect authoring for a collector. Measure preparation, activation and operation costs separately.

The recommended architectural direction is **immutable descriptions → fixed construction → ordinary typed objects with explicit ownership**. Keep automatic conveniences only where they can be explained as preparation over that model. Investigate run-bound problem dependencies as the next substantial simplification.

This was a source and design review. No implementation was changed, and no performance claims here are based on new benchmarks. Architectural recommendations require explicit acceptance before implementation under the repository’s contributor contract.
