# Core simplification analysis

Status: investigation requested by the developer on 2026-11-04. It assumes that the operator unification work on this branch is complete, including steps 1 to 4 of the [lifetime plan's implementation sequence](operator-state-lifetimes.md#implementation-sequence-confirmed-on-2026-11-04). Nothing in this document is accepted. Every change it proposes is architectural in the sense of `AGENTS.md` and needs explicit acceptance before implementation. It reopens several approaches recorded under "Discussed, tried, and rejected"; [section 9](#9-relation-to-accepted-decisions-and-recorded-rejections) lists them with the reason each rejection no longer applies.

Step 2 of that sequence, instruments and observation, was committed while this analysis was written. The analysis uses its names: an instrument is the definition a run creates one result for, such as a counter, duration or trace. The configuration introspection proposed here is the same contract as the parameter metadata of [serializable parameter grids](configuration-parameters-and-experiment-persistence.md), which needs it for experiments. [Section 7](#7-inspiration-from-other-libraries) compares 31 libraries in three groups: optimization and machine learning frameworks, metaheuristic frameworks, and structurally similar libraries outside the domain.

The question asked: what are the core concepts and design principles of HeuristicLib in its fundamental form, which internal systems are really core, and which should be removed, replaced or drastically simplified, so that corner cases are handled by the fundamental system instead of by new mechanisms.

## 1. Summary

The user-facing domain model is already small and sound. [Core concepts](../docs/guide/fundamentals/core-concepts.md) explains a run with six parts, and that page does not need to change. The concept growth is concentrated in the execution and analysis layers: 43 of the 90 glossary entries describe execution or analysis machinery, compared with 12 for the optimization domain.

The growth has a small number of root causes. The largest is that HeuristicLib has two ways to compose behavior: explicit configuration nodes, and runtime interception through pointcuts, advice and aspects woven during resolution. The second makes the execution graph depend on more than the configuration graph: on scope contents, attach order, deduplication and the requesting context. Nearly every rule in the current plans exists to make that extra input deterministic: ordering, duplicate removal, the building layer rule, advice belonging to the scope, the already-built and child-scope guards, failure ending the scope tree, one aspect per instrument per run, the exception that advice a configuration's own construction adds never applies to that configuration, and the manual/automatic equivalence contract. The accepted [aspects contract](aspects-as-automatic-composition.md) already says that every aspect behavior must be expressible as explicit composition. Once that holds, interception is a second, far more complex implementation of something the first mechanism already does.

The proposed fundamental design keeps the configuration/executor split, the scope as sharing unit, child scopes for meta-algorithm activations, `Sharing`, run-bound results, the operation contracts and the invocation wrapper backbone. It removes interception entirely and states the execution model as seven rules:

1. A configuration is immutable, introspectable data.
2. Building maps each configuration node to one executor within one build. Executors hold direct references and look nothing up while they run.
3. A meta-algorithm builds each child activation as a child build. `Sharing` on a node builds the listed configurations once for that node's lifetime.
4. Behavior is composed only by nodes: wrappers, composites and meta-algorithms.
5. Placing many nodes automatically is a deterministic configuration rewrite: the node that holds the intent expands its child when built. It is not a runtime mechanism.
6. Build-time lookups of shared executors and results resolve to the nearest enclosing provider; the run is the root provider of results.
7. Executors are bound to their problem when they are built, so operation methods take only their own inputs and a random number generator.

Under these rules the scenarios of the [system spec](operator-unification-system-spec.md) follow without dedicated mechanisms ([section 4.4](#44-the-system-spec-scenarios-under-these-rules)). The execution and analysis vocabulary shrinks from 43 glossary entries to about 12, and roughly half of the per-role infrastructure disappears.

## 2. What HeuristicLib fundamentally is

### 2.1 The domain core

These concepts are the reason the library exists. They are well chosen, mostly independent of the execution machinery and should stay as they are:

| Concept                                                | Responsibility                                                 |
| ------------------------------------------------------ | -------------------------------------------------------------- |
| Problem                                                | Evaluates candidates and states its objective and search space |
| Search space                                           | Decides which candidates are valid and states their invariants |
| Candidate                                              | An immutable proposed solution                                 |
| Objective, objective vector, evaluated candidate       | What is better, and a candidate together with its scores       |
| Search state                                           | The public, immutable progress value an algorithm yields       |
| Random number generator                                | The explicit, forkable source of randomness                    |
| Operator roles: creator, evaluator, selector and so on | The named kinds of work an algorithm delegates                 |
| Algorithm                                              | A search process that yields a stream of search states         |

### 2.2 The composition core

The defining idea of HeuristicLib, compared with fixed solver collections, is composition: algorithms are assembled from operators, operators from other operators, and algorithms from other algorithms, and every assembled thing can be used where its parts could. Three ideas carry that:

1. **Configuration graph and executor graph.** A configuration is reusable, comparable, persistable data. An executor is the plain object built from it for one run, with direct references to its children and private fields for its mutable data. The developer has confirmed this split repeatedly, and it is the property that lets one configuration produce independent runs, grids and repetitions.
2. **Operations as typed contracts.** A slot such as `IOperator<Mutation, TCandidate>` names the operation it needs. The executor interface has one named, strongly typed method. `IOperation<TSelf>` and `IInvocationWrapper` let one generic wrapper such as retry, counting or per-invocation choice serve every operation without a per-role implementation, with no delegates on the hot path.
3. **Wrapping.** Behavior around an operation or algorithm call is added by a node that holds a child and decorates its executor. Caching, limits, retry, choice and budgets already work this way.

### 2.3 The scikit-learn comparison

scikit-learn is explainable with an estimator, its roles and a handful of rules. HeuristicLib maps onto the same shape almost one to one, which shows that its fundamental model is not inherently larger:

| scikit-learn                                        | HeuristicLib                                                      |
| --------------------------------------------------- | ----------------------------------------------------------------- |
| Estimator parameters stored unchanged by `__init__` | Configuration record with `init` properties                       |
| `fit` creates fitted attributes ending in `_`       | Building creates an executor with private fields                  |
| Classifier, regressor and transformer interfaces    | Operations such as `Mutation` and `Evaluation`                    |
| `Pipeline`, meta-estimators such as `GridSearchCV`  | Wrappers, composites and meta-algorithms                          |
| `get_params`, `set_params`, `clone`                 | Missing: there is no generic access to a configuration's children |
| `verbose`, `warm_start`, experimental callbacks     | Run-level observation of the state stream                         |
| Rules: parameters unchanged, no logic in `__init__` | Rules: configurations are data, mutable data lives in executors   |

Two differences matter. scikit-learn has no general runtime interception: everything that changes behavior is a parameter or a composed estimator, and its callbacks, experimental since version 1.9, are fixed begin and end hooks kept out of the parameters. And its generic parameter access is a core contract on which cloning, grid search, printing and persistence all build. HeuristicLib lacks that contract and has compensated with runtime machinery ([root cause 5](#rc5-no-generic-access-to-the-configuration-graph)).

## 3. Diagnosis

### 3.1 Measurements

| Indicator                                                                                 | Value                                              |
| ----------------------------------------------------------------------------------------- | -------------------------------------------------- |
| Glossary entries in "Execution Model" and "Analysis and Experiments"                      | 43 of 90                                           |
| Glossary entries for the optimization domain                                              | 12                                                 |
| Lines in `plans/*.md`                                                                     | about 6,000                                        |
| Lines in the four plans of this branch                                                    | about 3,000                                        |
| Entries under "Discussed, tried, and rejected" in the backlog, mostly execution           | about 45                                           |
| Decision rounds on scopes, advice and results recorded on 2026-11-04 alone                | 10                                                 |
| Per-role infrastructure: executor interface, operation, observation, advice, bases        | about 3,300 lines for 9 roles                      |
| Test files exercising pointcuts, aspects, advice, scopes, sharing, handles or instruments | 98 of 323                                          |
| Public types in the core library                                                          | 849                                                |
| Production nodes that call `CreateChildScope` or `AddAspect`                              | 6: Cycle, Pipeline, two budgets, `Advised`, racing |
| Production configurations implementing two operations                                     | 0                                                  |
| Production uses of `Pointcut.Predicate` and `Pointcut.Named`                              | 0                                                  |

The interception and scope machinery is exercised mostly by tests and serves six production nodes, apart from run setup, which step 3 removes. Several capabilities that cost rules and code have no production user at all.

Step 2 shows the trend continuing while the plan converges. To replace resource handles and observation points it added `Instrument` and `Instrument<TResult>`, `RunResults`, `IClock` and `Clock<TTime, TResult>`, `Counter`, `OperatorCounter`, `AlgorithmCounter`, `Duration`, `OperatorDuration`, `InvocationSource`, `AlgorithmSource`, `IObservationInstrument`, `AlgorithmCallObservation` with `AlgorithmCallEvent`, `RetentionDecider`, `AdvisedAlgorithm` and `ObservedAlgorithm`, plus a rule exempting a configuration from advice its own construction adds, and the root elapsed-time wrapper needed a follow-up commit to forward executor controls. Each addition is well reasoned locally; together they replace a handful of removed types with more.

### 3.2 The growth pattern

Each corner case was solved by a mechanism, and each mechanism interacted with the earlier ones, which produced rules. The table traces the chain for the cases recorded in the plans of this branch:

| Corner case                                                   | Mechanism introduced                                  | Rules and concepts it produced                                                                                                                      |
| ------------------------------------------------------------- | ----------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Count or observe an operator deep inside a graph              | Pointcuts, advice and aspects woven during resolution | Ordering rule, duplicate removal by reference, matching before deduplication, `Check` before `Wrap`, nine role advice bases, consumer-role advice   |
| A budget counts only calls inside one child algorithm         | Advice added to a scope layer, later to the scope     | Building layer rule, then advice belonging to the scope, the already-built guard, the child-scope guard                                             |
| A terminator reads a counter that advice writes               | Resource handles and run setup                        | Supplied handles, `RunResources`, `AddResource`, per-trial supply; replaced by instruments, run-bound results and one aspect per instrument per run |
| A cache survives Cycle turns                                  | `ReuseExecutors`, then `Sharing` layers               | Nearest sharing wins, shared executors built in their own child scope, share-before-resolve guard, one executor type per entry                      |
| One reference in two slots should be one executor             | Default sharing within a scope                        | Advice context conflict error, then advice belonging to the scope to remove it                                                                      |
| Observe Cycle turns and cycles                                | Observation points, then interceptor slots            | Weak identity table, `with` copies with new points; both removed in favor of an algorithm-call source                                               |
| Two traces share a clock that must tick once                  | Clocks as aspects, advice deduplication               | Clock advice ordered before observations, closest duplicate kept                                                                                    |
| A construction fails half way through a budget                | Scope state that advice can leave behind              | Failure ends the scope tree, rejections before construction stay retryable, `TryResolve` semantics                                                  |
| A configuration performs two operations                       | Multi-operation configurations                        | One construction hook, reflection over interface maps to reject a second hook                                                                       |
| Evolution strategies adapt mutation strength through wrappers | Executor controls                                     | `IExecutorControlProvider`, forwarding rules in every wrapper, controls that choices and routers must hide                                          |

The pattern is consistent: the developer's governing goal, "complex behavior composed from a small set of consistent rules", is stated at the top of the system spec, but each round answers a scenario by adding or refining a rule. No round removed a mechanism that the scenario showed to be redundant.

### 3.3 Root causes

#### RC1. Two composition mechanisms, one of them runtime interception

Configuration nodes compose behavior inside the configuration graph. Advice composes behavior outside it, at resolution time and per request. The executor graph is therefore a function of the configuration graph plus the scope's aspect list, its attach order, the duplicate rules and the context of each request. Every rule in the first two rows of the table above exists to make that extra input deterministic.

The library's own principles argue against the second mechanism. The [design goals](../docs/contributing/design-goals.md) ask for "plain C# composition over framework magic", "a small number of strong concepts instead of many overlapping abstractions" and the avoidance of "hidden orchestration and opaque indirection". The [requirements](../docs/contributing/requirements.md) say that heavy runtime machinery needs concrete justification. Aspect-oriented interception is the textbook example of hidden orchestration. Of the 31 libraries compared in [section 7](#7-inspiration-from-other-libraries), only MOEA Framework attaches observers through a pointcut-like mechanism, a reflective walk that needs package exclusions, type skips and a guard against instrumenting itself.

The decisive argument is internal. The accepted [aspects contract](aspects-as-automatic-composition.md#governing-contract) requires every aspect behavior to be expressible through explicit composition with equivalent behavior and lifetime, and its acceptance scenarios demand evidence that both paths agree. When one path must be able to express everything the other does, the other is redundant. It can become a function that produces the explicit composition, which needs no equivalence evidence because there is only one path.

#### RC2. Identity is spread over many registries

Configurations are records with value equality, but behavior is keyed by reference in six places: the scope table, the sharing set, advice deduplication, the observation point weak table, the resource table and the result table. Users must keep variables for `evaluator.Cached()` and `evaluator.Invocations.Count()`, record copies change identity, and several rules exist only to keep the registries consistent with each other.

Reference identity of configuration nodes is inherent to a configuration graph that allows sharing, and serializers can preserve it with reference metadata. The problem is not identity itself but the number of independent tables keyed by it. The proposal keeps two: the build table and the result table.

Reference identity also does not survive serialization by itself. Keras stores a shared layer once by name and refers to its calls by index, and ECJ marks a shared component explicitly; Hydra has left shared object identity open for years ([section 7.4](#74-lessons), lesson 6). A persisted configuration therefore needs explicit identities for shared nodes and instruments, decided together with the serialization format.

#### RC3. Lifetime decided by the resolver instead of by structure

Because advice is applied per request, the same configuration node can be requested from contexts with different advice. The resolver then needs a policy for whether the request gets the same executor, which produced fresh-by-default, then default sharing, then the building layer rule, then advice belonging to the scope. When wrappers are nodes, a node inside a budget and the same node outside it are simply different nodes in the graph: the inside one is the counting wrapper, whose child is the shared node. The executor graph is isomorphic to the configuration graph and no policy is needed.

#### RC4. Run types threaded through every call

Every operation method receives the random number generator, the search space and the problem, and every executor interface is generic in `TSearchSpace` and `TProblem`, although each executor graph is built for exactly one problem: every call site in the library passes the problem the executor was built for. The cost is large:

- Three arity levels for every authoring base: `Mutator<TCandidate>`, `Mutator<TCandidate, TSearchSpace>` and `Mutator<TCandidate, TSearchSpace, TProblem>`, the same for stateless and single-candidate bases and for executor bases.
- Three typed scope views and three resolution extension blocks per role with four methods each.
- `ExecutionSignature`, `Fits` and `SupportedExecutionDescription` on every configuration, with each algorithm listing its children by hand: `execution.Fits(Creator, Crossover, Mutator, Terminator, Evaluator, Refiner, Selector)`.
- Bound algorithm bases that convert executors between the declared and the run's types.

The compile-time safety this buys is smaller than it appears. Configurations are agnostic, so whether a configuration graph fits a problem is decided when it is built, not by the compiler. The static types protect only the inside of each executor, which a single cast at build time protects equally well.

#### RC5. No generic access to the configuration graph

There is no contract to enumerate a configuration's parameters and children or to copy it with children replaced. Everything that must reach every node is therefore implemented either at runtime, through interception during resolution, by hand in every node, as `Fits` is, or by a private reflection walk, as `SearchConfigurationValidation` does. The same contract is a prerequisite for the developer's serialization goal, for parameter grids by path, for printing and diffing configurations and for configuration rewriting. scikit-learn's `get_params` and `set_params` play exactly this role.

The [serializable parameter grids](configuration-parameters-and-experiment-persistence.md) proposal already identifies this need for experiments and proposes declared parameter metadata with typed read and immutable replacement. This analysis argues that the same metadata is a core contract, not an experiment feature: it should also serve validation, rewriting and printing, and replace both `Fits` lists and the reflection walk.

#### RC6. Mechanism-first decisions

The plans of this branch decide scenarios one at a time with precise mechanism-level rules, which is careful work, but rarely ask whether a scenario could be expressed with an existing mechanism if another one were removed. The developer guidelines already require comparing "the smallest change within the existing design and the simplest coherent design if it differs" (§ 2.1). In practice the smallest change has won every round, and the simplest coherent design was never written down. [Section 6](#6-principles-that-keep-the-core-small) proposes governance that makes the second comparison concrete.

## 4. Proposed fundamental design

### 4.1 The seven rules

**Rule 1. Configurations are immutable, introspectable data.** A configuration holds parameters and child configurations, compares by value and holds no live mutable objects. Every configuration exposes its parameters and children through one generic contract, the HeuristicLib counterpart of `get_params`: the parameter metadata of the [parameter grid proposal](configuration-parameters-and-experiment-persistence.md#runtime-parameter-metadata). Whether it is declared or derived is that proposal's open decision; either way it runs at configuration time only, never during execution.

**Rule 2. A build maps each configuration node to one executor.** Building a configuration graph produces an executor graph with one executor per configuration node, so a reference used in two slots is one executor, as default sharing already decided. This follows Keras, where calling one layer twice shares it; most other surveyed libraries build fresh unless sharing is declared, which [section 8.2](#82-risks-and-open-questions) records as an open tension. Executors hold direct references to their children. Nothing is looked up, reflected or dispatched by key while they run.

**Rule 3. Meta-algorithms build child activations as child builds.** Cycle turns, Pipeline stages, racing contenders, restarts and meta-optimization evaluations each build their child configuration in a child build, which has its own table. `Sharing` on a node lists configurations that are built once in that node's build and used by its child builds. This is the accepted scope model without its advice.

**Rule 4. Behavior is composed only by nodes.** Anything that changes or observes execution is a node in the configuration graph: a wrapper around one child, a composite over several children or a meta-algorithm. No other mechanism changes an executor after it is built. Counting, timing and observation are generic wrappers built once over `IOperation<TSelf>.Wrap`, as the accepted `Counted`, `Timed` and `Observed` wrappers already are.

**Rule 5. Automation is configuration rewriting of stored intent.** A convenience that places many nodes is a node that stores the user's intent and, when it is built, expands its child by a pure, deterministic rewrite into ordinary wrapper nodes. `algorithm.LimitedToInvocations(mutator, 5)` keeps today's shape, a budget with a child algorithm, a target and a limit; when built, it builds a copy of its child in which every use of `mutator` is wrapped by a counting node. The rewrite keeps shared references shared: the same old node maps to the same new node. The persisted configuration holds the intent, as Gymnasium records the wrappers `make()` inserts as fields of its specification, and an `Expand()` view shows the effective graph for printing, comparison and tests. Deferred construction needs no special handling, because a Cycle turn builds the budget, which expands its own child. The same rewrite function is available to users who prefer to place the wrappers at configuration time.

**Rule 6. Build-time lookups resolve to the nearest provider.** Two things are looked up while building: the executor of a shared configuration and the result for an instrument, such as a counter or trace. Both resolve to the nearest enclosing node that provides them, otherwise to the run, which is the root provider of results. A budget provides its private counter for its own subtree; `Sharing` provides shared executors for its subtree. The executor keeps the direct reference it receives.

**Rule 7. Executors are bound to their problem.** A build knows its problem, so executors receive the problem and search space when they are built. Operation methods take only their own inputs and the random number generator, for example `Mutate(parents, random)`. A composite that runs children over another problem or search space, such as a composite encoding or a meta-optimization, builds them in a child build for that problem.

Rules 2, 3 and 6 replace the current scope rules. Rule 4 and rule 5 replace pointcuts, advice, aspects, observation sources as aspect factories, run setup and per-trial aspects. Rule 7 replaces the run-type plumbing.

### 4.2 Concepts after the change

For users:

| Concept               | Meaning                                                                        |
| --------------------- | ------------------------------------------------------------------------------ |
| Configuration         | An algorithm or operator as immutable data                                     |
| Wrapper               | A configuration that decorates one child, such as `Cached`, `Counted`, `Retry` |
| Meta-algorithm        | An algorithm that runs child algorithms, such as Cycle or Pipeline             |
| Shared configuration  | A configuration built once for a node's lifetime through `Sharing`             |
| Run                   | One execution of an algorithm on a problem, with lifecycle and results         |
| Instrument and result | What to collect, such as a counter or trace, and what one run collected for it |
| Observation           | The record of one operation or algorithm call that `Observed` passes on        |
| Experiment and trial  | Independent runs over configurations, problems and seeds                       |

For authors of operators and algorithms:

| Concept            | Meaning                                                                      |
| ------------------ | ---------------------------------------------------------------------------- |
| Executor           | The runtime object a configuration builds, with private fields               |
| Build context      | What a construction hook receives: the problem, child building and providers |
| Operation          | The typed contract of one role, with its static wrapping members             |
| Invocation wrapper | A struct that adds behavior around every call of any operation               |

That is twelve concepts, compared with 43 glossary entries today. Pointcut, advice, join point, aspect, wrapper chain, resolution scope layers, resource, resource handle, run resources, observation source, observation point, clock as a separate kind, moment, trial analyzer, executor controls and execution signature leave the vocabulary.

### 4.3 Core internal systems

| System                                 | Status in the proposal | Why it is core                                                                     |
| -------------------------------------- | ---------------------- | ---------------------------------------------------------------------------------- |
| Configuration introspection            | New                    | Serialization, rewriting, generic validation, grids by path, printing              |
| Build context with table and providers | Simplified scope       | Rules 2, 3 and 6, the only place executors are constructed                         |
| Operation contracts and wrapping       | Kept                   | Typed slots, generic wrappers for all roles, no delegates on the hot path          |
| Run lifecycle and result table         | Kept                   | Streaming, pause and resume, cancellation, failure, results readable after failure |
| Random number generator forking        | Kept                   | Determinism independent of scheduling                                              |
| Batch execution and concurrency        | Kept                   | Single-candidate authoring and parallel evaluation                                 |
| Configuration rewriting                | New, small library     | Rule 5; a memoized map over the graph, applied by intent nodes when built          |

### 4.4 The system spec scenarios under these rules

| Scenario                                | How it is expressed                                                                                                                                                                  | Result                         |
| --------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------ |
| S1 private accounting                   | `algorithmA.LimitedToInvocations(mutator, 5)` is a budget that, when built, expands `A` with `mutator` wrapped by a counting node and provides a new counter for that subtree        | C1 and C2 are independent      |
| S2 sharing within a run                 | `mutator.Counted(calls)` in the slot and a terminator reading `calls`; nothing provides `calls`, so both receive the run's result                                                    | One counter, no setup          |
| S3 independent runs                     | Results belong to the run                                                                                                                                                            | 3 and 0                        |
| S5 simple cases                         | Unchanged one-line call                                                                                                                                                              | Unchanged                      |
| S6 budget placement                     | A budget expands only its own child; a budget inside a Cycle turn is rebuilt every turn and provides a new counter; nested budgets produce nested counting nodes on the same mutator | Same tables as measured        |
| S7 executor reuse                       | `cycle.Sharing(cached)`, unchanged                                                                                                                                                   | Unchanged                      |
| S8 run-level trace                      | `cycle.Traced(trace)` stores the trace and, when built, wraps every use of the mutator with an observing wrapper writing the run's result                                            | One trace of 10 samples        |
| S9 when budgets stop                    | Unchanged: a budget checks between states                                                                                                                                            | Unchanged                      |
| Q2 observe only A's calls               | `algorithmA.Traced(trace)` inside the Cycle, or `mutator.Observed(trace)` in A's slot                                                                                                | 3                              |
| Q3 is a wrapper counted as an algorithm | No runtime pointcut exists. Configuration-time selection sees the nodes the graph contains                                                                                           | Question dissolves             |
| Q4 attaching counting twice             | Explicit placement does what is written. Rewrite helpers skip a node already wrapped for the same instrument                                                                         | 3, no surprise case 3          |
| Q5 analysis inside a Cycle child        | Results belong to the run                                                                                                                                                            | One recording of 6 samples     |
| Q6 failures                             | A failed build propagates and is discarded; no executor can carry part of its advice because nothing is applied after construction                                                   | Run `Failed`, results readable |
| Q7 one live object across runs          | Deferred as decided. A caller callback through a runtime-only `Observed` copy covers "one sink for many runs"                                                                        | Unchanged                      |
| Advice context conflict                 | Inside the budget the slot holds the counting node, outside it holds the original; both share the original's executor                                                                | No error needed                |
| Clock shared by two traces              | A clock is a counter result written by one counting node                                                                                                                             | Ticks once, no deduplication   |

One case behaves differently from today and must be stated. An expansion below a `Sharing` node that lists a configuration containing the rewritten target produces a new configuration, which is no longer the shared one. For `Cycle(a.LimitedToInvocations(evaluator, 5), b).Sharing(cached)`, where `cached` wraps `evaluator`, A's turns get their own cache instead of the shared one. Today the same configuration silently counts nothing. Both outcomes resolve a genuine contradiction, counting per turn inside an object shared across turns. The expansion's outcome is visible through `Expand()` and can be reported at build time, because the `Sharing` node lists a configuration that its subtree never requests.

### 4.5 Representative usage

**Proposed syntax, not implemented.** User code barely changes; the machinery behind it does.

```csharp
// Unchanged entry points
var limited = ga.LimitedToInvocations(ga.Mutator, 5);
var final = limited.CreateRun(problem, random).Complete();

// A run-level counter read by a terminator, no handles and no setup
var evaluations = new Counter();
var counted = ga with { Evaluator = ga.Evaluator.Counted(evaluations) };
var run = counted.TerminatedBy(Terminate.AfterCount(evaluations, 10_000)).CreateRun(problem, random);
run.Complete();
var used = run.Result(evaluations).Value;

// Tracing stores the instrument and expands its child when built; `counted` itself stays persistable
var offspring = ga.Crossover.Invocations.Trace(observation => observation.Offspring.Count, clocks: [evaluations]);
var traced = counted.Traced(offspring); // today: counted.Advised(offspring)
var tracedRun = traced.CreateRun(problem, random);
tracedRun.Complete();
var trace = tracedRun.Result(offspring);
```

Operator authoring loses the run types:

```csharp
// Today
public interface IMutationExecutor<TCandidate, in TSearchSpace, in TProblem> : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

// Proposed
public interface IMutationExecutor<TCandidate> : IExecutor
{
    IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);
}

public sealed record GaussianMutator(double Sigma) : Mutator<RealVector, BoundedRealVectorSearchSpace>
{
    // The base checks the run's search space once and passes it typed.
    protected override IMutationExecutor<RealVector> CreateExecutor(BoundedRealVectorSearchSpace searchSpace, BuildContext context) =>
        new Executor(Sigma, searchSpace);
}
```

A budget author expands the stored intent and makes one provider call instead of adding an aspect and following scope rules:

```csharp
protected override IAlgorithmExecutor<TSearchState> CreateExecutor(BuildContext<TSearchState> context)
{
    var calls = new Counter();                                                        // an instrument private to this build
    var counter = new CountAccumulator();
    var expanded = Algorithm.Replace(ObservedOperator, target => target.Counted(calls)); // expands the stored intent
    var child = context.Providing(calls, counter).Build(expanded);
    return new Executor(child, counter, MaximumCount);
}
```

## 5. System-by-system verdict

| System                                                                                             | Verdict               | Rationale                                                                                                                                                           |
| -------------------------------------------------------------------------------------------------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Problem, search space, candidate, objective, search state, random number generator                 | Keep                  | Domain core                                                                                                                                                         |
| Configuration records, `ValueArray`, `with`                                                        | Keep                  | Configuration as data                                                                                                                                               |
| Configuration introspection                                                                        | Add                   | RC5; enables serialization, rewriting, grids and generic validation                                                                                                 |
| `IOperator<TOperation, TCandidate>`, `IOperation<TSelf>`, `IInvocationWrapper`, `WrappingExecutor` | Keep                  | The generic wrapper backbone becomes the only composition mechanism                                                                                                 |
| Role executor interfaces with named methods                                                        | Simplify              | Drop `TSearchSpace` and `TProblem` and the search space and problem arguments (rule 7)                                                                              |
| Authoring bases: role, stateless and single-candidate bases in three arities                       | Simplify              | One arity; a base declares the search space or problem type it needs and receives it typed at build                                                                 |
| Executor bases in three arities                                                                    | Simplify              | One arity                                                                                                                                                           |
| `ResolutionScope` and its typed views                                                              | Replace               | Build context: table, child builds and providers. No aspect list, ordering, guards or failure state                                                                 |
| Child scopes and `Sharing`                                                                         | Keep                  | Rule 3; meta-algorithm authors decide activations                                                                                                                   |
| Pointcut, advice and its 13 bases, aspect, join point, wrapper chain                               | Remove                | RC1; replaced by wrapper nodes and rewriting                                                                                                                        |
| `Advised`, scope-level `AddAspect`, one aspect per instrument per run                              | Remove                | Rule 5; `Traced` and `LimitedToInvocations` are rewrites                                                                                                            |
| Run setup, `ResolutionScopeBuilder`, per-trial aspects                                             | Remove                | Already decided; rewriting covers attachment                                                                                                                        |
| Resource handles, `RunResources`, `AddResource`                                                    | Remove                | Already decided; providers and run results replace them                                                                                                             |
| Instruments, run-bound results, `run.Result(instrument)`                                           | Keep                  | Already decided; resolved through rule 6                                                                                                                            |
| `Counted`, `Timed`, `Observed`                                                                     | Keep                  | Generic wrappers; observation wrappers come from each operation, as the design review requires                                                                      |
| Observation records per operation                                                                  | Keep                  | Data passed by `Observed`                                                                                                                                           |
| Advice factories in `InvocationSource` and `AlgorithmSource`, observation points, Cycle records    | Simplify              | A source keeps only the factory for its observing wrapper                                                                                                           |
| `IClock`, `Clock<TTime, TResult>` and the separate clock family                                    | Simplify              | A clock is any result a trace reads at each sample; it needs no type family of its own                                                                              |
| Trace analyzer as aspect, retention objects with mutable state                                     | Simplify              | A trace is an immutable instrument; the run holds its entries and retention progress, as the design review decided                                                  |
| Accumulating analyzers                                                                             | Simplify              | A result kind whose per-run state the run creates                                                                                                                   |
| Executor controls, `IExecutorControlProvider`, `IMutationStrengthControl`                          | Replace               | Pass adaptive values as operation inputs, for example a strength-parameterized mutation, or keep them in the algorithm. Removes forwarding rules from every wrapper |
| Multi-operation configurations and the reflective single-hook check                                | Remove                | No production user; reintroduce with a concrete need                                                                                                                |
| `ExecutionSignature`, hand-written `Fits` lists                                                    | Replace               | Each node checks only itself; traversal is generic through introspection                                                                                            |
| `TryResolve` family                                                                                | Simplify              | Becomes a non-throwing fit check; with no advice, rejections are only signature mismatches                                                                          |
| Named wrappers and `INamedConfiguration`                                                           | Simplify              | Names stay as optional metadata for rewriting selections and display; they no longer need to be selectable algorithm nodes                                          |
| Algorithm bases: bound variants and the `TSelf` parameter                                          | Simplify              | Bound variants disappear with rule 7; `TSelf` only serves typed Cycle creation                                                                                      |
| Iterative algorithm base, interceptor slot                                                         | Keep                  | Small and useful                                                                                                                                                    |
| Operator recommendations, operator contracts and candidate invariants                              | Keep outside the core | Useful conveniences and validation; they should not appear in the core vocabulary                                                                                   |
| Experiments, grids, repetitions                                                                    | Simplify              | Remove setup; vary parameters by path through introspection                                                                                                         |
| Glossary legacy entries and superseded related terms                                               | Remove                | The glossary should describe the current model; history belongs in plans                                                                                            |

## 6. Principles that keep the core small

The [design goals](../docs/contributing/design-goals.md) already state the right intent. What is missing is a way to apply it when a new corner case arrives. These principles are proposed as additions to the developer guidelines.

1. **Express before building.** A new scenario is first written with existing nodes and rules. A new mechanism is considered only when that is impossible, and the proposal must show which existing mechanism it makes redundant.
2. **One composition mechanism.** Behavior is added by nodes. Conveniences generate nodes. Nothing modifies executors after construction.
3. **The executor graph is a function of the configuration graph and the problem.** Any proposal that makes it depend on something else, such as attach order, request context or global registration, needs a recorded exception.
4. **Define errors out of existence.** The current scope has guards that exist because state can be added after something was built: advice after resolution, sharing after resolution, advice after child scopes. Immutable, lexical construction removes the window in which those errors can occur. A new error rule is a signal that a mechanism has a temporal coupling to remove.
5. **Concept budget.** The execution and analysis vocabulary has a fixed budget, for example the twelve concepts above. Adding one requires removing one or an explicit decision to raise the budget.
6. **Rule of three for mechanisms.** A generic mechanism needs three production users. Interception had two budget nodes and one racing algorithm; multi-operation configurations had none.
7. **Current state in the docs, history in the plans.** The glossary and architecture pages describe what the code does. Legacy entries and superseded terms move out.

The proposal also applies the usual object-oriented principles more strictly than the current design:

| Principle             | Current tension                                                                                                    | Proposal                                                                     |
| --------------------- | ------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------- |
| Single responsibility | `ResolutionScope` constructs, memoizes, shares, weaves, orders, deduplicates, guards, locks and records failure    | The build context constructs, memoizes and provides; wrappers decorate       |
| Open/closed           | Interception was introduced to add behavior without editing graphs                                                 | Rewriting adds behavior without editing graphs and without a second path     |
| Liskov substitution   | Wrappers must forward or hide executor controls, so substituting a wrapper changes what a consumer finds           | No controls; a wrapper is substitutable through its operation contract alone |
| Interface segregation | Every executor method carries the search space and problem, even when it uses neither                              | Methods carry only their inputs                                              |
| Dependency inversion  | Advice depends on resolution internals such as `OperationInfo` and scope state                                     | Wrappers depend only on the operation contract                               |
| KISS and YAGNI        | Pointcut algebra, consumer-role advice, multi-operation hooks and supplied handles with few or no production users | Removed until a concrete need returns                                        |
| Explicit dependencies | A terminator and an aspect meet through a handle bound in run setup                                                | Both name the same instrument, visible in the configuration                  |

## 7. Inspiration from other libraries

Three surveys on 2026-11-04 compared 31 libraries in three groups: optimization and machine learning frameworks, metaheuristic frameworks close to HeuristicLib's domain, and libraries outside the domain with a similar structure, an immutable blueprint turned into a runtime graph with composable wrappers. Most project websites were unreachable from the research environment, so the facts come from sources and documentation in each project's repository. The scikit-learn paper's principles come from a secondary source. Facts that could not be verified are left out.

### 7.1 Optimization and machine learning frameworks

| Library                     | Configuration and per-run state                                                                                           | Termination and budgets                                                                                                                    | Monitoring                                                                                                                  |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------- |
| Jenetics                    | Immutable `Engine` built once; each `EvolutionStream` owns its run state; resumable from a result value                   | `stream.limit(predicate)`; stateful limits are passed as suppliers so every stream gets a new one; `ConcatEngine` nests budgets            | `peek(statistics)` over results that already carry durations and counts; one before/after interceptor pair                  |
| jMetal 6 component-based EA | Not separated; the algorithm object holds its run                                                                         | `Termination.isMet(statusMap)` over keys the loop writes                                                                                   | Observers receive the same status map                                                                                       |
| Timefold Solver             | Serializable `SolverConfig` data, a thread-safe factory, a new `Solver` per run with solver, phase and step scope objects | Terminations on the solver and on each phase; a phase checks `OR(solver, own)`; one termination class measures whichever scope it is given | Event listeners and fixed lifecycle listeners per scope; no interception                                                    |
| Optax                       | Transformations are pure `(init, update)` pairs; state is an explicit value                                               | None; every transform that needs a step count keeps its own, duplicated on purpose; wrappers nest their inner state                        | None; users read the state                                                                                                  |
| pymoo                       | The algorithm is configuration and run state at once; `minimize` deep-copies algorithm and termination per run            | Progress values; any takes the maximum, all the minimum; evaluations are counted by the evaluator                                          | A callback and a display called in a fixed order after each generation                                                      |
| pagmo2                      | Problem, algorithm, population, island, archipelago; `evolve(population)` returns a population                            | Constructor arguments per `evolve` call; longer runs call it again                                                                         | Verbosity and an algorithm-specific log                                                                                     |
| evosax                      | Strategy object, parameter value and state value, three-way                                                               | Left to the user's loop                                                                                                                    | `tell` returns metrics                                                                                                      |
| scikit-learn                | Parameters stored unchanged; fitted attributes end in `_`; `clone` rebuilds from parameters; nested `get_params`          | Ordinary parameters such as `max_iter` and `tol`                                                                                           | `verbose`; since 1.9 experimental callbacks with begin and end hooks, kept out of the parameters, in about seven estimators |
| ML.NET                      | `IEstimator.Fit` returns a separate `ITransformer`; `Append` returns a new chain                                          | Not applicable                                                                                                                             | One log event, documented as the simplest possible interception                                                             |
| Flax                        | Linen separated module configuration from variables; NNX moved state back into objects                                    | Not applicable                                                                                                                             | Not applicable                                                                                                              |

### 7.2 Metaheuristic frameworks

| Library          | Configuration and per-run state                                                                                                                    | Budgets and counting                                                                                                                                          | Monitoring and instrumentation                                                                                                                                                                                            |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ParadisEO and EO | Stateful functors wired by reference; `eoAlgoFoundryEA` builds a fresh algorithm on every call; functors are not serialized                        | `eoEvalFuncCounter` decorates the evaluator and counts real evaluations; `eoEvalContinue` holds a reference to that counter; continuators combine in a vector | `eoCheckPoint` calls statistics, updaters, monitors and continuators once per generation; no interception. The forge cache is off by default because stateful operators would be shared by accident                       |
| DEAP             | Algorithms are plain functions; run state is local variables or objects the caller passes in                                                       | Generations only; evaluation counts are computed inline and logged                                                                                            | Statistics, hall of fame and logbook are passed in and returned. `Toolbox.decorate`, the only interception, makes the toolbox unpicklable                                                                                 |
| Nevergrad        | `ConfiguredOptimizer` stores a class and its settings and creates a new optimizer per call; an optimizer is used once                              | The budget in evaluations belongs to the optimizer; a chain creates each sub-optimizer with its own budget                                                    | Two callback points, ask and tell                                                                                                                                                                                         |
| Optuna           | Study and storage own the state; samplers and pruners receive the study on every call; sampler state is not persisted, a documented resume pitfall | `MaxTrialsCallback` derives the count from the trial record                                                                                                   | One callback at the end of each trial                                                                                                                                                                                     |
| MOEA Framework   | The algorithm object is configuration and state; version 5 removed its `Executor` and `Analyzer` façade in favor of constructors                   | `MaxFunctionEvaluations` checks the difference from a baseline taken when each run starts                                                                     | The `Instrumenter` walks the algorithm's object graph by reflection and attaches collectors through composable `AttachPoint` predicates; it needs package exclusions, type skips and a guard against instrumenting itself |
| Opt4J            | Guice modules are the configuration and are saved as XML; one injector per task is the run scope; sharing is implicit through injector singletons  | An iteration counter with an injected maximum; `Control` checkpoints inside and between iterations                                                            | Listeners registered through multibinders                                                                                                                                                                                 |
| ECJ              | Parameter files; prototypes set up from parameters and cloned; `EvolutionState` holds the whole run and can be checkpointed                        | A `generations` or `evaluations` parameter; the evaluator increments the state's evaluation count                                                             | 18 fixed statistics hooks arranged in a tree, which the class documentation admits are only partly implemented. Sharing in parameter files is explicit through `same`                                                     |
| EvoTorch         | The searcher holds the run state and a status dictionary per step                                                                                  | A number of generations                                                                                                                                       | Hooks before and after each step and evaluation; loggers append themselves to a hook                                                                                                                                      |
| HeuristicLib 3.3 | Run state in a scope tree and a serializable engine stack                                                                                          | Evaluation counting by naming convention: `ActualName = "EvaluatedSolutions"` on several operators                                                            | Analyzers are operators in the graph. `GeneticAlgorithm.cs` needs 36 `ActualName` assignments and 7 change handlers to keep the name wiring consistent                                                                    |
| OR-Tools CP      | Search limits are protobuf messages; counters live in the solver                                                                                   | `RegularLimit` checks differences from offsets and has a `cumulative` flag to span repeated nested searches; limits compose                                   | About 25 search monitor hooks; a limit is itself a monitor                                                                                                                                                                |

### 7.3 Structurally similar libraries

| Library                        | Blueprint and runtime state                                                                                                             | Sharing and lifetime                                                                                                                                         | Cross-cutting concerns and results                                                                                                                                                                                      |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Pekko Streams                  | Blueprints are immutable and freely shareable; every materialization creates fresh stage state                                          | A blueprint used twice in one graph is materialized twice; sharing across materializations only through an explicitly shared object, `SharedKillSwitch`      | Attributes are inherited by nested modules, the closest wins, and stages receive them when created. Results are materialized values combined explicitly with `Keep`                                                     |
| Gymnasium                      | `EnvSpec` is the data blueprint; `make()` builds a new wrapper stack every time                                                         | None                                                                                                                                                         | Budgets (`TimeLimit`) and statistics (`RecordEpisodeStatistics`) are wrappers. `make()` inserts standard wrappers in a fixed order from spec fields. Version 1.0 removed implicit attribute forwarding through wrappers |
| Keras                          | Layers are stateful; `build` binds weights to the input shape on the first call; `clone_model` gives fresh state                        | Calling one layer twice shares it                                                                                                                            | The serialized configuration stores a shared layer once by name and refers to each call as `[name, node index, tensor index]`. Fixed callback hooks; `fit` returns its `History`                                        |
| Hydra and OmegaConf            | Configuration trees with `_target_`; `instantiate` builds objects recursively; since 1.4 runtime objects are rejected in configurations | Interpolation shares values, not objects; shared object identity has been an open request for years, with unresolved questions on where identity is declared | Not applicable                                                                                                                                                                                                          |
| Polly 8                        | A builder produces an immutable pipeline; strategy state such as a circuit breaker lives in the built pipeline                          | Sharing means sharing a pipeline; a registry caches pipelines by key                                                                                         | Strategies are explicit wrappers, the first added outermost. Telemetry through listeners and enrichers to a named meter                                                                                                 |
| Microsoft dependency injection | Transient, scoped and singleton lifetimes                                                                                               | Scope validation rejects a scoped service captured by a singleton                                                                                            | Built-in decorators were declined because every container adapter would have to support them                                                                                                                            |
| Ray RLlib                      | A configuration builder, frozen and deep-copied when built; it stores callback classes, not instances                                   | Not applicable                                                                                                                                               | Fixed callback hooks; a results dictionary per step; a metrics logger reduced across components                                                                                                                         |
| PyTorch Lightning              | Budgets are trainer settings; the command line interface saves the configuration for reproduction                                       | Callback state needs a `state_key` when one callback type appears twice                                                                                      | Callbacks with fixed hooks, documented as isolated and independent of order; named metrics read back from the trainer                                                                                                   |
| Apache Beam                    | The pipeline graph is separate from the runner's execution; copies of user functions share no state                                     | Composite transforms nest other transforms                                                                                                                   | Counters are named by namespace and name, scoped to the transform that reports them, aggregated across instances and queried from the pipeline result                                                                   |
| .NET instrumentation and Rx    | Libraries instrument themselves with `ActivitySource` and `Meter`; a cold observable reruns its pipeline for every subscription         | `IMeterFactory` isolates meters per service collection; Rx shares a pipeline only through `Publish`                                                          | Explicit instrumentation APIs rather than interception                                                                                                                                                                  |

### 7.4 Lessons

1. **Interception is the outlier.** Of the 31 libraries, only MOEA Framework attaches observers through a pointcut-like mechanism, and it pays for it with a reflective walk that needs package exclusions, type skips and a guard against instrumenting itself. DEAP's one interception tool breaks pickling. Microsoft declined even plain decorators in its container because every adapter would have to understand them. Everyone else uses explicit wrappers, a few fixed hooks or explicit instrumentation calls. This supports [RC1](#rc1-two-composition-mechanisms-one-of-them-runtime-interception).
2. **Budgets, counting and statistics are wrappers in the closest relatives.** ParadisEO counts evaluations with a decorator around the evaluator and stops with a continuator that reads it; that is HeuristicLib's `evaluator.Counted(counter)` with a count terminator, without any further mechanism. Gymnasium implements step budgets and episode statistics as wrappers, and Polly every resilience strategy.
3. **Wrappers must keep their role contract and carry no side channels.** MOEA Framework argues against wrappers because they change the visible type, need access to the nested object and can be bypassed. Role-typed wrappers placed by rewriting avoid all three, because nobody holds the unwrapped node. Gymnasium removed implicit attribute forwarding through wrappers in version 1.0, which supports removing executor controls and their forwarding rules.
4. **Automatic placement is deterministic expansion of stored intent.** Gymnasium's `make()` inserts its standard wrappers from fields of the specification in a fixed order and records them as those fields, not as explicit wrappers. The persisted form keeps the user's intent and the expansion stays inspectable. [Section 7.5](#75-consequences-for-the-proposal) applies this to rule 5.
5. **Fresh by default, sharing explicit.** Pekko materializes a blueprint used twice in one graph twice, Rx reruns cold pipelines per subscription, Hydra's interpolation creates two instances, and ECJ shares a component only when the parameter file says `same`. Keras is the exception: calling one layer twice shares it. HeuristicLib's accepted default sharing within a scope follows Keras.
6. **Shared nodes need explicit identity in the persisted form.** Keras stores a shared layer once by name and refers to its calls by index; ECJ names the shared component explicitly. Hydra has left shared object identity unresolved for years, and its open questions are where identity is declared, initialization order and cycles. HeuristicLib should decide these before serializing configurations.
7. **Two proven ways connect a counter to a terminator.** Either the terminator holds a reference to the counting component, as in ParadisEO, or the run owns the counter and limits read it relative to a baseline, as in ECJ, MOEA Framework and OR-Tools. OR-Tools makes per-search or cumulative counting an explicit parameter of the limit instead of deriving it from sharing rules.
8. **Nested budgets come from structure.** Timefold checks one termination against the solver or the phase scope, and Jenetics and Nevergrad give each sub-engine or sub-optimizer its own budget. HeuristicLib's child builds play the role of Timefold's phase scopes.
9. **One owner for run state, and runtime objects are single use.** Jenetics streams, Timefold solvers, Nevergrad optimizers, Opt4J injectors and Pekko materializations are created per run from immutable configuration. The hybrids leak: pymoo needs deep copies, jMetal has stateful terminations inside the algorithm object, and Optuna's unpersisted sampler state is a documented resume pitfall. Configurations hold descriptions, not instances: Jenetics suppliers, RLlib callback classes and Hydra's rejection of runtime objects.
10. **Results come from the run.** DEAP returns its logbook, Keras its history, Beam a pipeline result, RLlib a results dictionary, and Pekko one materialized value per materialization. When several instances report the same kind of result, Beam names counters by namespace and scopes them to their transform, and Lightning requires a `state_key`: identity of results must be explicit.
11. **Bind structure when building, pass only per-call data per call.** Keras binds a layer's weights once in `build`, Pekko delivers inherited attributes when it creates a stage, and Polly keeps strategy state in the built pipeline while per-call data travels in a context. These are precedents for [rule 6](#41-the-seven-rules) and [rule 7](#41-the-seven-rules). Flax introduced NNX because Linen pushed its configuration/state split through every call site.
12. **Hooks stay few and observe outputs first.** Nevergrad has two callback points and Optuna one. ECJ has eighteen and admits partial implementation. Jenetics, evosax and jMetal analyze values the loop already yields; most of HeuristicLib's progress analysis can read the stream of search states, and `Observed` is needed only for operator calls the states do not show.
13. **Implicit mechanisms tend to be corrected later.** Gymnasium removed attribute forwarding, MOEA Framework removed its façade, Polly folded policy wrapping into its builder, RLlib's shallow freeze needs a deep copy as a defense, Lightning warns against order-dependent callbacks, scikit-learn needed about a decade for experimental callbacks, and HeuristicLib 3.3 paid continually for wiring by string names. In each case the implicit form eventually cost more than the explicit one.

### 7.5 Consequences for the proposal

Confirmed: explicit wrapper nodes as the only composition mechanism (rule 4), lexical build-time providers (rule 6), binding executors when they are built (rule 7), run-owned results, the configuration/executor split and the removal of executor controls.

Changed: rule 5 now stores intent and expands it when the node is built, following Gymnasium. A budget keeps today's shape, a child algorithm, a target and a limit, and expands its child when built; `Traced` does the same for a trace. The persisted configuration stays what the user wrote, and an `Expand()` view shows the effective graph. Sections [4.1](#41-the-seven-rules), [4.4](#44-the-system-spec-scenarios-under-these-rules) and [4.5](#45-representative-usage) describe the revised rule.

New open decisions, recorded in [section 8.2](#82-risks-and-open-questions): explicit identity of shared nodes and instruments in the persisted form, per-call or cumulative counting as an explicit terminator parameter, a build-time report for captive executors, and the tension between default sharing within a build and the fresh-by-default majority.

### 7.6 Sources

Read on 2026-11-04.

- Optimization and machine learning frameworks: Jenetics [`Engine.java`](https://github.com/jenetics/jenetics/blob/master/jenetics/src/main/java/io/jenetics/engine/Engine.java), [`Limits.java`](https://github.com/jenetics/jenetics/blob/master/jenetics/src/main/java/io/jenetics/engine/Limits.java) and [`EvolutionInterceptor.java`](https://github.com/jenetics/jenetics/blob/master/jenetics/src/main/java/io/jenetics/engine/EvolutionInterceptor.java); jMetal [`EvolutionaryAlgorithm.java`](https://github.com/jMetal/jMetal/blob/main/jmetal-component/src/main/java/org/uma/jmetal/component/algorithm/EvolutionaryAlgorithm.java); Timefold [`TerminationFactory.java`](https://github.com/TimefoldAI/timefold-solver/blob/main/core/src/main/java/ai/timefold/solver/core/impl/solver/termination/TerminationFactory.java) and [`PhaseLifecycleListener.java`](https://github.com/TimefoldAI/timefold-solver/blob/main/core/src/main/java/ai/timefold/solver/core/impl/phase/event/PhaseLifecycleListener.java); Optax [`base.py`](https://github.com/google-deepmind/optax/blob/main/optax/_src/base.py) and [`_accumulation.py`](https://github.com/google-deepmind/optax/blob/main/optax/transforms/_accumulation.py); pymoo [`optimize.py`](https://github.com/anyoptimization/pymoo/blob/main/pymoo/optimize.py) and [`termination.py`](https://github.com/anyoptimization/pymoo/blob/main/pymoo/core/termination.py); pagmo2 [`algorithm.hpp`](https://github.com/esa/pagmo2/blob/master/include/pagmo/algorithm.hpp); evosax [`base.py`](https://github.com/RobertTLange/evosax/blob/main/evosax/algorithms/base.py); scikit-learn [`develop.rst`](https://github.com/scikit-learn/scikit-learn/blob/main/doc/developers/develop.rst), [`sklearn/callback/_base.py`](https://github.com/scikit-learn/scikit-learn/blob/main/sklearn/callback/_base.py) and the [API design paper](https://arxiv.org/abs/1309.0238); ML.NET [`IEstimator.cs`](https://github.com/dotnet/machinelearning/blob/main/src/Microsoft.ML.Core/Data/IEstimator.cs); Flax [`why.rst`](https://github.com/google/flax/blob/main/docs_nnx/why.rst).
- Metaheuristic frameworks: ParadisEO [`eoEvalFuncCounter.h`](https://github.com/nojhan/paradiseo/blob/master/eo/src/eoEvalFuncCounter.h), [`eoEvalContinue.h`](https://github.com/nojhan/paradiseo/blob/master/eo/src/eoEvalContinue.h), [`eoCheckPoint.h`](https://github.com/nojhan/paradiseo/blob/master/eo/src/utils/eoCheckPoint.h), [`eoForge.h`](https://github.com/nojhan/paradiseo/blob/master/eo/src/eoForge.h) and [`eoAlgoFoundryEA.h`](https://github.com/nojhan/paradiseo/blob/master/eo/src/eoAlgoFoundryEA.h); DEAP [`algorithms.py`](https://github.com/DEAP/deap/blob/master/deap/algorithms.py) and [`base.py`](https://github.com/DEAP/deap/blob/master/deap/base.py); Nevergrad [`base.py`](https://github.com/facebookresearch/nevergrad/blob/main/nevergrad/optimization/base.py) and [`optimizerlib.py`](https://github.com/facebookresearch/nevergrad/blob/main/nevergrad/optimization/optimizerlib.py); Optuna [`study.py`](https://github.com/optuna/optuna/blob/master/optuna/study/study.py) and [`_callbacks.py`](https://github.com/optuna/optuna/blob/master/optuna/_callbacks.py); MOEA Framework [`Instrumenter.java`](https://github.com/MOEAFramework/MOEAFramework/blob/master/src/org/moeaframework/analysis/runtime/Instrumenter.java), [`Extension.java`](https://github.com/MOEAFramework/MOEAFramework/blob/master/src/org/moeaframework/algorithm/extension/Extension.java), [`MaxFunctionEvaluations.java`](https://github.com/MOEAFramework/MOEAFramework/blob/master/src/org/moeaframework/core/termination/MaxFunctionEvaluations.java) and [release notes](https://github.com/MOEAFramework/MOEAFramework/blob/master/docs/releaseNotes.md); Opt4J [`Opt4JTask.java`](https://github.com/SDARG/opt4j/blob/master/opt4j-core/src/main/java/org/opt4j/core/start/Opt4JTask.java) and [`Control.java`](https://github.com/SDARG/opt4j/blob/master/opt4j-core/src/main/java/org/opt4j/core/optimizer/Control.java); ECJ [`EvolutionState.java`](https://github.com/GMUEClab/ecj/blob/master/ecj/src/main/java/ec/EvolutionState.java), [`Statistics.java`](https://github.com/GMUEClab/ecj/blob/master/ecj/src/main/java/ec/Statistics.java) and [`BreedingPipeline.java`](https://github.com/GMUEClab/ecj/blob/master/ecj/src/main/java/ec/BreedingPipeline.java); EvoTorch [`searchalgorithm.py`](https://github.com/nnaisense/evotorch/blob/master/src/evotorch/algorithms/searchalgorithm.py); HeuristicLib 3.3 [`LookupParameter.cs`](https://github.com/heal-research/HeuristicLab/blob/master/HeuristicLab.Parameters/3.3/LookupParameter.cs) and [`GeneticAlgorithm.cs`](https://github.com/heal-research/HeuristicLab/blob/master/HeuristicLab.Algorithms.GeneticAlgorithm/3.3/GeneticAlgorithm.cs); OR-Tools [`constraint_solver.h`](https://github.com/google/or-tools/blob/stable/ortools/constraint_solver/constraint_solver.h) and [`search_limit.proto`](https://github.com/google/or-tools/blob/stable/ortools/constraint_solver/search_limit.proto).
- Structurally similar libraries: Pekko [stream basics](https://github.com/apache/pekko/blob/main/docs/src/main/paradox/stream/stream-flows-and-basics.md), [composition](https://github.com/apache/pekko/blob/main/docs/src/main/paradox/stream/stream-composition.md) and [dynamic streams](https://github.com/apache/pekko/blob/main/docs/src/main/paradox/stream/stream-dynamic.md); Gymnasium [`common.py`](https://github.com/Farama-Foundation/Gymnasium/blob/main/gymnasium/wrappers/common.py), [`registration.py`](https://github.com/Farama-Foundation/Gymnasium/blob/main/gymnasium/envs/registration.py) and the [migration guide](https://github.com/Farama-Foundation/Gymnasium/blob/main/docs/introduction/migration_guide.md); Keras [`functional.py`](https://github.com/keras-team/keras/blob/master/keras/src/models/functional.py) and the [layer guide](https://github.com/keras-team/keras-io/blob/master/guides/making_new_layers_and_models_via_subclassing.py); Hydra [instantiation](https://github.com/facebookresearch/hydra/blob/main/website/docs/advanced/instantiate_objects/overview.md) and [issue 1393](https://github.com/facebookresearch/hydra/issues/1393); Polly [pipelines](https://github.com/App-vNext/Polly/blob/main/docs/pipelines/index.md) and [telemetry](https://github.com/App-vNext/Polly/blob/main/docs/advanced/telemetry.md); .NET [service lifetimes](https://github.com/dotnet/docs/blob/main/docs/core/extensions/dependency-injection/service-lifetimes.md), [issue 36021](https://github.com/dotnet/runtime/issues/36021) and [metrics instrumentation](https://github.com/dotnet/docs/blob/main/docs/core/diagnostics/metrics-instrumentation.md); RLlib [`algorithm_config.py`](https://github.com/ray-project/ray/blob/master/python/ray/rllib/algorithms/algorithm_config.py) and [`callbacks.py`](https://github.com/ray-project/ray/blob/master/python/ray/rllib/callbacks/callbacks.py); Lightning [callbacks](https://github.com/Lightning-AI/pytorch-lightning/blob/master/docs/source-pytorch/extensions/callbacks.rst) and [callback state](https://github.com/Lightning-AI/pytorch-lightning/blob/master/docs/source-pytorch/extensions/callbacks_state.rst); Beam [programming guide](https://github.com/apache/beam/blob/master/website/www/site/content/en/documentation/programming-guide.md); Rx [`Publish`](https://github.com/ReactiveX/reactivex.github.io/blob/master/documentation/operators/publish.html).

## 8. Risks, alternatives and validation

### 8.1 Alternatives compared

Following developer guidelines § 2.1, three alternatives:

| Alternative                       | Concepts added                                               | Concepts removed                                                                    | Assessment                                                                                  |
| --------------------------------- | ------------------------------------------------------------ | ----------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| A. Finish the current plan        | Definitions, `Advised`, algorithm-call source, elapsed clock | Handles, run setup, observation points                                              | Smallest change; keeps two composition paths, the equivalence contract and every scope rule |
| B. Remove interception only       | Introspection, rewriting, providers                          | Pointcut, advice, aspect, join point, wrapper chain, scope guards and ordering      | Most of the conceptual gain with moderate migration; keeps run types in every signature     |
| C. B plus problem-bound executors | As B                                                         | As B, plus run-type parameters, arity ladders, execution signatures and bound bases | The simplest coherent design; the largest migration, touching every operator                |

The recommendation is C, staged as B first. B is independently valuable and validates rules 1 to 6 before rule 7 touches every operator.

### 8.2 Risks and open questions

1. **Introspection implementation.** This is the [parameter grid proposal's](configuration-parameters-and-experiment-persistence.md#open-decisions) open decision 1, which prefers declared metadata without reflection over properties. Rewriting adds one requirement: replacing a node everywhere in a graph, memoized so that shared references stay shared, which is that proposal's open decision 4. Recommendation: settle both proposals on one contract, and make it a core part of `IConfigurationNode` rather than an experiment service.
2. **Rewriting typed slots.** A rewrite replaces an `IOperator<TOperation, TCandidate>` with a wrapper of the same type, which is safe when the target is typed. Selecting by role across a graph, such as every evaluator, needs the operation known at the call site. Recommendation: typed selection only.
3. **Rewriting below `Sharing`.** Described in [section 4.4](#44-the-system-spec-scenarios-under-these-rules). Recommendation: report a listed shared configuration that its subtree never requests.
4. **Problem-bound executors and nested problems.** Composite encodings and meta-optimization run children over other search spaces and problems. Both already construct children explicitly, so a child build for the projected problem fits them, but a spike must show it on `CompositeSearchSpace` and `MetaOptimizationProblem`.
5. **Stateless operators as their own executors.** With rule 7 an operator that needs its search space allocates a small executor per build. The cost is per build, not per call, but the benchmark suite should confirm that no hot path changes.
6. **Delegates in trace instruments.** Rewriting with delegate-valued traces produces a configuration that cannot be persisted, as with `Advised` today. Rewriting keeps the original configuration persistable, which is better than placing the delegate inside the user's configuration. Value strategies for common trace values remain the long-term answer under developer guidelines § 4.8.
7. **Racing and runtime-generated configurations.** `DynamicRacingAlgorithm` adds tracking advice per contender. It would rewrite each contender's configuration before building it and provide the tracker in the contender's child build.
8. **Executor controls.** Replacing mutation-strength controls needs a decision on whether adaptive strength is an operation input or an algorithm-owned value. Recommendation: an operation input, because it keeps wrappers transparent without forwarding rules.
9. **Default sharing within a build.** Rule 2 keeps the accepted decision that a reference used twice is one executor, as in Keras. Pekko, Rx, Hydra and ECJ build fresh unless sharing is declared ([section 7.4](#74-lessons), lesson 5), and a configuration read from data cannot show which references were meant to be the same object unless the format says so. Recommendation: keep rule 2 for C# configurations, and give shared nodes an explicit identity in the persisted form, as Keras and ECJ do. Decide up front where that identity is declared, how references to it are resolved and whether cycles are rejected, the questions Hydra left open.
10. **Instrument identity.** Instruments use reference identity today. Beam names counters by namespace and name and scopes them to their transform, and Lightning requires a `state_key` when one callback type appears twice. Recommendation: instruments carry a name that serves as their identity in the persisted form, while C# code keeps passing the instrument object.
11. **Per-call or cumulative counting.** OR-Tools, MOEA Framework and ECJ let a limit read a run-owned counter relative to a baseline, and OR-Tools makes per-search or cumulative counting an explicit `cumulative` parameter. A count terminator with that parameter could express a per-turn budget without rebuilding private state. It gives wrong results when the same budget runs interleaved or concurrently within one run, as racing contenders do, because they share the run counter. Recommendation: keep private budget counters through rule 6, and add the parameter only to terminators that read run results.
12. **Captive executors.** Microsoft's container rejects a scoped service captured by a singleton. Under rule 3, a shared configuration builds its children in a scope of its own, so a child also used directly in each turn becomes two executors without notice. Recommendation: report such a child at build time rather than silently building it twice.

### 8.3 How to validate before deciding

A time-boxed spike on a separate branch, comparable to the earlier operator role prototypes:

1. Implement introspection, the memoized rewrite and the build context with providers next to the existing resolver.
2. Port `Counted`, `Observed`, the two operator budgets as intent nodes that expand their child, `Sharing`, Cycle and one trace, and check that `Expand()` prints the effective graph.
3. Re-run the measured scenarios S6 to S9 and Q2 to Q6 against both implementations and compare the logs.
4. Port one role, mutation, to problem-bound executors, including the composite encoding mutator, and benchmark a genetic algorithm generation against the current path.
5. Report concepts, public types and lines removed, and the scenarios whose behavior differs.

## 9. Relation to accepted decisions and recorded rejections

Accepted decisions this proposal keeps: the configuration/executor split, executor fields for private data, the scope as sharing unit, child scopes created by meta-algorithm authors, `Sharing`, run-bound results with one result per instrument, `Counted`, `Timed` and `Observed`, observation through executor calls, budgets checked between states, the failure outcomes of spec Q6, removal of run setup, handles and observation points, and the operation contracts with invocation wrappers.

Accepted decisions this proposal supersedes: aspects as a runtime mechanism, `Advised`, advice belonging to the scope, the ordering and duplicate rules of the design review, the already-built and child-scope guards, the exemption from self-added advice, one aspect per instrument per run, the construction failure state of the scope tree, multi-operation configurations with a single hook, and executor controls. The governing contract of [aspects as automatic composition](aspects-as-automatic-composition.md#governing-contract) is fulfilled more strictly: automatic placement produces the explicit composition itself.

Recorded rejections this proposal reopens:

| Rejection                                                                             | Recorded reason                                                                                                            | Why it no longer applies                                                                                                                                                                                                                                                                              |
| ------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Advice expressed as wrapper configurations                                            | Mixed the configuration graph with woven behavior and hid advice behind a configuration                                    | The rejected form let the resolver substitute a wrapper whose child was never used for whatever advice selected. Here the node holding the intent expands its own child into wrappers whose child is the wrapped node, deterministically and visibly through `Expand()`, as Gymnasium's `make()` does |
| Advice-based configured wrappers                                                      | No behavior needed both forms                                                                                              | Already reopened by the developer; the proposal removes the advice form instead of adding a second one                                                                                                                                                                                                |
| Generic private state sharing, and ancestor lookup in rule 6 of the lifetime decision | An all-purpose registry with ancestor lookup and implicit sharing                                                          | Providers are explicit, lexical and build-time only. Only a node that declares an instrument or `Sharing` provides anything, nothing is shared implicitly, and the lookup is not order dependent                                                                                                      |
| Typed operator invocation and context carriers                                        | A generic invocation path and a context argument cost performance and per-role leaves remain                               | Rule 7 adds no context argument and no generic invocation; it removes two arguments from every named method                                                                                                                                                                                           |
| Internally derived configurations as selectable sources                               | Selection and measurement would follow internals the user cannot see; hidden nodes could share state with the user's graph | No pointcut selects nodes, so derived nodes cannot change what is measured. Expansion derives only wrappers around the stored target and copies of its ancestors, and `Expand()` shows them                                                                                                           |

Recorded rejections the proposal is consistent with: live resource instances in reusable configurations, runtime objects replacing configurations and executors, parallel observation systems, observation points, and generated operator families.
