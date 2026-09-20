using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static partial class Analyzer
{
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IAlgorithm<TCandidate, TSearchState> algorithm,
        IMeasurement<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace([algorithm], measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms,
        IMeasurement<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        CreateAggregating<IAlgorithm<TCandidate, TSearchState>, AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TProblem, TValue, TResult>(algorithms, measurement, aggregation,
            static (source, record) => new AlgorithmObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(source, record),
            clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IAlgorithm<TCandidate, TSearchState> algorithm,
        Func<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace(algorithm, new DelegateMeasurement<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TResult>(
        IAlgorithm<TCandidate, TSearchState> algorithm,
        Func<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace([algorithm], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TResult>(
        IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms,
        Func<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        CreateProjected(algorithms, value,
            static (source, record) => new AlgorithmObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(source, record),
            clocks, retention);

    // Observes at the interface search space and problem, so an implicitly typed lambda needs no type arguments.

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TValue, TResult>(
        IAlgorithm<TCandidate, TSearchState> algorithm,
        Func<AlgorithmObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchState : class, ISearchState =>
        Trace(algorithm, new DelegateMeasurement<AlgorithmObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TResult>(
        IAlgorithm<TCandidate, TSearchState> algorithm,
        Func<AlgorithmObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchState : class, ISearchState =>
        Trace([algorithm], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TResult>(
        IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms,
        Func<AlgorithmObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchState : class, ISearchState =>
        CreateProjected(algorithms, value,
            static (source, record) => new AlgorithmObservationHook<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>(source, record),
            clocks, retention);
}
