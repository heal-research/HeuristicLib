# Defining operations

Define an operation when no role fits the step your algorithm needs. This page defines an `Estimation` operation, which predicts objective values without evaluating, and a `ScaledEstimator` that performs it.

## The parts of an operation

An operation has an executor interface with one named method, an operation type that implements `IOperation<TSelf>` and wraps that executor, and typed resolution extensions. Operators then implement `IOperator<TOperation, TCandidate>` for it, and naming, retry, choice, counting, timing and custom wrappers work without further code. A built-in operation is defined the same way.

The named method ends with the call's random number generator. The executor received its problem during construction, so the call carries neither search space nor problem:

<<< ../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-executor{csharp}

The operation implements the static members of `IOperation<TSelf>` explicitly. It recognizes its executor interface, wraps an executor in a private wrapping executor, and holds a private `Invocation` struct for one call. An item operation, marked with `IItemOperation<TSelf>`, also routes a call's items to several executors through a private routed executor. The library bases `WrappingExecutor<TExecutor, TWrapper>` and `RoutingExecutor<TExecutor>` hold the executors, the wrapper and the router, type them as the operation's executor interface:

<<< ../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-type{csharp}

The invocation is what every wrapper receives: its `Random` and `Proceed`. An operation whose calls do not split into items implements `IOperation<TSelf>` instead and needs no routed executor. An operation with candidate work returns true from `SupportsCandidateWork` and implements `CountCandidateWork` on its invocation. The static members, the invocation and the two executors stay per operation: C# needs a nominal class for each executor interface, a struct has no base class, and the operation's static members are the only place that names the executor interface for the run's types.

Resolution extensions cast the resolved executor to the operation's interface. Creation, crossover, evaluation, mutation, refinement, replacement and selection each declare one block for the untyped scope with an explicit problem and one for `ResolutionScope<TCandidate, TSearchSpace, TProblem>`. The typed block also serves four-argument scopes through their base class. Resolution uses the scope's search state even when a four-argument scope is held as a three-argument scope. A scope without a search-state argument uses `ISearchState`. Termination and interception retain their four-argument blocks because their executor interfaces name the search state.

`TryResolve` calls `TryResolveOperator`, which takes the interface from the executor variable and reports a failure as a reason:

<<< ../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-resolution{csharp}

## Make the operation observable

An operation is observable once it declares two parts beside the operation, as each built-in operation does: an observation record, which implements `IInvocationObservation<TOperation, TCandidate, TSelf>` to make an executor report it, and an `Invocations` source. The record carries the call's inputs and outputs, with each batch snapshotted as a `ValueArray<T>`, together with the search space and problem its executor was bound to, so a trace can rank by that problem's objective:

<<< ../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-observation{csharp}

`Analyzer.Trace(estimator.Invocations, observation => observation.Estimates.Count)` and `estimator.Invocations.Observing(…)` then work as they do for built-in operations. The `Observed` form takes an `IObservationReceiver<TObservation>` in its `receiver` slot and binds that receiver's recorder when constructed. The observation record's `Wrap<TSearchState>` hook receives the bound problem as `IProblem<TCandidate, ISearchSpace<TCandidate>>`. The search-state type lets roles such as termination name their executor interface. The hook casts to the operation's executor interface after resolution has checked that the executor performs the operation.

## Next steps

- [Write an operator](/extend/writing-operators) for the operation.
- [Observations and clocks](/extend/observations-and-clocks) for analyzers over the operation's observation record.
- [Analyzer architecture](/contributing/architecture/analyzers) for how observation records and sources are built.
