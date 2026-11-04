# HeuristicLib architecture simplification analysis

Date: 2026-11-04. Status: analysis and recommendations, not an accepted replacement architecture. No implementation changes are authorized or recorded by this document.

## Finding

HeuristicLib has a relatively small optimization model inside an increasingly general execution framework. The main simplification opportunity is to reduce what that framework promises, especially the interactions between construction, contextual decoration, sharing and capability discovery.

The configuration/executor separation is valuable and should survive. So should typed domain operations, immutable candidate flow, explicit randomness and composition. Those are not the source of the conceptual growth.

The strongest recommendation is a smaller construction kernel that produces ordinary typed objects with stable children. Automatic instrumentation should be an optional preparation facility over that kernel. Construction hooks should not change the rules under which their siblings are constructed. Specialized adaptation should have explicit domain contracts. Analysis and experiment tooling should remain usable without becoming prerequisites for understanding an operator.

This is a change in guarantees, not merely a reorganization of namespaces. Keeping all existing guarantees while reducing the names would conceal complexity rather than remove it.

## Baseline and evidence

The analysis assumes the current branch plan is complete, as requested. The latest accepted sections of [operator state lifetimes](operator-state-lifetimes.md), especially “Advice and scope layers,” “Design review,” “Implementation sequence,” and the step 1 and step 2 decisions, supersede older descriptions in the glossary and architecture documentation.

The assumed finished design includes:

- One executor per configuration reference within a resolution scope; explicit sharing extends lifetime across child scopes.
- Scope-level advice, added during construction, and per-request decoration of shared executors.
- Immutable instruments and one mutable result per instrument per run.
- Explicit `Advised`, `Observed`, `Counted` and `Timed` composition.
- Observation of operator invocations, algorithm calls and yielded states.
- Removal of resource handles, supply APIs, run setup/builders, observation points and per-trial aspect setup.

Removing those already scheduled mechanisms is therefore not a new recommendation here. The [serializable experiment grids document](configuration-parameters-and-experiment-persistence.md) is a separate proposal, not an accepted part of that completed architecture. Remaining unresolved implementation details are not treated as settled merely because the branch is assumed complete.

Important source evidence:

| Area                                         | Evidence                                                                                                                                                                                                                                                                 |
| -------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Ordinary optimization                        | [GeneticAlgorithm.cs](../src/HeuristicLib/Algorithms/Evolutionary/GeneticAlgorithm.cs), construction around line 40 and search steps around line 81; [HillClimber.cs](../src/HeuristicLib/Algorithms/LocalSearch/HillClimber.cs), execution around line 69               |
| Problem and progress                         | [IProblem.cs](../src/HeuristicLib/Problems/IProblem.cs); [ISearchSpace.cs](../src/HeuristicLib/SearchSpaces/ISearchSpace.cs); [PopulationState.cs](../src/HeuristicLib/Algorithms/SearchStates/PopulationState.cs)                                                       |
| Construction interactions                    | [ResolutionScope.cs](../src/HeuristicLib/Execution/ResolutionScope.cs), `AddAspect`, `Share`, `Build`, `MatchingAdvice`, `Node` and `Tree`                                                                                                                               |
| Local-looking budget with scope-wide effects | [OperatorCountBudgetAlgorithm.cs](../src/HeuristicLib/Algorithms/Control/OperatorCountBudgetAlgorithm.cs), construction around lines 36 and 103                                                                                                                          |
| Universal operation support                  | [IOperator.cs](../src/HeuristicLib/Operators/IOperator.cs); [IOperation.cs](../src/HeuristicLib/Operators/IOperation.cs); [Mutation.cs](../src/HeuristicLib/Operators/Mutators/Mutation.cs); [ConstructionHooks.cs](../src/HeuristicLib/Execution/ConstructionHooks.cs)  |
| Adaptive capability discovery                | [EvolutionStrategy.cs](../src/HeuristicLib/Algorithms/Evolutionary/EvolutionStrategy.cs), lines 43 and 109 onward; [ExecutorControls.cs](../src/HeuristicLib/Execution/ExecutorControls.cs)                                                                              |
| Result construction and attachment           | [Instrument.cs](../src/HeuristicLib/Execution/Results/Instrument.cs); [RunResults.cs](../src/HeuristicLib/Execution/Results/RunResults.cs); [InstrumentSpecs.cs](../test/HeuristicLib.Tests.ApiUsageSpecs/Analysis/InstrumentSpecs.cs), custom collector around line 109 |
| Analysis runtime representation              | [TraceAnalyzer.cs](../src/HeuristicLib/Analysis/Tracing/TraceAnalyzer.cs); [Moment.cs](../src/HeuristicLib/Analysis/Tracing/Moment.cs); [Clock.cs](../src/HeuristicLib/Inspection/Clock.cs)                                                                              |
| Deferred construction and experiments        | [CycleAlgorithm.cs](../src/HeuristicLib/Algorithms/Composition/CycleAlgorithm.cs), line 139; [GridExperiment.cs](../src/HeuristicLib/Experiments/GridExperiment.cs); [ExperimentRun.cs](../src/HeuristicLib/Experiments/ExperimentRun.cs)                                |

These are structural findings from source, executable specifications, documentation and plan history. No new performance benchmark or build validation was performed for this analysis. Historical measurements are identified as historical.

## The fundamental model

A compact explanation of HeuristicLib can be:

> A problem defines valid candidates and how to judge them. An algorithm searches by composing typed operators. Immutable configurations describe that composition. A run builds independent executable objects, supplies randomness and cancellation, and produces progress and results.

That explanation covers both a genetic algorithm and a hill climber. A meta-algorithm is an algorithm using other algorithms. An encoding organizes a representation and its useful components. Neither needs a second execution paradigm.

The important concepts are these:

| Concept                    | Essential responsibility                                       | Boundary                                                                      |
| -------------------------- | -------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| Candidate                  | A proposed answer represented as a stable value                | Does not carry execution services or hidden adaptation state                  |
| Problem                    | Defines admissibility, evaluation and objective semantics      | Can expose search space and objective as distinct useful domain values        |
| Operator                   | Performs one meaningful typed operation                        | Its signature and semantics describe its inputs, output and dependencies      |
| Algorithm                  | Owns search orchestration and composes operators or algorithms | Produces progress; callers should not assemble its search loop                |
| Configuration and executor | Reusable immutable description versus executable realization   | Execution fields are created for an execution lifetime, not stored in records |
| Run and search state       | One execution and its public progress values                   | A state is not an executor checkpoint or a bag of internal services           |

This is a conceptual grouping, not a proposal for a new universal `Component` superclass. Search spaces, objective vectors, evaluated candidates and populations remain useful types. Reducing the vocabulary should not erase real distinctions such as partial Pareto ordering versus a total ranking.

Likewise, an algorithm and a mutator need not share an invocation interface merely because both are composable. An algorithm's streamed lifecycle and a batch mutation's successful return have different semantics.

### Principles that deserve to be core

1. **Describe once, execute independently.** Reusing a configuration must not accidentally share mutable execution or analysis state across runs.
2. **Compose through meaningful typed contracts.** A child is an explicit dependency. The runtime calls its ordinary method through a retained reference.
3. **Ownership determines mutable state lifetime.** An executor owns its fields; a call owns its locals; a run owns its retained results. Deliberately shared objects have an explicit enclosing owner.
4. **Keep public progress separate from private execution state.** Passing a population to another algorithm does not transfer its caches, private counters or suspended iterator.
5. **Make behavior-affecting inputs explicit.** Randomness, cancellation, scheduling, external data and dynamic-problem revision must have identifiable owners and contracts.
6. **Do framework work before repeated computation.** Discover types, bind dependencies and select decorators before the affected calls execute. A new activation should use prepared construction where practical.
7. **Preserve semantic composition.** A wrapper is substitutable only for capabilities it actually preserves. Similar method shapes do not prove equivalent behavior.
8. **Require a concrete use for generality.** A consumer-defined operation must be usable through a small contract. It need not automatically support every generic framework service.

## The internal systems that are actually necessary

| System                                  | Necessary responsibility                                                     | Recommended disposition                                                            |
| --------------------------------------- | ---------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| Typed domain contracts                  | Validity, scoring, meaningful search operations and public progress          | Keep                                                                               |
| Configuration construction              | Turn data descriptions into executable dependencies and private fields       | Keep a small kernel                                                                |
| Build-time identity tracking            | Distinguish repeated references from equal independent configurations        | Keep internally; define serialization semantics                                    |
| Activation ownership                    | Rebuild private state for independent runs, restarts and child activations   | Keep; avoid exposing a general lifetime-policy framework                           |
| Explicit runtime sharing                | Preserve a cache or adaptive object across selected child activations        | Keep a bounded rule over stable objects                                            |
| Run lifecycle                           | Exclusive execution, retained iterator, cancellation, completion and failure | Keep where these user capabilities are required                                    |
| Random stream assignment                | Make stochastic work repeatable independently of task scheduling             | Keep explicit; a seed alone is insufficient                                        |
| Result ownership and binding            | Create independent collectors once per run and bind writer references        | Keep the invariant; simplify the public protocol                                   |
| Observation decorators                  | Capture meaningful calls and yielded states                                  | Keep typed, optional and directly composable                                       |
| General AOP resolver                    | Dynamically add contextual advice while building and sharing graphs          | Remove from the minimal kernel; replace automatic placement with fixed preparation |
| Operation descriptor protocol           | Implement generic wrappers over arbitrary named operations                   | Conditional implementation support, not a mandatory basic extension contract       |
| Generic control discovery               | Find arbitrary capabilities through arbitrary decorators                     | Replace broad guarantee with explicit domain capabilities where practical          |
| Invariant implication/composition       | Infer declared compatibility of operators and search spaces                  | Optional validation facility                                                       |
| Clocks, moments and retention hierarchy | Collect and project analysis histories                                       | Optional analysis vocabulary; simplify representation                              |
| Experiment materialization/scheduling   | Construct independent trials and retain their outcomes                       | Higher-level orchestration of ordinary runs                                        |
| Parameter discovery and persistence     | Load, inspect, vary and serialize configuration graphs                       | Integration tooling before execution                                               |

“Optional” describes dependency and learning boundaries, not necessarily separate NuGet packages. Moving types between assemblies without changing the contracts does not solve the problem.

## Why complexity grows

### Construction changes the rules of construction

`ResolutionScope` combines object identity, hierarchical ownership, advice inheritance, wrapper order, deduplication, late advice installation and construction failure handling.

Consider a budget wrapper. Its apparent responsibility is to run its child until selected work reaches a limit. Its construction instead creates counting advice and adds it to the shared resolution scope. That advice can affect other configurations resolved in the same scope, including work outside the budget's declared child. The remarks on `OperatorCountBudgetAlgorithm` explicitly describe this.

Once that is permitted, important questions follow:

- What if a sibling resolved the selected executor before the budget installed advice?
- What if the scope already created a child scope with inherited advice?
- Does advice introduced while constructing a node also apply to that node?
- What if the same shared object was already built under a different context?
- Which partial construction state remains valid after advice or construction throws?

The current guards, self-adder exclusion and tree-wide failure rule answer these questions coherently. They are consequences of the contract, not merely poor coding.

The history supplies unusually direct evidence. The backlog entry “Building shared executors in the sharing scope itself” explains why each explicitly shared node needs a private child scope: a shared budget would otherwise mutate its owner's advice after child scopes exist or count sibling work. It explicitly names a different budget mechanism as a reason to reconsider the ruling.

**Recommendation:** construction creates objects under fixed rules. A configuration should not mutate the decoration policy of its siblings. Local counting is explicit composition, and automatic instrumentation is determined before constructing the affected graph.

This requires relocating or replacing the current budget attachment behavior. Freezing `AddAspect` without changing those callers would only break the library.

### Shared objects have stable children, but advice is contextual

A shared cache executor contains its evaluator reference. A later caller can wrap that cache, but cannot retroactively change its evaluator without changing the shared object for other callers.

The current design therefore distinguishes the shared raw executor, the context that built its children, and each request's outer advice. That is a substantial conceptual burden for someone who just wants to count evaluations.

**Recommendation:** share the completed executable object, including its fixed children and node-level decoration. Observe one caller through an explicit wrapper at that use. Do not promise that arbitrary contextual advice can enter an already shared composite's internals.

This makes an existing limitation explicit in ordinary object terms. It changes the current per-request wrapping guarantee and needs acceptance. It does not make all contextual use cases disappear.

### Universal services require a universal protocol

The operator model simultaneously supports candidate-only consuming slots, specialized problem/search-space/state implementations, named executor methods, external operations, generic retry/choice/count/timing and multi-operation state ownership.

`IOperation<TSelf>`, `OperationInfo`, invocation structs, wrapping executors and routing executors support that combination. `Mutation` demonstrates the complete protocol. `ConstructionHooks` additionally uses cached reflection over interface mappings to enforce one construction hook across several operation interfaces.

It would be misleading to label all this redundant while demanding the same guarantees.

**Recommendation:** preserve named typed execution methods. Make the advanced operation protocol necessary only when a custom operation wants those universal services. A specialized custom component should remain fully usable through its ordinary typed configuration/executor contract without joining every service.

This accepts that generic conveniences are not automatic for every arbitrary operation. It must not privilege built-in components: consumers must be able to opt into exactly the same advanced protocol. If universal support for all operations is indispensable, retain the descriptor machinery and recognize its irreducible cost under the current C# API shape.

Do not replace it with a universal `Invoke(input, context)` while retaining all named role interfaces. That was prototyped and rejected. The historical pass-through benchmark favored the role-specific path, and nominal interface forwarding remained. A genuine alternative would have to change the public execution contracts, not add another protocol underneath them.

### Optional capabilities become obligations for every wrapper

Evolution strategy resolves an optional mutation-strength control, implemented by Gaussian mutation. Generic control discovery and forwarding then become responsibilities of all transparent decorators.

The difficult question is semantic: what is the strength of a random choice between two mutators, or a pipeline containing two adaptive mutators? A generic discovery interface cannot answer that on its own.

**Recommendation:** adaptation should be an explicit domain capability or an adaptive composite owning feedback state. An algorithm requiring adaptive mutation should say so. A decorator can explicitly preserve that contract when meaningful; arbitrary decoration should not imply preservation of arbitrary capabilities.

Do not delete control forwarding until this dependency is replaced. Making the existing helper internal is a useful visibility change, but it does not remove the architectural obligation.

### Analysis has another construction and attachment model

An immutable definition creating fresh run state is sound. However, the public instrument protocol combines result construction, dependent result lookup, aspect construction and recording. The custom offspring-size histogram in `InstrumentSpecs` needs this machinery to maintain one dictionary after mutation calls.

Also, definitions carry a source whose meaning changes with attachment: automatic placement uses it, while explicit `Observed` records at the wrapper location even if the definition originally named another configuration. `InstrumentPlacementTests` deliberately verifies replacement-child behavior.

**Recommendation:** a custom collector needs a fresh per-run state and a typed recording operation. Source selection belongs to attachment; it should not be part of a collector definition whose target is ignored under another attachment mode. These responsibilities can be separated without requiring another pair of public class hierarchies.

Bind collectors and concrete writer references during ordinary graph preparation. Retain a result table for callers if useful. A separate public result-and-runtime-aspect factory lifecycle should not be mandatory for a simple collector.

## A smaller coherent architecture

The recommended direction has three responsibilities, not three new public frameworks:

1. **Describe:** immutable typed records hold settings and child configurations. Integration tooling loads, expands and validates these graphs.
2. **Build:** construction binds typed dependencies, creates private state and per-run collectors, determines stable sharing, and installs any preselected decorators. It also prepares constructors for known future activations.
3. **Execute:** algorithms call retained child references, yield progress and record into already bound sinks. Runs own lifecycle and retained results.

The following rules would make that architecture explainable:

- Reusing a configuration reference within an activation reuses one executable object. Equal independent records are not silently merged.
- A new activation gets new private state unless an enclosing composition deliberately retains a particular object.
- A shared object keeps its children and instrumentation. Sharing does not mean reconstructing it under each caller's context.
- A decorator at one use affects that use. An observer attached to a shared node sees that node's calls.
- Decoration order is visible nesting. Automatic placement expands to a fixed order with the same behavior.
- Per-run results are fresh independently of activation lifetimes. Multiple activations can write to one run-owned trace.
- Construction does not mutate policies affecting already built or sibling objects.
- Dynamic framework discovery is absent from repeated calls. Typed activation factories handle known restart structures.

This does not require an expression-tree interpreter, generated code, a public compiled-plan abstraction, a service locator or an execution opcode language. Ordinary constructors, interfaces, records and prepared factory objects are enough to investigate the design. Those factory objects must not become a new universal policy framework.

### Concrete case: cache shared between alternating algorithms

Two child algorithms alternate, each with a private per-turn budget, while using one cache for the enclosing run.

The runtime structure should have one cache executor retained by the outer composition and fresh child executors for each activation. Those fresh objects receive the retained cache reference through their prepared construction. Their budget counters are private fields or explicitly bound local accounting objects. A run-wide trace is a separate sink to which the relevant wrappers hold references.

Three measurements have different locations:

- Requests from child A: a counting wrapper around A's use of the cache.
- All cache requests: a counting wrapper on the shared cache object.
- Actual expensive evaluations: a counting wrapper around the evaluator inside the cache.

The distinction comes from graph placement. No advice should change meaning based on which child first requested the shared cache.

The difficult remaining requirement is a local budget automatically selecting arbitrary deep nodes in a nested algorithm. Explicit composition may require the caller or algorithm author to expose the relevant dependency. If that convenience must remain, its placement needs preparation before construction; it cannot continue secretly mutating a shared scope. The pilot must prove this case rather than assume it solved.

### Concrete case: two traces share an evaluation counter

Construct one counter for the run, one counting wrapper at the chosen boundary and two trace sinks. Both traces read the same counter directly. The wrapper updates it before completed-call observations are recorded, so both samples include that call.

Attaching a second trace must not add a second counting wrapper. Preparation can normalize the shared attachment once. This is a real invariant even after removing reference-based advice deduplication; moving it does not eliminate the obligation.

### Concrete case: adaptive mutation

An adaptive mutation composite owns its strength and update logic, or exposes a specific feedback contract to the algorithm. A timing decorator can explicitly preserve that contract. A choice of unrelated mutators exposes no adaptive-strength contract unless the choice itself defines what updating strength means.

This gives up automatic discovery through arbitrary wrappers, but makes capability preservation a domain decision rather than a global convention.

## Simplification opportunities outside the kernel

### Collapse the authoring hierarchy

Mutation alone has twelve base declarations across `Mutator`, executor bases, `StatelessMutator` and `SingleCandidateMutator`, each with several generic arities. The inheritance dimensions encode conveniences rather than distinct domain behaviors.

Prefer one clear construction contract, one small runtime contract for each meaningful operation, and a narrowly justified leaf convenience. Scalar-to-batch adaptation and scheduling can be helpers or explicit composition. Symmetry across every arity is not itself a product requirement.

Binding the problem and search space into executor fields could reduce generic and argument plumbing further, but is a separate design option. Some components deliberately operate on multiple problems or changing problem instances. Do not introduce a runtime context bag or bind everything prematurely merely to shorten signatures.

### Keep validity; make inference optional

Search-space membership and operator preconditions are fundamental. The general invariant implication, tri-state guarantee and contract-composition system is optional automation around those facts.

`SearchSpaceCompatibility.Check` accepts undeclared contracts and skips unknown output claims. It is useful declaration checking, not a proof that arbitrary operators preserve validity.

Keep `Contains`, early component-specific checks and verification utilities. Consider moving reusable invariant inference out of the central authoring path. The tradeoff is less automatic compatibility inference, not abandoning validity. A fixed-cardinality bit-vector space still needs bit-swap variation or another valid operation; ordinary bit flips remain inappropriate.

### Treat clocks as sampled analysis data

Counters and durations are useful ordinary objects. A convergence sample can carry best objective, evaluation count, active duration and domain epoch as fields. It does not inherently need a general time/moment ontology.

Custom coordinates must remain possible; simplifying clocks must not close analysis to built-in counts and durations. Retention is a storage policy, and best-so-far is a fold with per-run mutable state. Neither needs to define the execution kernel.

The current `Moment.Read` creates an immutable dictionary of clock keys and boxed values for every stored or replaced sample. Bind a fixed column layout once and write direct readings into slots or typed columns; perform clock-to-column lookup during query. This removes per-sample dictionary construction without claiming an unmeasured speedup.

### Experiments are recipes plus scheduling of ordinary runs

Preserve ordered trial descriptions, stable trial identity, deterministic random forks, bounded concurrency, independent mutable state and successful results after sibling failures. Grids and repetitions can be transformations that produce trial descriptions. They need not introduce another optimization execution model.

Current `GridExperiment` uses the algorithm value as a trial key and rejects equal configurations. Prefer explicit trial identity; equality can support diagnostics or intentional deduplication. Two equal configurations are legitimate independent trials.

Do not merge experiment and algorithm lifecycle state machines merely because both have state names. Pausing one retained iterator differs from coordinating independent tasks. Their shared vocabulary does not establish a shared host responsibility.

### Separate persistence from execution restrictions

Records make configuration data-like, but do not by themselves establish serialization. A persistence contract still needs polymorphic type identification, required-value construction, versioning and preservation or rebinding of graph references.

There are two different equalities: value equality of settings and reference identity of deliberately shared nodes or observation targets. Deserializing two equal copies where one reference was shared can change behavior. Likewise, a `with` copy changes root identity and can leave a reference-selected trace pointing at the old root. The backlog already records unmatched observation sources as a problem.

Prefer explicit composed observation locations in the small core. If automatic reference-based selection remains, configuration rewriting and persistence must either rebind targets coherently or diagnose stale selections. Global IDs on every configuration are not an automatic solution.

The completed branch temporarily permits delegates in attached trace definitions, so some otherwise immutable algorithm configurations cannot be persisted. Recommend a clear boundary between persistable optimization/analysis descriptions and runtime callbacks supplied by an embedding application. This may justify a narrow runtime attachment input; it does not justify reviving the removed mutable setup/resource-supply framework wholesale.

Also challenge the experiment proposal's ban on reflection during parameter discovery. Reflection or serializer metadata during REST loading and grid expansion does not violate direct typed execution. Share the serializer's model where possible; use generated metadata when an actual AOT requirement warrants it. Do not force every operator author to maintain a parallel handwritten property schema solely to keep a cold path reflection-free.

## Performance boundary and unavoidable limits

Normal operator calls already use retained typed references. This analysis found no basis for claiming they perform resolver lookup or reflection per candidate.

There are two narrower exceptions:

- `CycleAlgorithmExecutor` creates child scopes and resolves algorithms during each turn. Those are activation-time construction operations inside a running outer algorithm.
- Trace samples currently construct keyed clock dictionaries. That affects observed execution, not the unobserved operator path.

If “no lookup during the run” includes known activation boundaries, prepare typed activation factories before execution. If it includes an outer meta-optimizer discovering an entirely new configuration, some construction must still occur at that new inner run's preparation boundary. Completely prohibiting all new binding after the outermost start excludes that dynamic capability.

The useful rule is: **no framework dispatch or dependency discovery per operation; prepared construction for known activations; explicit preparation of genuinely new inner runs.** Intentional caches, problem data indexes and user analysis dictionaries remain legitimate computations.

Concurrency also has a real cost. Shared mutable executors require appropriate synchronization or a documented restriction on concurrent use. Synchronization alone does not make order-sensitive adaptation or RNG draws deterministic. Partition random streams by logical work identity and preserve required result ordering. Do not make the resolver responsible for proving thread safety of arbitrary consumer objects.

Budget semantics need similar precision. The current budgets check after a yielded state and may overshoot. A hard cap on expensive evaluations belongs at the evaluation boundary, with a policy for partial batches and failures. Removing AOP does not remove the difference between a stopping criterion and a hard admission limit.

## SOLID and KISS applied to this code

| Principle             | Concrete implication                                                                                                                                                                                    |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Single responsibility | A builder constructs dependencies; a collector records; an attachment selects placement. Their lifetimes can be coordinated without combining all three public authoring protocols.                     |
| Open/closed           | Consumers extend through small contracts. Universally discovering every capability is not required for extensibility. Optional advanced protocols must remain equally available to consumers.           |
| Liskov substitution   | A decorator preserves the contract it exposes. It should not imply a mutation-strength control remains meaningful after arbitrary routing or composition.                                               |
| Interface segregation | A simple custom operation should not have to implement routing, generic wrapper dispatch, instrumentation creation and capability discovery merely to execute.                                          |
| Dependency inversion  | Algorithms depend on typed operation contracts; optimization does not depend on trace retention, parameter grids or plotting coordinates.                                                               |
| KISS                  | Prefer stable object references and visible composition over resolver rules that depend on construction order. Accept bounded repetition when generalization creates a larger protocol than it removes. |

SOLID does not prescribe the maximum possible interface decomposition. More interfaces can increase the number of obligations users must understand. Microsoft's framework guidance explicitly warns that excessive abstractions harm usability and recommends validating abstractions with several concrete implementations and consumers. [Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/abstractions-abstract-types-and-interfaces).

## What the comparison libraries teach

Scikit-learn's useful lesson is consistent contracts, inspectable parameters, composition and few mandatory extension methods. Its estimator conventions and cloning separate configuration parameters from fitted state conceptually, although they live in one object rather than HeuristicLib's two graphs. Preserve HeuristicLib's explicit split rather than copying that storage model. [Estimator development guide](https://scikit-learn.org/stable/developers/develop.html), [API design paper](https://arxiv.org/abs/1309.0238).

Scikit-learn also has metadata routing with consumer requests, routers and method-specific policies. That illustrates how cross-cutting dependency propagation can grow another system even around a simple estimator model. The lesson is to bound such machinery and keep it unnecessary for the ordinary path. [Metadata routing](https://scikit-learn.org/stable/metadata_routing.html).

Pymoo presents optimization through problem, algorithm, termination and result, with callback and ask/tell facilities as additional control. Its user-facing organization is useful inspiration for a short entry path; it is not evidence that nested C# component composition can satisfy every current HeuristicLib guarantee without supporting machinery. [Pymoo overview](https://www.pymoo.org/).

## Meaningful alternatives

| Alternative                                                                                | What it changes                                                                                                           | Assessment                                                                                    |
| ------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| Finish the branch, reorganize documentation, hide internals                                | Smaller learning path; preserves current guarantees and interactions                                                      | Useful immediately, insufficient for the requested drastic simplification                     |
| Keep typed composition; narrow the kernel; fixed preparation and explicit local decorators | Removes construction-time policy mutation and broad transparent-capability promises; reduces mandatory extension services | Recommended direction; requires deliberate behavior changes and a representative pilot        |
| Replace domain operations with one universal invocation/graph engine                       | Potentially one wrapping protocol, but weaker named APIs and another argument/dataflow model                              | Poor fit for stated goals and repository evidence; do not repeat the failed bridging approach |

The recommended alternative does not require deleting every operation descriptor immediately. Resolve the ownership and contextual-decoration knot first. Then use actual authoring examples to decide whether the remaining descriptor protocol earns its maintenance cost.

Keep prior rejections of mutable live resources in reusable records, merged configuration/runtime objects, Rx as a second event system, universal context bags, automatic source generation as a substitute for design, and a generic configurable run host. Those approaches either contradict the requested separation or relocate complexity.

## How to establish that the replacement is simpler

Before migrating the library, build one isolated design pilot covering:

1. A GA and a hill climber sharing the same problem and ordinary operators.
2. Alternating child algorithms with fresh local budgets and one retained cache.
3. Explicit and automatic observation at equivalent boundaries.
4. One run-wide trace across fresh child activations and two traces sharing a counter.
5. An adaptive mutator under timing and under a multi-child choice.
6. A consumer-defined operation and a consumer-defined collector.
7. A failed retry attempt, a successful retry, cancellation and an algorithm call yielding no states.
8. Independent concurrent trials with one reused immutable analysis definition.
9. Configuration round-trip and grid replacement preserving intentional sharing and observation placement.

Judge the pilot by the explanations and obligations it removes: no construction-order-dependent advice, no self-adder exception, no private scope solely to accommodate advice mutation, no mandatory arbitrary control forwarding, and no collector author required to write aspect factories. A shorter glossary alone is insufficient.

Benchmark representative light operators with no instrumentation and with counting, observation and retry; measure allocations separately. Measure preparation and fresh activation costs separately from operation calls. Do not transfer an old pass-through result to a new architecture without measuring it.

Keep the configuration/executor split and domain calls stable during the experiment. A wholesale rewrite before proving the difficult composition cases would risk producing another large mechanism built around different names.

## Decisions before implementation

These are proposals, not silently accepted requirements:

1. **Shared-node semantics:** may a shared executor have stable children and node-level instrumentation, with caller-specific behavior expressed at that use? Recommendation: yes. Give up arbitrary contextual penetration into shared composites.
2. **Extension guarantee:** must every custom operation automatically support all generic retry/routing/instrumentation/control services? Recommendation: no. Ordinary typed participation is basic; universal services are explicit opt-in with the same protocol available to built-in and consumer code.
3. **Persistence boundary:** can runtime callbacks live outside the persisted configuration description? Recommendation: yes. Persist data descriptions, bind runtime integration at preparation, and permit cold-path metadata/reflection.

After those decisions, settle activation and budget preparation against the pilot. Binding all problem dependencies into executors, replacing every generic arity and choosing a new public authoring API are downstream decisions; they should not be guessed first.
