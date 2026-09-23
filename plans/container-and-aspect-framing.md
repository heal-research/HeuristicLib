# Container and aspect framing

Status: direction accepted. The work described here is the second of two branches. It starts after the layering branch described in [layering and phase separation](layering-and-phase-separation.md), which goes first as a cleanup. Final names and detailed designs are settled in the implementing branches. Written during `analysis-overhaul` and current as of its end on 2026-09-23.

This document compares the execution model with dependency injection containers and aspect-oriented programming, records the direction taken from that comparison, and orders the work together with the layering report. The report remains the record of its findings and its dependency analysis.

## Summary

The execution model is two well-known systems in one:

- Resolution is a scoped DI container. `ResolutionScopeBuilder` is the registration phase, `ResolutionScope` the resolution phase, and child scopes are nested lifetime scopes.
- Decoration is an aspect weaver. Budgets and racing wrappers are around advice, observations are after-returning advice, and `IExecutionModule` is the aspect. The scope weaves them in as it builds each instance.

The names mostly agree with that reading already. `builder.Install(module)` is Guice's `install`, `Decorate` rewrites a registration the way Scrutor's does, and `IExecutionModule` has the shape of an Autofac or Guice module. What is missing is saying so, and going further where AOP offers more than HeuristicLib does today.

The direction:

1. Say it. The docs present the execution model as a DI container plus an aspect weaver, so users recognize concepts they know instead of learning a system of HeuristicLib's own.
2. Use the established terms. API names follow AOP and DI terminology where the meaning matches. A different name is fine where it is clearly better. Final names are decided in the branch.
3. Lean into AOP. Pointcuts become a type of their own with matching in the style of AspectJ, all advice kinds become available, and analysis becomes one client of that system: an analyzer is a module, with no parallel contract.
4. Keep an order. The layering branch goes first and cleans up the structure. The DI and AOP alignment builds on it.

Some things stay refused, listed below: type-keyed resolution and auto-wiring, dynamic proxies, a universal role invocation path and a runtime service locator.

## The framing

### Two halves and the joint

| HeuristicLib | DI container | AOP |
| --- | --- | --- |
| Configuration | Registration, Spring bean definition | |
| Execution instance | Resolved service, bean | Target object |
| `ResolutionScopeBuilder` | `IServiceCollection`, Autofac `ContainerBuilder`, Guice `Binder` | |
| `ResolutionScope` | `IServiceProvider`, Autofac `ILifetimeScope` | Weaver |
| `CreateChildScope` | `CreateScope`, `BeginLifetimeScope` | |
| `Decorate` | Scrutor `Decorate` | Around advice |
| `IExecutionModule`, `Install` | Autofac `Module`, Guice `AbstractModule` and `install` | Aspect |
| `Observe` callback | | After-returning advice |
| Observation value | | Join point context |
| Algorithm and operator boundaries | | Join points |
| Observation source | | Pointcut naming one configuration |
| `DecorationOrigin` and the ordering rules | Decorator registration order | Advice precedence |
| `ExecutionSignature.Fits` | `ValidateOnBuild`, Simple Injector `Verify()` | |
| Run | One container scope | |
| `NewExecutionInstancesPerCycle` | A new lifetime scope per unit of work | |

The joint between the halves is the decoration's type. A decoration is `Func<TConfiguration, TConfiguration>`. It rewrites the registration rather than the instance, as Scrutor rewrites a `ServiceDescriptor`, and the scope then resolves the rewritten registration. Weaver and container are one component, which is why decoration composes with resolution instead of running beside it.

Guice is the closest single precedent. Its `bindInterceptor(classMatcher, methodMatcher, interceptor)` registers advice inside the same container that resolves the advised objects.

### Where the model departs from both

Each of these is hard to explain today and takes one sentence once the reader's prior is named.

1. **Keyed by object reference, not by service type.** Two equal configurations are two registrations. A `with` copy is a new registration, so an analyzer attached to the original records nothing when the copy runs. Resolution looks nothing up by type.
2. **Instance identity is chain identity.** Two scopes share an execution instance exactly when the decoration chains applying to it are identical. In Spring a proxied singleton is still one singleton, and in Autofac a decorated registration keeps its one lifetime. Here the chain is part of what an instance is. Sibling scopes never share, and instances are never hoisted to the ancestor that owns their chain, because hoisting would let an instance outlive the scope it was built for. That is the failure a captive dependency causes in a DI container, reached from the other side.
3. **Precedence is derived, not declared.** AspectJ's `declare precedence` and Spring's `@Order` leave the order of two aspects at one join point to the author, and undefined without one. Here it follows from origin, then scope depth, then install order. Origin comes first so that advice the configuration itself declares, such as a budget, always sits inside advice a run adds and never measures it.

A fourth difference belongs with the phases. Registering after the container is built is silently ignored by MS DI and throws in Simple Injector. Here it cannot be written, because declaring and resolving are separate types.

These describe the model as it is today, not constraints on the redesign. If the alignment moves closer to standard AOP, adopting its resolution and precedence rules more closely is an option, where that makes the system easier to understand or solves a problem the current rules cannot.

## Relation to the layering report

The report reached the same model from the other side. Its part 1 lists "DI registrations, container, resolved object graph" as one of three fitting analogies, and its part 3 names pointcut, advice and weaver.

| Layering report | Where it goes |
| --- | --- |
| Part 1: configuration and execution are phases, not layers | Adopted as the model both branches work from |
| Part 1: `Execution` holds four responsibilities | Layering branch: the namespace split, following the line .NET draws between `DependencyInjection` and `Hosting` |
| Cycle 1: counters under `Analysis` | Layering branch |
| Cycle 2: `IAnalyzer` and `IExecutionModule` share a signature | Layering branch: analyzers become modules, with no parallel contract |
| The `DecorationOrigin` hole | Closed on `analysis-overhaul` by the run. It disappears for good once analyzers are modules |
| Part 3: the pointcut has no type | DI and AOP branch: a pointcut type with AspectJ-style matching, replacing the report's phase 4 |
| Part 4: `AlgorithmRun` is three objects | Layering branch, as an internal split |
| Phase 5: enforcement | Layering branch, plus one rule in the DI and AOP branch |

## Direction

### Analyzers are modules

Decided: analysis builds on execution modules, and there is no parallel analyzer contract. Whatever an analyzer is, it is a module that owns results. This avoids a duality that is not needed and makes it plain how analysis integrates with the execution model instead of standing beside it.

An `IAnalyzer` interface or an `Analyzer` base class on top of the module contract stays possible. Keep one where it helps readers understand the system, or where genuinely analyzer-specific behavior belongs in it, such as the synchronization and publication rules `AccumulatingAnalyzer` holds today. The history below is the warning against keeping one that only marks a distinction.

The analysis rework went through three shapes to get here. `IAnalyzer` started as `IAnalyzer : IExecutionHook` with no members. The simplification pass deleted it, because a marker that appears in constraints reads as a distinction that does not exist. It came back as its own contract when analyzers became first-class stateful run components that own their results, because an execution hook did not feel like a base an analyzer could stand on. That is the current code: an analyzer "is not an execution module, although its installation can create and install any number of modules", and the run installs it with module origin.

A module read as a place to declare advice is a general mechanism, and analysis is one of its clients. Ownership is a separate question: an aspect with fields is still an aspect. The change belongs to the layering branch, because it also removes the run's dependency on analysis. `AlgorithmRun` then keeps one attachment list, `TrialAnalyzer` constrains on the module type, and install order becomes attachment order across everything a run installs, where today analyzers install first. That changes behavior for runs that attach both and needs a spec before the change. Whether a user-facing convenience such as `AddAnalyzer` stays is a naming question for that branch.

### Pointcuts

A pointcut becomes a type of its own, matched when the scope builds instances rather than on every call. Matching follows AspectJ:

- by role or type, including derived types;
- by attribute on the configuration type;
- by wildcard patterns over type names;
- by name, through a `Named` wrapper. It forwards every call to the operator it wraps and only carries a name, so matching a name is an attribute check under the hood;
- by nesting, meaning a join point only when reached from a given caller.

Because matching happens while the scope builds, it sees every configuration that gets resolved, including those created during resolution, and matching costs nothing on the unadvised path.

The rework built a selector type once. `Anchor<TObservation>`, with `Anchor.At(...)` factories and a public `IObservationRecorder`, cut `Analyzer.Trace` from 20 overloads to 4 and `Observe` from 10 to 2, and reduced `Analyzer.Trace` from six type parameters to three. It was removed because an anchor wrapping one configuration reference is only a second name for it. A pointcut earns its place by selecting what one reference cannot. User-facing shortcuts such as `algorithm.TracePopulationQuality()` keep taking the configuration and build the pointcut themselves.

A pointcut that matches nothing is a valid result, not a mistake, and is not reported. One pointcut definition is meant to be reused across algorithms: a pointcut for mutations applied in an experiment on an algorithm without a mutator records nothing, and that is the right answer.

#### Nesting

Open, and to be discussed again during the redesign. The problem: resolution builds one instance per configuration per scope, and a decoration wraps that instance. When a genetic algorithm and a hill climber both use one mutator object, both hold the same instance, so a wrapper around it is seen by both, and no wrapper exists that only the hill climber holds.

First ideas, none convincing:

1. Use the separation that already exists. Some meta-algorithms, such as `CycleAlgorithm` with fresh instances per cycle, build a stage in a scope of its own, so the mutator that stage uses is already a separate instance from the outer algorithm's. A nesting pointcut could target only such cases. It needs no new mechanism, but it depends on how a meta-algorithm happens to build its stages, and it cannot separate callers that share a scope.
2. Give the caller its own instance when a nesting pointcut matches. For a stateful operator the two callers would stop sharing its state, so observing a run would change it.
3. Wrap the call rather than the instance, as AspectJ's `call` join point does, handing the caller a wrapper around the shared instance. This keeps sharing intact but is complex.

The redesign may make the problem disappear or suggest a better answer, for example by adopting AOP's resolution rules more closely. Nesting here is structural, known when the graph is built, so whatever the answer, it can be decided once while the scope builds rather than checked on every call like AspectJ's `cflow`.

### Advice kinds

All advice kinds become available: before, after returning, after throwing, after in the sense of finally, and around. Around advice may change what an operation does, and aspects that change algorithm behavior are a legitimate use.

Analyzers should use read-only advice. That is a convention, not a rule the system enforces: an analyzer is built on the same system as behavior-changing aspects, and nothing forbids it from using one. Changing the search is simply not what an analyzer is for.

Advice stays typed per role. The wrappers that apply advice are written per role, as the current observing wrappers are. Advice across roles is the tricky part. It is useful for generic concerns such as counting operator calls or timing them, where the specifics of the operator do not matter, only that it was called. There each role's wrapper can erase its call to a common join point shape, which the backlog's [typed operator invocation](developer-backlog.md#typed-operator-invocation) entry allows for wrappers that apply advice, because they leave the role contracts untouched. The rejected universal invocation path is a different thing: it replaced the role contracts themselves.

The current precedence rules, origin, then depth, then install order, extend to advice kinds. How before, around and after advice from one module order among each other is part of the branch's design. Rebuilding budgets and the counting and duration instrumentation as around advice, a backlog item, fits here.

### Naming

API names use the AOP and DI terms wherever HeuristicLib means the same thing: pointcut, advice, aspect, join point, module, scope, decorate. A different name is fine where it is clearly better. Terms whose meaning differs here stay out of API names:

- container, because it implies lookup by type;
- singleton, scoped and transient, because sharing here follows chain identity rather than a lifetime setting;
- proxy, because it implies generated wrappers.

Names that already match, and are a starting point rather than a decision: `ResolutionScope`, `ResolutionScopeBuilder`, `CreateChildScope`, `Decorate`, `Decoration`, `DecorationOrigin`, `IExecutionModule`, `Install` and `ExecutionSignature.Fits`.

The `Interceptor` operator role transforms a produced search state. In Castle DynamicProxy, Guice, Spring AOP and Autofac, an interceptor is around advice. Once the API uses AOP terms, keeping the role's name would put one word in the API with two meanings, so the role is renamed. `StateRewriter` is one candidate. The rename touches about 52 source and test files: `IInterceptor`, `IInterceptorInstance`, the instance bases, the concrete interceptors, the counting, duration and observing decorations, the pipeline, the observation and clock plumbing, algorithm properties named `Interceptor`, samples, specs and docs.

The glossary gains a status for terms from other systems that HeuristicLib does not adopt as its own:

- `Analogue`: the term a well-known system uses for a closely related concept. Use it to orient a reader, never as a synonym in HeuristicLib text.

Where an AOP term becomes the canonical name, the glossary uses it directly instead.

### What stays refused

The design goals ask for "plain C# composition over framework magic", and § 3.1 rules out ambient state, global registries and opaque indirection. Leaning into AOP keeps those constraints:

- **Type-keyed resolution and auto-wiring.** Resolution stays keyed by configuration reference, with no `Resolve<IMutator<T>>()` and no constructor discovery. Pointcuts that match by type choose which instances receive advice. They do not change how an instance is found.
- **Dynamic proxies and reflection emit.** Wrappers stay hand-written per role, as [operator implementation](../docs/contributing/architecture/operator-implementation.md) and the rejected [generated operator families](developer-backlog.md#generated-operator-families) settle. Pointcut matching may use reflection while the scope builds; the call path does not.
- **A universal role invocation path.** Castle's `IInterceptor.Intercept(IInvocation)` sends every call through one generic path with `Proceed()`. That is the [typed operator invocation](developer-backlog.md#typed-operator-invocation) experiment, measured at 2.39 ns against 12.84 ns and rejected.
- **A runtime service locator.** § 4.3 already limits the scope to instance creation, child scopes and delayed child algorithm creation. Composition-root code receiving the container is the standard exception in DI practice.

Once the branch settles it, this list becomes a guideline, § 3.5, because it is policy.

## Documentation

Contributor docs get the full framing. The user guide explains what users meet: a sentence or two where a DI prior prevents a known mistake, and a page on writing aspects once pointcuts and advice exist.

### Contributor docs

1. [execution-instances.md](../docs/contributing/architecture/execution-instances.md) opens with the framing: two paragraphs, a trimmed version of the two-halves table, and the departures.
2. [instance-resolution.md](../docs/contributing/architecture/instance-resolution.md) keeps its content and gains orientation. "Two phases, two types" names the lineage in one sentence: ignored by MS DI, rejected by Simple Injector, unrepresentable here. The sharing section names chain identity and the captive-dependency failure the no-hoisting rule prevents. The ordering section sets the three keys against `declare precedence` and `@Order`.
3. [analyzers.md](../docs/contributing/architecture/analyzers.md) presents analysis as one client of the aspect system: an analyzer is a module that owns results and conventionally uses read-only advice.
4. [writing-meta-algorithms.md](../docs/guide/extending/writing-meta-algorithms.md) is addressed to authors, so its warning about calling `CreateExecutionInstance` on a child can name the self-invocation problem Spring users know: a call that bypasses the proxy bypasses the advice.

### User guide

1. [running-algorithms.md](../docs/guide/execution/running-algorithms.md), in "Explicit runs for analysis":

   > If you have used dependency injection: an algorithm configuration is a set of registrations, and each run builds a fresh scope from them. That is why two calls to `algorithm.Stream` start two runs, and why analyzers attach to the run rather than to the algorithm, the way decorators are added to a container rather than to the class they wrap.

2. [observability-and-analysis.md](../docs/guide/execution/observability-and-analysis.md), beside "Observation sources match configurations by reference":

   > In dependency injection terms, a source is a registration, not a service type. A configuration copied with `with` is a new registration, and an analyzer attached to the original never sees it.

3. [writing-meta-algorithms.md](../docs/guide/extending/writing-meta-algorithms.md), opening its warning:

   > This is the same mistake as calling `new` on a service your container should provide. The object works, but none of the decorators registered for it apply.

4. A page on writing aspects: pointcuts, advice kinds and the convention that analyzers only read.
5. [core-concepts.md](../docs/guide/fundamentals/core-concepts.md) and the guide index stay with the problem domain.

## Order of work

### First: the layering branch

The layering branch goes first. It is a cleanup that leaves a clearer structure for the alignment to build on, and its moves are easiest to review while nothing else changes. It takes the layering report's phases 0 to 3 and 5:

1. The doctrine: `layering.md` with the layers, their rules and the phase distinction.
2. The two dependency cycles, including analyzers becoming modules.
3. The namespace split. Its names are decided at the start of the branch, so each file moves once. `Execution` and `Runs` are the current proposal.
4. The internal split of `AlgorithmRun` into composition, scope and a `RunHost` that `ExperimentRun` reuses. The public `AlgorithmRun` and its fluent API stay.
5. Enforcement of the layer rules through an architecture test or a Roslyn analyzer.

### Second: the DI and AOP alignment branch

It starts from the layering branch's structure. A suggested order:

1. The framing in the contributor docs and the user-guide sentences above.
2. Terminology: AOP terms in the API where the meaning matches, and the `Interceptor` role rename.
3. The pointcut type and its matching, nesting included.
4. The advice kinds.
5. Budgets and instrumentation rebuilt on around advice, if the design supports it.
6. The user-guide page on writing aspects.
7. One more enforcement rule. `HLib0001` flags a child built with `CreateExecutionInstance` instead of resolved, but only inside another `CreateExecutionInstance`. Flagging such calls anywhere in `src` outside the creation delegate handed to `ResolutionScope.Resolve` would also catch meta-algorithms that build children lazily, which [writing-meta-algorithms.md](../docs/guide/extending/writing-meta-algorithms.md) guards by prose alone. Its false-positive rate needs checking against the authoring bases first.

## Open for the branches

1. Final names: the pointcut type, the advice kinds, the module or aspect type, the `Interceptor` role, and the namespaces, which the layering branch settles.
2. Nesting. None of the first ideas convinces, and the redesign may resolve it differently.
3. The pointcut syntax: a C# expression API, AspectJ-like string patterns or both; what wildcards cover, such as type names, namespaces and generic arguments; the attribute model; and how `Named` carries its name.
4. Resolution and precedence rules: which of the current rules stay, and how closely to follow standard AOP, including precedence within one module and between advice kinds.
5. Advice across roles: the shape of the common join point and which advice kinds it is offered for.
6. Whether an `IAnalyzer` interface or `Analyzer` base class sits on top of the module contract, and whether user-facing conveniences such as `AddAnalyzer` and the trial analyzer lookup stay.
7. Whether `AlgorithmRun` and `ExperimentRun` belong in `Contracts` at all, from the layering report's judgement calls.

## Out of scope

- A lifetime enum or policy object replacing `NewExecutionInstancesPerCycle`. The framing names the concept, and nothing else asks for the type.
