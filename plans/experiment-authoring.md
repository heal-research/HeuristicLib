# Experiment Authoring And Execution

## Status

This document records the agreed first draft design for experiment configuration, authoring and execution. It describes confirmed requirements, the intended implementation direction and explicitly deferred topics.

## Goal

An experiment configuration defines a reproducible family of independent algorithm runs. An `ExperimentRun` materializes those runs with isolated execution registries, isolated analyzer states and deterministic random assignments. Callers decide whether the runs execute sequentially, concurrently or individually.

Experiment is the current provisional term for this concept.

## API Ease Target

The experiment API must make common experiment construction feel as direct as ordinary algorithm composition. The final public API should support this fluent shape:

```csharp
var experiment = algorithm
    .AsGrid()
    .VaryBy(
        [50, 100, 200],
        (algorithm, populationSize) => algorithm with
        {
            PopulationSize = populationSize
        })
    .VaryBy(
        [0.05, 0.1],
        (algorithm, mutationRate) => algorithm with
        {
            MutationRate = mutationRate
        })
    .Repeat(10);
```

`AsGrid()` returns a `GridExperiment`. `VaryBy(...)` returns another `GridExperiment` and `Repeat(...)` returns a `RepeatedExperiment`. The public chain must not require an intermediate `.AsExperiment()` conversion.

Every `VaryBy(...)` performs a left to right Cartesian expansion. Its transformation receives each algorithm configuration produced by all preceding dimensions, then produces one configuration for every declared value of the new dimension. Trial materialization follows dimension declaration order and value declaration order. For the example above, all mutation rates for population size `50` appear before all mutation rates for population size `100` and so on. This order also determines structural random fork paths and remains stable across execution policies.

Users should not need to manually construct execution registries, child runs, random forks or nested generic orchestration types for common grid and repetition experiments. Exact generic declarations remain implementation details as long as normal use preserves this shape and static typing.

This usage should become an executable API usage spec before implementation is considered complete.

## Experiment Versus Meta Algorithm

An experiment and a meta algorithm may both contain several algorithm configurations, but their execution semantics differ.

| Meta algorithm | Experiment |
| --- | --- |
| Coordinates child algorithms inside one run | Coordinates several independent runs |
| May pass search state between child algorithms | Does not pass search state between runs |
| May deliberately share execution instances through related registries | Gives every run its own root execution registry |
| Produces one search state stream | Provides one search state stream per run |
| Has one analyzer state per attached analyzer | Creates isolated analyzer states for every run |

An experiment is not an algorithm and should not use algorithm naming.

## Run Vocabulary And Types

`Run` is the shared execution concept, but the first implementation should use separate concrete types rather than introducing a public base class only for conceptual symmetry:

```text
Algorithm configuration
  -> AlgorithmRun

Experiment configuration
  -> ExperimentRun
      -> ExperimentTrial
          -> AlgorithmRun
```

The current non generic `Run` base and `Run<TCandidate, TSearchSpace, TProblem, TSearchState>` should become the corresponding non generic and generic `AlgorithmRun` types. An `ExperimentRun` represents one execution of the complete experiment. An `ExperimentTrial` represents one independently executed algorithm within that experiment. This matches the natural model that one experiment contains several trials.

`AlgorithmRun` and `ExperimentRun` have different progress types, result types and scheduling needs. No current consumer needs to handle both polymorphically. Their single use lifecycle machinery may be shared internally. A public generic run abstraction should be extracted later only if a concrete polymorphic use case appears.

Configurations create runs. Runs expose the three primary execution forms:

```csharp
run.Stream(...);
run.Complete(...);
await run.CompleteAsync(...);
```

Configuration convenience extensions should use the same names. The existing `RunToCompletion` wording should be replaced so algorithm and experiment execution use one vocabulary.

### Setup And Execution Types

The first implementation uses one mutable `AlgorithmRun` or `ExperimentRun` object. A run contains its algorithm or experiment, problem and random number generator. `WithAnalyzer(...)` and `WithAnalysis(...)` mutably attach analyzers before execution and return the same run for fluent composition. Calling `Stream(...)`, `Complete(...)` or `CompleteAsync(...)` starts execution and prevents later analyzer attachment.

Both run types use the same delayed execution initialization rule. The configurable run records concrete configurations only. The first execution call creates analyzer states, observation plans, execution registries and algorithm or operator execution instances. Neither `CreateRun(...)`, `WithAnalyzer(...)` nor `WithAnalysis(...)` creates that runtime infrastructure.

There is no public setup method or setup type in the first implementation. The public API must not mix a mutable run with a partial explicit setup abstraction. The alternative design would require a separate immutable setup type and a separate running type with an explicit conversion between them. That complete alternative is postponed because it currently requires another public concept or a final build step that weakens the easy fluent API. C# does not provide linear types, so returning a new running type cannot by itself prevent callers from retaining and reusing an older setup object.

Preparation is internal and implicit in the first implementation. `Stream(...)`, `Complete(...)` and `CompleteAsync(...)` mark the run as started, freeze analyzer attachment and create the required analyzer states, observation plan, registry and execution instances. No public `Setup()`, `Freeze()` or `Prepare()` method is added.

Every lifecycle operation validates whether execution has started. A repeated execution call, analyzer attachment after execution started or a conflicting individual and combined experiment execution call throws a clear `InvalidOperationException`.

A future separate immutable setup type may expose an explicit conversion to a running type. That possibility does not affect the first implementation.

### Run Lifecycle

`AlgorithmRun.ExecutionStarted` is `false` while analyzers may be attached and becomes `true` when `Stream(...)`, `Complete(...)` or `CompleteAsync(...)` is called. The transition occurs when the method is called, not when a returned stream is first enumerated. Public streaming methods are therefore noniterator wrappers that start and prepare the run before returning a private iterator.

| `ExecutionStarted` | Analyzer attachment | Execution calls | Analyzer result access |
| --- | --- | --- | --- |
| `false` | Allowed | The first call starts execution | Not available because analyzer states do not exist yet |
| `true` | Rejected | Rejected | Available after analyzer states are created |

`ExperimentRun.ExecutionStarted` is derived from its trials and becomes `true` as soon as any trial run has started. Combined execution first verifies that no trial has started, then calls the ordinary `Stream(...)` method on every trial run before scheduling begins. This prepares every trial and prevents direct trial execution after combined execution starts. Starting any trial individually prevents later combined execution while leaving the other trials available for individual execution.

Runs do not track separate completed, faulted, canceled or stopped phases. They remain single use after execution starts regardless of how execution ends. The first implementation does not promise thread safety for configuring or starting the same run concurrently, so lifecycle guards use simple Boolean state without locks.

## Independent Algorithm Runs

Every trial in an experiment owns a regular `AlgorithmRun`.

Each run must have:

1. Its own root `ExecutionInstanceRegistry`.
2. Its own algorithm execution instance and operator execution instances.
3. Its own analyzer states and results.
4. Its own deterministic random number generator fork.
5. Its own lifecycle and completion behavior.

Reusing the same algorithm configuration in several trials must not reuse its execution instances. Configuration sharing remains valid because configurations are reusable descriptions.

## Experiment Run

An `ExperimentRun` is the container and coordinator for several independent `AlgorithmRun` objects. It is not an algorithm run and does not have one search state stream of its own.

The current preferred conceptual shape is:

```csharp
ExperimentRun<TAlgorithm, TKey, ...>
    IReadOnlyList<ExperimentTrial<TAlgorithm, TKey, ...>> Trials

ExperimentTrial<TAlgorithm, TKey, ...>
    TKey Key
    TAlgorithm Algorithm
    AlgorithmRun<...> Run
```

`IExperiment` must retain both `TAlgorithm` and `TKey` in addition to the candidate, search space, problem and search state types. The concrete algorithm type gives analyzer selectors static access to properties such as `Mutator`. The experiment contract exposes `MaterializeCases()` for callers that want to inspect or manually process the typed algorithm and key pairs. Normal experiment execution does not require calling it because `CreateRun(...)` materializes cases internally. An experiment no longer resolves an experiment execution instance through `ExecutionInstanceRegistry`.

Normal execution uses the experiment directly:

```csharp
var run = experiment.CreateRun(problem, random);
var results = await run.CompleteAsync();
```

Manual materialization is an optional inspection and integration API:

```csharp
var cases = experiment.MaterializeCases();
```

Each trial exposes its underlying algorithm run, which owns the assigned mutable random number generator and analyzer results. The trial retains reproducibility metadata describing the deterministic random fork assignment.

Creating an experiment run receives the experiment root random number generator, expands its finite experiment configuration into concrete trial descriptions, assigns deterministic keys and random forks and creates the corresponding algorithm run setup objects without starting them. Algorithm run analyzer states, registries and execution instances remain unresolved until execution starts.

`WithAnalysis(...)` immediately evaluates its selectors against every concrete trial, creates the concrete analyzer configurations and attaches those configurations to the corresponding algorithm run setup. It does not create analyzer states, registries or execution instances. The experiment run must not retain the selector or analyzer factory delegates after attachment. Calling `WithAnalysis(...)` several times before execution remains valid.

Analysis attachment has its own transactional boundary while the experiment run is configurable:

1. Reject the binding immediately if the same binding handle was already attached.
2. Evaluate its selector and analyzer factory for every trial into temporary storage.
3. If any selector or factory call fails, attach nothing from that binding and leave the run configurable.
4. After all concrete analyzer configurations were created successfully, attach the complete set to the child algorithm runs in deterministic trial order.

This transaction occurs during `WithAnalysis(...)`, not during execution preparation, because the materialized run must not retain the binding lambdas.

In this document, materializing an experiment means expanding it into concrete trial and analyzer configurations. Instantiating execution means creating run scoped analyzer states, registries and execution instances. Materialization is eager during setup. Execution instantiation is delayed consistently for algorithm runs and experiment runs.

Internal execution preparation creates analyzer states, registers observations and creates registries from the already concrete analyzer configurations. Combined experiment execution prepares every trial through its ordinary `Stream(...)` method before scheduling begins. Successfully prepared streams are scheduled according to the execution policy. A preparation failure in one trial does not prevent unaffected trials from running and contributes to the final aggregate exception in deterministic trial order.

The caller may then consume the trial runs sequentially, concurrently with bounded concurrency or individually.

An experiment run and each algorithm run are single use. Reusing the experiment configuration creates a fresh experiment run and a fresh set of trials.

Aggregate and individual execution modes must not be mixed. The first execution start determines the mode:

1. Calling `ExperimentRun.Stream(...)`, `Complete(...)` or `CompleteAsync(...)` prepares the stream of every trial and prevents direct execution of its trial runs.
2. Directly starting any trial run prevents later combined execution of the experiment run.
3. Individual mode permits the caller to execute the remaining trial runs independently.
4. Repeated execution of any individual algorithm run still fails through its own single use guard.

The first implementation enforces these rules through each algorithm run's `ExecutionStarted` property and clear `InvalidOperationException` failures. A later type state design may make invalid combinations unavailable through the public types.

## Analyzer Prominence

Analyzers are a central user facing concept. Supplying them only as a trailing constructor or `CreateRun(...)` parameter makes them difficult to discover and understates their role.

The regular run API should provide a prominent attachment style before execution starts. One candidate is a fluent form such as:

```csharp
var run = algorithm.CreateRun(problem, random)
    .WithAnalyzer(bestQuality)
    .WithAnalyzer(genealogy);
```

`WithAnalyzer(...)` mutates the configurable run and returns that same run for fluent composition. Attachment after execution starts is rejected because analyzer observations determine registry replacements.

Analyzers are attached only through the fluent run API. `CreateRun(...)` does not accept analyzers. The first draft may also provide `WithAnalyzers(...)` as a `params` convenience for attaching several analyzers in one call while preserving parameter order.

The regular `AlgorithmRun` remains the owner of concrete analyzer attachments, analyzer states and analyzer results. This is the correct ownership boundary because observations are installed into that run's registry and results describe that one algorithm execution.

Analyzer attachment has three distinct levels:

1. An analyzer configuration describes one analysis.
2. An experiment analyzer binding describes how to select or create an analyzer configuration for each materialized experiment trial.
3. Each child `AlgorithmRun` owns the concrete analyzer state and result created from its attached analyzer configuration.

An experiment run may index and aggregate child analyzer results, but it should not become a second owner of those results.

## Analyzer Binding In Experiments

Operator bound analyzers require a stronger mechanism. A grid may produce algorithm configurations whose observed mutator, evaluator or other child differs by configuration. The analyzer must therefore be selected after each algorithm configuration has been materialized.

The experiment API should support a typed analyzer binding based on the concrete algorithm configuration. `ExperimentAnalysis.ForEach` returns a runtime binding that represents one operator selector and one analyzer factory. It does not attach or execute analyzers immediately.

The exact generic declaration is an implementation detail for the first draft. The conceptual contract is the combination of a selector lambda from the concrete algorithm configuration to exactly one `TOperator` and a factory lambda from that selected operator to one analyzer configuration.

For example:

```csharp
var mutationCalls = ExperimentAnalysis.ForEach(
    algorithm => algorithm.Mutator,
    mutator => new CallCountAnalyzer(mutator));

var run = experiment.CreateRun(problem, random)
    .WithAnalysis(mutationCalls);
```

One binding initially creates one analyzer type. Several analyzers for the same selected operator use several bindings and several `WithAnalysis(...)` calls:

```csharp
var mutationCalls = ExperimentAnalysis.ForEach(
    algorithm => algorithm.Mutator,
    mutator => new CallCountAnalyzer(mutator));

var mutationDuration = ExperimentAnalysis.ForEach(
    algorithm => algorithm.Mutator,
    mutator => new DurationAnalyzer(mutator));

var run = experiment.CreateRun(problem, random)
    .WithAnalysis(mutationCalls)
    .WithAnalysis(mutationDuration);
```

Bindings are applied in attachment order. When several analyzers observe the same operator, all observations must be merged rather than replacing one another. Their observers must be invoked deterministically in attachment order. Focused tests must cover both result production and invocation order for multiple analyses attached to the same operator.

The selector actively navigates from the concrete algorithm configuration to exactly one desired operator. It cannot skip a trial or select several operators. Observing another operator requires another binding. The experiment infrastructure should not automatically traverse the configuration graph and evaluate a boolean filter against every operator.

Names remain provisional. The important requirements are:

1. The selector runs once per materialized experiment trial before its algorithm run starts.
2. It receives the concrete algorithm configuration used by that trial.
3. The selector returns exactly one `TOperator` for each trial.
4. Each analyzer factory receives that operator and produces a concrete analyzer configuration.
5. The produced analyzer configuration is attached only to that algorithm run.
6. Analyzer states and results remain isolated per algorithm run.
7. The binding provides typed keyed result retrieval without requiring users to manually retain every generated analyzer configuration.

The design should support several independent analyzer bindings in one experiment run. An `ExperimentRun` does not offer direct constant analyzer attachment. Every experiment analyzer is created through a binding against each concrete algorithm configuration.

The intended materialization flow is:

```text
experiment configuration
  -> materialized trial with concrete algorithm configuration
  -> create the child AlgorithmRun
  -> evaluate analyzer bindings for that trial
  -> attach the selected analyzers
  -> instantiate analyzer states, registries and execution instances when execution starts
```

An `ExperimentRun` does not need a second analyzer store. Each trial exposes its regular `AlgorithmRun`, which remains the source of truth. `ExperimentTrial` does not forward analyzer result methods. Individual result access uses `trial.Run.GetResult(...)` while binding based aggregate access uses `experimentRun.GetResults(...)`.

Analyzer result availability follows the same lifecycle for algorithm and experiment runs. Results are unavailable before execution starts. Calling `Stream(...)`, `Complete(...)` or `CompleteAsync(...)` eagerly creates analyzer states, so their initial results are available immediately after the execution method returns. During stream enumeration callers may read live results. After completion the same objects contain their final values.

When an analyzer binding creates a different analyzer configuration for every trial, the experiment run retains a mapping from an opaque binding identity and trial key to the concrete analyzer configuration. It does not retain the binding delegates. This enables a typed result query such as:

```csharp
var results = run.GetResults(mutationAnalysis);
```

without requiring the user to manually retain every generated analyzer configuration. Each returned entry should expose its trial, concrete analyzer configuration and typed result value.

### Experiment Result Analysis

Experiment result access must make comparisons and aggregation across trials straightforward. A typed result entry obtained through a binding handle exposes:

1. The `ExperimentTrial` that produced the result.
2. The concrete analyzer configuration attached to that trial.
3. The typed analyzer result value.

This surface must let users use normal typed collection operations to answer at least:

1. Which trial produced the best result.
2. The mean and variance of a measured value.
3. Results grouped by concrete grid algorithm configuration.
4. Results grouped or compared by repetition.

The trial key supplies the grid and repetition identity required by those queries. The initial design does not need dedicated mean, variance or best result methods if the typed result collection makes the corresponding LINQ queries direct and readable.

### Runtime Bindings And Persistable Configurations

Lambdas may be used for temporary setup, selection and runtime binding. They must not remain in the materialized experiment run or its algorithm runs after setup has produced the concrete configuration objects.

Analyzer selector and factory delegates belong to not yet started `ExperimentRun` setup. `WithAnalysis(...)` evaluates them immediately for every concrete trial and converts them into ordinary analyzer configurations attached to the corresponding algorithm runs. The external binding handle may remain a runtime only object containing delegates because it is not retained by the experiment run. The run uses only the handle's opaque identity for later typed result lookup.

Formal serialization requirements should be postponed until the intended serialization boundary has been reconsidered. Runs, execution registries, random number generators, runtime binding handles and active execution instances are not realistic serialization targets. The first implementation should preserve the existing property that algorithm, operator and concrete analyzer configuration objects are serialization friendly in principle. It should not claim that complete experiment setup or execution state is serializable.

Grid construction lambdas should likewise be treated as temporary setup helpers when an experiment run is materialized. The current grid configuration may retain them until `CreateRun(...)` expands the finite grid. The resulting experiment run retains only concrete algorithm configurations and typed keys. Whether the grid configuration itself must become serializable is part of the postponed serialization discussion.

Regular algorithms retain their direct analyzer attachment form because their concrete operator configurations are already available:

```csharp
algorithm.CreateRun(problem, random)
    .WithAnalyzer(new CallCountAnalyzer(algorithm.Mutator));
```

Selector based analyzer attachment should not be added to regular algorithms in the initial implementation. It can be reconsidered if deeply nested algorithm configurations demonstrate a concrete need.

## Experiment Composition

Base experiment forms must be composable. The initial required composition is repeating every configuration in a grid. Possible later compositions include:

1. Comparing several explicitly supplied algorithm configurations.
2. Running configurations across several problem instances.
3. Repeating every algorithm and problem combination.

The current preferred direction is a recursive experiment source model. An experiment may contain another experiment. An algorithm is adapted internally to an experiment source with one leaf run. Experiment combinators expand or transform the leaf run cases, then an experiment run materializes the flattened cases as independent trials.

Conceptually:

```text
Algorithm
  -> single run experiment source
  -> grid expansion
  -> repetition expansion
  -> independent experiment runs
```

Composition must retain stable keys and metadata. Applying repetition to another experiment extends each inner key with a repetition component. Applying a grid retains the selected parameter combination.

The initial implementation should use a simple generic key on each trial. The key describes what distinguishes trials within the particular experiment form:

1. A grid uses its concrete algorithm configuration as the key because that configuration identifies the selected parameter values.
2. Repeating one algorithm uses the repetition index as the key because every trial has the same algorithm configuration.
3. Repeating an experiment extends each inner key with the repetition index.

Repeating a grid therefore produces:

```csharp
(TAlgorithm Algorithm, int Repetition)
```

For example, a grid producing configurations `A`, `B` and `C` followed by `Repeat(2)` materializes these trial keys in deterministic grid first order:

```text
(A, 0)
(A, 1)
(B, 0)
(B, 1)
(C, 0)
(C, 1)
```

Each trial retains the same concrete algorithm configuration in its `Algorithm` property and owns an independent algorithm run. The structural random fork path follows the same composition, such as grid position followed by repetition index, without using the algorithm configuration hash code.

The implementation should work through a typed intermediate trial description containing the concrete algorithm configuration, current key and immutable structural random fork path. `AsGrid(...)` produces descriptions keyed by algorithm configuration. `Repeat(...)` transforms every inner description by appending a repetition dimension to its `ImmutableArray<int>` fork path. `ExperimentRun` finally turns those descriptions into algorithm run setup objects.

The generic experiment composition contract is:

```text
IExperiment<..., TAlgorithm, TInnerKey>.Repeat(repetitions)
  -> IExperiment<..., TAlgorithm, (TInnerKey Inner, int Repetition)>
```

The direct algorithm convenience overload omits the constant algorithm key and uses only the repetition index. When the inner experiment is a grid, `TInnerKey` is `TAlgorithm`, so the composed key is the desired algorithm configuration and repetition pair. This transformation must happen internally without requiring the user to construct or project keys.

API usage specs and focused tests must prove all three key shapes:

```csharp
algorithm.Repeat(10);                  // int
algorithm.AsGrid(...);                 // TAlgorithm
algorithm.AsGrid(...).Repeat(10);      // (TAlgorithm, int)
```

The concrete algorithm remains a separate strongly typed trial property even when it also serves as the grid key. It is not hidden inside an untyped tag. A richer path abstraction should be considered only after the first implementation demonstrates concrete navigation or formatting needs.

Grid materialization rejects equal generated algorithm configurations because using the algorithm configuration as the key would otherwise produce duplicate trial identity. Repeating the same configuration intentionally uses `Repeat(...)`, which adds the repetition index. This duplicate behavior may be reconsidered later. Random forks use deterministic structural positions rather than algorithm hash codes.

The initial experiment forms are:

1. `GridExperiment`
2. `RepeatedExperiment`

`RepeatedExperiment` can wrap one algorithm or another experiment. An explicit algorithm comparison experiment is deferred until its user purpose, heterogeneous algorithm typing and analyzer selection model are clear. A later `BenchmarkExperiment` may combine algorithms, problem instances and repetitions.

## Problems And Initial States

The initial implementation passes the same problem object to every trial. The shared problem is expected to be immutable or safe for concurrent use.

Separate execution registries do not isolate the shared problem object. Dynamic problems, stateful problem instances and problems backed by mutable external resources may therefore require a future per trial problem selector or factory.

The same rule applies to initial search states. The first implementation should retain the simplest current behavior. Per trial production of mutable initial states may be added with the future problem creation capability.

## Randomness

Every algorithm run owns the random number generator it will use. `CreateRun(...)` receives that generator. An experiment run owns the experiment root generator and gives every child algorithm run a deterministic fork. Random assignment must not depend on sequential or concurrent scheduling.

Requirements are:

1. Stable experiment trial ordering or an explicit structured fork path.
2. Separate forks for nested dimensions such as grid configuration and repetition.
3. Identical child random sequences under sequential, concurrent and bounded parallel execution.
4. No accidental consumption of a child random number generator before its run starts.
5. Retained seed or fork metadata sufficient to reproduce one child run.

Using object hash codes as fork keys is not acceptable because keys and hash implementations may not be stable.

## Scheduling And Streaming

An experiment run creates trials but does not prescribe one scheduling policy. The API must expose both individual trial streams and a convenient combined stream.

### Individual Trial Streams

The trial collection is the fundamental streaming surface:

```csharp
foreach (var trial in experimentRun.Trials)
{
    await foreach (var state in trial.Run.Stream(cancellationToken))
    {
    }
}
```

The caller controls the outer scheduling and exception handling. If one trial stream throws, the caller may catch that exception and continue with the next trial. Callers may also consume selected trial streams concurrently.

### Combined Experiment Stream

`ExperimentRun.Stream(...)` is a convenience view that schedules and combines trial streams. It yields a simple deconstructable record containing the trial and yielded search state:

```csharp
public sealed record ExperimentStreamEntry<TTrial, TSearchState>(TTrial Trial, TSearchState State);
```

The concrete generic declaration may use the experiment type parameters directly rather than a generic `TTrial`. The important public shape is the `Trial` and `State` pair.

```csharp
await foreach (var entry in experimentRun.Stream(policy, cancellationToken))
{
    Console.WriteLine(entry.Trial.Algorithm);
    Console.WriteLine(entry.Trial.Key);
    Console.WriteLine(entry.State);
}
```

Callers may also deconstruct entries directly:

```csharp
await foreach (var (trial, state) in experimentRun.Stream(policy, cancellationToken))
{
}
```

The combined stream is not the only way to stream an experiment. This distinction is important because an async iterator ends permanently when it throws. Independent trial streams allow per trial exception handling that a single interleaved stream cannot provide after it has surfaced an exception.

The scheduling API should support:

1. Sequential execution.
2. Concurrent execution of all runs.
3. Bounded parallel execution.
4. Manual execution and progress monitoring of selected runs.

An execution policy parameter is preferred over separate method names such as `StreamSequentially` and `StreamInParallel`. A small policy API can provide sequential, unbounded and bounded concurrency without exposing magic integer values:

```csharp
ExperimentExecutionPolicy.Sequential()
ExperimentExecutionPolicy.Concurrent()
ExperimentExecutionPolicy.Concurrent(4)
```

The default policy should be sequential. Maximum concurrency describes concurrent algorithm runs rather than worker threads.

Each algorithm run keeps its own stream. Every combined stream entry contains its trial, which provides the key and algorithm configuration. Parallel combined event order is schedule dependent and should not be described as reproducible.

Scheduling must not change the random sequence or optimization result of any individual run.

Materialization order is the canonical deterministic order for all nonstreaming collections. `ExperimentRun.Trials`, successful completion tuples, binding based analyzer results and exceptions inside the final `AggregateException` retain that order regardless of scheduling. Parallel combined stream emission order remains schedule dependent, but every emitted entry retains its deterministic trial and key identity.

## Completion, Cancellation And Failure

An experiment level cancellation token stops scheduling pending trials, cancels all active trials and throws cancellation. This is cancellation rather than resumable pausing. Callers can use separate cancellation tokens when they execute trial runs individually.

Failures should use normal exception handling in the initial implementation. Exceptions should not be retained as inspectable experiment outcome objects.

Individual trial streams throw independently. A caller enumerating the trials can catch one trial failure and continue with later trials.

The combined `ExperimentRun.Stream(...)` catches failures from individual trial streams, allows unaffected trials to finish and emits their remaining progress. After all scheduled trials have finished, it throws an `AggregateException` whose inner exceptions identify the failed trials:

```csharp
public sealed class ExperimentTrialException<TKey> : Exception
{
    public TKey Key { get; }
}
```

Each `ExperimentTrialException<TKey>` wraps the original failure as its inner exception and exposes the failed trial key. The aggregate contains these exceptions in deterministic trial materialization order regardless of scheduling. This applies to failures during trial preparation and execution. Exceptions are retained only until the combined operation finishes and do not become public inspectable experiment outcomes.

`ExperimentRun.Complete(...)` and `CompleteAsync(...)` use the same policy. They allow unaffected trials to finish and throw an aggregate exception when combined execution finishes.

Successful experiment completion does not introduce an `ExperimentTrialCompletion` type because ordinary algorithm completion returns its final search state directly. It returns a read only collection of trial and final state tuples:

```csharp
IReadOnlyList<(ExperimentTrial<TKey, ...> Trial, TSearchState State)>
```

Regular algorithm completion uses `LastAsync(...)` and therefore throws when an algorithm yields no state. Experiment completion should preserve that behavior. A trial that yields no state contributes a failure to the aggregate exception. If combined completion throws, no completion collection is returned. Successful algorithm runs and their analyzer results remain available through the experiment run and its trials after the caller handles the exception.

## Stable Identity And Metadata

Every trial needs a stable unique key separate from object reference identity. A trial should retain:

1. Its key.
2. Its algorithm configuration.
3. Its problem or problem identity.
4. Its repetition and parameter metadata where applicable.
5. Its random fork metadata.

Repetition indices are natural keys for a repeated experiment. A grid uses the concrete generated algorithm configuration as its key because that configuration records the selected parameter values.

A run or execution path may become useful for more deeply composed experiments. Each composition layer could append one path segment, such as a grid parameter choice, repetition index, problem instance or explicit comparison label.

The initial generic trial key and typed tuple composition should be preferred until it proves insufficient. A future path must not expose an untyped `object` value that forces callers to cast or perform manual type checks. Possible later directions include a closed hierarchy of typed path segment records or generic segment implementations behind a non generic path interface with typed retrieval helpers. The algorithm configuration itself should remain available through the strongly typed trial rather than being hidden only inside an untyped path tag.

Identity and metadata may need to remain separate. The path can provide stable hierarchical identity while the trial exposes strongly typed algorithm, problem and experiment data.

## Validation

The experiment model should validate at least:

1. Positive repetition counts.
2. At least one materialized experiment trial.
3. Unique run keys.
4. Valid nonempty grid dimensions.
5. Stable deterministic case materialization.

Focused lifecycle tests must cover:

1. Analyzer attachment while configuring.
2. Analyzer attachment after every execution start form being rejected.
3. Calling `Stream(...)` freezing configuration even before enumeration starts.
4. `ExecutionStarted` changing when execution begins and remaining true after every completion outcome.
5. Individual trial execution preventing later combined execution.
6. Combined execution preventing direct trial execution.
7. Remaining trials being independently executable after individual execution starts.
8. Multiple analyses of the same operator all executing in attachment order.
9. A failing selector or analyzer factory attaching no partial analysis configuration.
10. Attaching the same binding handle twice being rejected.
11. Completion results, analyzer results and aggregate failures retaining materialization order under parallel execution.
12. Every combined failure being wrapped in an `ExperimentTrialException<TKey>` with the correct trial key and original inner exception.

## Current Implementation Problems To Remove

The migration must remove these current patterns:

1. Experiment creation through one `ExecutionInstanceRegistry`.
2. `null!` run placeholders in experiment convenience methods and usage specs.
3. Raw keyed streams that hide the algorithm runs and analyzer results.
4. Algorithm terminology in `GridAlgorithm` and `RepeatAlgorithm`.
5. Recursive interleaved repeat extension calls.
6. Incorrect use of the stream index as the per run iteration index.
7. Streaming surfaces that do not distinguish individual trial streams from the optional combined experiment stream.

## Deferred Design Topics

No remaining conceptual decision blocks the first draft. Exact generic declarations may be chosen during implementation as long as they preserve the documented statically typed API.

Formal serialization guarantees, richer run paths, individual cancellation handles, per trial problem factories, per trial initial state factories and an explicit heterogeneous algorithm comparison experiment are deferred design topics.

## Initial Migration Boundary

The first implementation should focus on the common execution foundation and migrate repeated and grid execution. It should not redesign algorithm builders, dynamic racing or unrelated experimental operators.

Documentation and API usage specs should present experiments as orchestration over independent runs rather than multi stream algorithms.
