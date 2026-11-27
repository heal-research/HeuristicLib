# Layering

HeuristicLib organizes dependencies by responsibility. A higher layer may use a lower layer. Lower-layer implementations must not take on higher-layer responsibilities. Types in the same layer may collaborate. Folders and assemblies help readers find code, but neither defines a type's layer. The architecture test checks these rules against source references.

## Layers and ownership

<LayerStack>

A higher layer may use a lower one, never the reverse. Types in one layer may collaborate. The table lists the full contents of each layer.

</LayerStack>

| Layer                 | Responsibility                                                  | Types and concepts                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| --------------------- | --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| L0: domain vocabulary | Values and concepts independent of configured execution graphs. | Search spaces, candidate invariants, objectives, randomness, collections and numerics. Problem contracts and representation-independent problem bases. Shared execution concurrency helpers.                                                                                                                                                                                                                                                                                                                                                  |
| L1: composition model | Contracts and authoring bases for configurations and executors. | `IConfigurationNode`, `IExecutionNode`, `IExecutor`, `IAdaptation` and `Adaptation<TAdapted>` (in `Operators`, grouped in the `Operators/Adaptation` folder), `ExecutionSignature`, validation, role interfaces and authoring bases, including wrapper bases and `INamedConfiguration`. Operator contract composition and verification.                                                                                                                                                                                                       |
| L2: resolution        | Managing run-bound results and constructing executor graphs.    | `ResolutionScope`, `RunResults`, `ISubstitution`, `Substitution` and `SubstitutionGroup` in `Execution`.                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| L3: library content   | Concrete behavior and predefined inspection.                    | Scale and offset adaptation configurations and runtime objects in `Operators/Adaptation`. Instantiable algorithm and operator configurations, encodings and concrete problems. Operation observation records and sources beside each operation. Algorithm observation, `ObservationSource<TObservation>`, `IObservationReceiver<T>`, `Observing`, counters, durations, counting and timing wrappers, clock configurations (`CounterClock`, `DurationClock`, `ElapsedClock`), their `IClockReading` run results and factories in `Inspection`. |
| L4: hosting           | Starting and coordinating runs and their lifecycles.            | `AlgorithmRun`, `ExecutionStream` and `RunLifecycleState` in `Execution/Runs`. Experiments, trials, `ExperimentRun` in `Experiments`.                                                                                                                                                                                                                                                                                                                                                                                                         |
| L5: analysis          | Deriving and publishing results from observations.              | `Analyzer<TResult>`, `AggregateAnalyzer`, traces, ranking, best-so-far and retention in `Analysis`.                                                                                                                                                                                                                                                                                                                                                                                                                                           |

`Algorithms` and `Operators` contain both composition contracts and concrete content. `Execution` contains domain concurrency helpers, composition contracts, resolution mechanisms and hosting types. `Inspection` contains only library content: counters, durations and their accumulators, algorithm observation, observation sources and `Observing`, clock contracts (`IClock` and `IClock<TTime>`), configurations and their `IClockReading<TTime>` run results, and convenience factories. Classify the named type by its responsibility.

An encoding is a vertical slice through one representation: candidate, search space, operators and recommendations. All types in `Encodings` are library content. Domain vocabulary remains independent of concrete representations.

Construction hooks bridge authoring contracts to resolution. A configuration's construction hook (`CreateExecutor` for operators and algorithms, `CreateExecutionNode` for other nodes) receives a `ResolutionScope` for children and typed result lookup. Hosting creates an independent root scope for each run. Configurations never own resolution storage or hosting lifecycle.

Generic operator contracts, operations, the operator wrapper base and the routed-executor base `RoutingExecutor<TExecutor>`, beside `WeightedRouter<TChoice>`, live in `Operators`. `OperationInfo`, the invocation wrapper contracts and the wrapping-executor base `WrappingExecutor<TExecutor, TWrapper>` live in `Execution` as composition machinery. Executors own private mutable fields. The run result table binds result-producing configurations by reference. Inspection owns concrete metrics and observations. Neither resolution nor hosting depends on Analysis.

Experimental can contain content, hosting or analysis at the corresponding layers and depend on the main library. The main library must not depend on Experimental. PythonInterop is an integration client above both libraries.

## Dependency rules

- Domain vocabulary references only domain vocabulary.
- Hosting may depend on domain vocabulary, composition contracts and resolution. It must not reference `Analysis` or concrete library algorithms.
- Operators, algorithms, encodings and problems may use lower-layer contracts and shared helpers. Neither their content nor `Inspection` and resolution may reference `Analysis`. Observation sources and `Observing` in `Inspection` are available to content, such as a meta-algorithm that tracks its contenders.
- Analysis may use content and hosting conveniences. Analysis implements result-producing configuration nodes through the composition contracts. Inspection owns the observation interface and callback groups, so neither content nor resolution needs an Analysis dependency.

## Layers and execution phases

Configuration and executor are phases within these responsibilities. Configurations describe reusable settings, children and analyzers. Executors hold mutable fields and resolved children. [Execution nodes](/contributing/architecture/execution-nodes) defines what each phase owns. Substitutions rewrite a configuration before resolution, and `CreateRun` can place a list of them for one run before validation ([Substitution and ordering](/contributing/architecture/execution-resolution#substitution-and-ordering)).

## Run ownership

`AlgorithmRun`, `ExecutionStream` and `RunLifecycleState` belong to the `HEAL.HeuristicLib.Execution` namespace, with their source files grouped under `Execution/Runs`. Experiment runs remain in `HEAL.HeuristicLib.Experiments` and share `RunLifecycleState`.

Create each run from its configured algorithm. There is no separate setup or supply phase. `AlgorithmRun` owns its lifecycle and retained execution enumerator together. It is returned in `Ready`, resolves its executor graph on first start and retains that graph and its iterator across pauses: a paused run retains the enumerator for resumption, and a running stream has exclusive access to it. Completion or failure is terminal, and results remain readable. See [running algorithms](/guide/execution/running-algorithms) for the lifecycle contract.

`ExecutionStream` adapts synchronous and asynchronous enumeration and enforces single consumption: each returned stream has one consumer, and a run permits only one active stream. Stream cancellation is cooperative at root-algorithm yield boundaries. It pauses the run instead of interrupting a partially executed iteration.

`ExperimentRun` owns trial scheduling and its terminal lifecycle. It rejects starting an experiment whose trial has already been started directly.

The runs share lifecycle state names but have different transition rules. Keep those rules and diagnostics with the run that owns the execution or scheduling. Extract a shared component only when it can own a concrete shared responsibility. Grouping composition fields or moving state transitions behind a configurable host does not establish one.

## Concept placement

Namespaces group concepts that users work with together, even when their types belong to different layers. `SearchSpaces` keeps `IOperatorContract` beside its composition, verification and compatibility helpers. `Data` serves symbolic expression interpretation, machine learning and Python integration, so it remains independent of `MachineLearning`. `Collections` remains the home of shared collection vocabulary even with a single public type.

The main benchmark functions share `Problems.TestFunctions`. `SingleObjectives`, `ZDT` and `MetaFunctions` are grouping folders. Experimental's `Problems.TestFunctions.BBoB` is a distinct benchmark suite with different implementations, including types whose names overlap the main functions. Python integration uses the single `HEAL.HeuristicLib.PythonInterop` namespace with `SymbolicRegression`, `Experiments` and `Analysis` grouping folders. These follow the [namespace and folder rules](/contributing/developer-guidelines#§-9-5-match-namespaces-and-folders-by-audience).
