# Observations and clocks

The [analysis tools](/guide/execution/observability-and-analysis) cover traces, best-so-far analysis, counters and durations. This page writes an analyzer that combines observation types and a clock of your own. To make an operation of your own observable, see [Make the operation observable](/extend/defining-operations#make-the-operation-observable).

## An analyzer over several observation types

An analyzer that combines different observation types derives from `Analyzer<TResult>` and implements `IObservationReceiver<TObservation>` once for each type. It needs three parts:

- `CreateSubstitutions` places one observation wrapper per boundary with `source.RecordingInto(this)`.
- `CreateExecutionNode` creates an empty result for each run. The result implements `IRunResult` and synchronizes its own updates.
- Each `CreateRecorder` looks up the run's result with `results.Result(this)` and returns the callback for its observation type.

This analyzer counts the candidates the mutator produces in each yielded iteration. Mutation observations add to a pending count, and each iteration observation closes it:

<<< ../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-mixed-analyzer{csharp}

Attach it like any other analyzer and read its result after the run:

<<< ../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-mixed-analyzer-usage{csharp}

The first iteration is the initial population, which the mutator has not touched, so its count is zero. `run.Result` looks a result up by the analyzer reference, so the analyzer is a class and keeps no collected data itself. The data lives in the result, which keeps independent runs, including concurrent ones, apart. [Analyzer architecture](/contributing/architecture/analyzers#accumulating-analyzers) describes the library's own analyzers of this kind, such as the Experimental genealogy analyzer.

## A clock of your own

A custom clock implements `IClock<TTime>`. Its typed `CreateExecutionNode(ResolutionScope)` creates a reading for the run. The boxed construction delegates to it. The root owns one reading per clock instance and run. Its `Read()` may be called concurrently and must preserve the observed computation.

Typed reads such as `At(clock)` and `By(clock)` infer `TTime` through `IClock<TTime>`. Mixed clock lists use `IReadOnlyList<IClock>`, and `results.Result(clock)` infers its type through `IClock`. Through `IClock<TTime>` it needs an explicit result type because the typed interface has two configuration-node constructions.

A clock only reads. If it also needs wrappers to update its value, it implements `ISubstitution`, and the caller lists it after the analyzers that read it. A clock that observes calls holds a private `Counter`: its recorder increments the run's accumulator, and its reading reads that same root accumulator. It places an `Observed` wrapper for itself through a `SubstitutionGroup`. Traces and best-so-far analyzers place no clocks. `Clock.Elapsed` needs no placement.

## Next steps

- [Analyzer architecture](/contributing/architecture/analyzers) for how analyzers, recorders and run results fit together.
- [Write wrappers](/extend/writing-wrappers) for wrappers that act on calls rather than observe them.
