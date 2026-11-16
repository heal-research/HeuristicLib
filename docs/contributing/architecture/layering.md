# Layering

HeuristicLib organizes dependencies by responsibility. A higher layer may use a lower layer; lower-layer implementations must not take on higher-layer responsibilities. Types in the same layer may collaborate. Folders and assemblies help readers find code, but neither defines a type's layer. The architecture test checks these rules against source references.

## Layers and ownership

| Layer                 | Responsibility                                                  | Types and concepts                                                                                                                                                                                                                                                                                                                                                                                              |
| --------------------- | --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| L0: domain vocabulary | Values and concepts independent of configured execution graphs. | Search spaces, candidate invariants, objectives, randomness, collections and numerics; problem contracts and representation-independent problem bases; shared execution concurrency helpers.                                                                                                                                                                                                                    |
| L1: composition model | Contracts and authoring bases for configurations and executors. | `IConfigurationNode`, `IExecutionNode`, `IExecutor`, `IAdaptation` and `Adaptation<TAdapted>` (in `Operators`, grouped in the `Operators/Adaptation` folder), `ExecutionSignature`, validation, role interfaces and authoring bases, including wrapper bases and `INamedConfiguration`; operator contract composition and verification.                                                                         |
| L2: resolution        | Managing run-bound results and constructing executor graphs.    | `ResolutionScope`, `Instrument<TResult>`, `RunResults` and `Substitution` in `Execution`.                                                                                                                                                                                                                                                                                                                       |
| L3: library content   | Concrete behavior and predefined inspection.                    | Scale and offset adaptation configurations and runtime objects in `Operators/Adaptation`; instantiable algorithm and operator configurations, encodings and concrete problems; operation observation records and sources beside each operation; algorithm observation, `ObservationSource<TObservation>`, `Observing`, counters, durations, counting and timing wrappers, clocks and factories in `Inspection`. |
| L4: hosting           | Starting and coordinating runs and their lifecycles.            | `AlgorithmRun`, `ExecutionStream` and `RunLifecycleState` in `Execution/Runs`; experiments, trials, `ExperimentRun` in `Experiments`.                                                                                                                                                                                                                                                                           |
| L5: analysis          | Deriving and publishing results from observations.              | Analyzers, traces, ranking, best-so-far and retention in `Analysis`.                                                                                                                                                                                                                                                                                                                                            |

`Algorithms` and `Operators` contain both composition contracts and concrete content. `Execution` contains domain concurrency helpers, composition contracts, resolution mechanisms and hosting types. `Inspection` contains only library content: counters, durations and their accumulators, algorithm observation, observation sources and `Observing`, clocks and convenience factories. Classify the named type by its responsibility.

An encoding is a vertical slice through one representation: candidate, search space, operators and recommendations. All types in `Encodings` are library content. Domain vocabulary remains independent of concrete representations.

Construction hooks bridge authoring contracts to resolution. A configuration's construction hook (`CreateExecutor` for operators and algorithms, `CreateExecutionNode` for other nodes) receives a `ResolutionScope` for children and typed result lookup. Hosting creates an independent root scope for each run. Configurations never own resolution storage or hosting lifecycle.

Generic operator contracts, operations, the operator wrapper base and the routed-executor base `RoutingExecutor<TExecutor>`, beside `WeightedRouter<TChoice>`, live in `Operators`. `OperationInfo`, the invocation wrapper contracts and the wrapping-executor base `WrappingExecutor<TExecutor, TWrapper>` live in `Execution` as composition machinery. Executors own private mutable fields. The run result table binds instrument definitions by reference. Inspection owns concrete metrics and observations. Neither resolution nor hosting depends on Analysis.

Experimental can contain content, hosting or analysis at the corresponding layers and depend on the main library. The main library must not depend on Experimental. PythonInterop is an integration client above both libraries.

## Dependency rules

- Domain vocabulary references only domain vocabulary.
- Hosting may depend on domain vocabulary, composition contracts and resolution. It must not reference `Analysis` or concrete library algorithms.
- Operators, algorithms, encodings and problems may use lower-layer contracts and shared helpers. Neither their content nor `Inspection` and resolution may reference `Analysis`. Observation sources and `Observing` in `Inspection` are available to content, such as a meta-algorithm that tracks its contenders.
- Analysis may use content and hosting conveniences. Instruments belong to resolution; Analysis implements result-producing definitions without creating a dependency from hosting or resolution to Analysis.

## Layers and execution phases

Configuration and executor are phases within these responsibilities. Configurations describe reusable settings, children and instrument definitions. Executors hold mutable fields and resolved children. `AnalyzedBy` places analyzers in the configuration and `Substitute` places other wrappers; both return a new configuration before resolution.

## Run ownership

`AlgorithmRun` owns its lifecycle and retained execution enumerator together: a paused run retains the enumerator for resumption, and a running stream has exclusive access to it. `ExecutionStream` adapts synchronous and asynchronous enumeration and enforces single consumption. `ExperimentRun` owns trial scheduling and its terminal lifecycle; it rejects starting an experiment whose trial has already been started directly.

The runs share lifecycle state names but have different transition rules. Keep those rules and diagnostics with the run that owns the execution or scheduling. Extract a shared component only when it can own a concrete shared responsibility; grouping composition fields or moving state transitions behind a configurable host does not establish one.

## Concept placement

Namespaces group concepts that users work with together, even when their types belong to different layers. `SearchSpaces` keeps `IOperatorContract` beside its composition, verification and compatibility helpers. `Data` serves symbolic expression interpretation, machine learning and Python integration, so it remains independent of `MachineLearning`. `Collections` remains the home of shared collection vocabulary even with a single public type.

The main benchmark functions share `Problems.TestFunctions`; `SingleObjectives`, `ZDT` and `MetaFunctions` are grouping folders. Experimental's `Problems.TestFunctions.BBoB` is a distinct benchmark suite with different implementations, including types whose names overlap the main functions. Python integration uses the single `HEAL.HeuristicLib.PythonInterop` namespace with `SymbolicRegression`, `Experiments` and `Analysis` grouping folders. These follow the [namespace and folder rules](/contributing/developer-guidelines#§-9-5-match-namespaces-and-folders-by-audience).
