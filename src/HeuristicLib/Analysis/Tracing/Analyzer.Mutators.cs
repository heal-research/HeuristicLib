using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static partial class Analyzer
{
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IOperator<Mutation, TCandidate> mutator,
        IMeasurement<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace([mutator], measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IReadOnlyList<IOperator<Mutation, TCandidate>> mutators,
        IMeasurement<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        TraceAnalyzer<TResult>.Create<IOperator<Mutation, TCandidate>, MutatorObservation<TCandidate, TSearchSpace, TProblem>, TProblem, TValue>(mutators,
            static (source, record) => Aspect.Observe(source, record),
            measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IOperator<Mutation, TCandidate> mutator,
        Func<MutatorObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(mutator, new DelegateMeasurement<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IReadOnlyList<IOperator<Mutation, TCandidate>> mutators,
        Func<MutatorObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(mutators, new DelegateMeasurement<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TResult>(
        IOperator<Mutation, TCandidate> mutator,
        Func<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace([mutator], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TResult>(
        IReadOnlyList<IOperator<Mutation, TCandidate>> mutators,
        Func<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        TraceAnalyzer<TResult>.Create(mutators,
            static (source, record) => Aspect.Observe(source, record),
            value, clocks, retention);

    // Observes at the interface search space and problem, so an implicitly typed lambda needs no type arguments.

    public static TraceAnalyzer<TResult> Trace<TCandidate, TValue, TResult>(
        IOperator<Mutation, TCandidate> mutator,
        Func<MutatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
        Trace(mutator, new DelegateMeasurement<MutatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TValue, TResult>(
        IReadOnlyList<IOperator<Mutation, TCandidate>> mutators,
        Func<MutatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
        Trace(mutators, new DelegateMeasurement<MutatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TResult>(
        IOperator<Mutation, TCandidate> mutator,
        Func<MutatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null) =>
        Trace([mutator], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TResult>(
        IReadOnlyList<IOperator<Mutation, TCandidate>> mutators,
        Func<MutatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null) =>
        TraceAnalyzer<TResult>.Create(mutators,
            static (source, record) => Aspect.Observe(source, record),
            value, clocks, retention);
}
