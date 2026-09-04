using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public abstract class Clock
{
    private protected Clock() { }

    internal abstract object Read();
    internal abstract void Install(ExecutionInstanceResolverBuilder builder);

    public static Clock<long> FromIterations<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new IterationClock<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

    public static Clock<long> FromEvaluations<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new EvaluationClock<TCandidate, TSearchSpace, TProblem>(evaluator);

    public static Clock<TimeSpan> FromElapsedTime(TimeProvider timeProvider) => new ElapsedTimeClock(timeProvider);
}

public abstract class Clock<TTime> : Clock
{
    private protected Clock() { }

    internal abstract TTime ReadTime();
    internal sealed override object Read() => ReadTime()!;
}

internal sealed class Moment
{
    private readonly ImmutableDictionary<Clock, object> times;

    private Moment(ImmutableDictionary<Clock, object> times)
    {
        this.times = times;
    }

    internal TTime At<TTime>(Clock<TTime> clock)
    {
        if (!times.TryGetValue(clock, out var time))
            throw new KeyNotFoundException("The moment does not contain a reading from the requested clock.");
        return (TTime)time;
    }

    internal static Moment Read(IEnumerable<Clock> clocks)
    {
        var times = ImmutableDictionary.CreateBuilder<Clock, object>(ReferenceEqualityComparer.Instance);
        foreach (var clock in clocks)
            times.Add(clock, clock.Read());
        return new Moment(times.ToImmutable());
    }
}

file sealed class IterationClock<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
    : Clock<long>, IObservationRecorder<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private long latest;

    internal override long ReadTime() => Interlocked.Read(ref latest);

    internal override void Install(ExecutionInstanceResolverBuilder builder) => builder.Observe(algorithm, this);

    public void Record(AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState> observation) =>
        Interlocked.Exchange(ref latest, observation.Iteration);
}

file sealed class EvaluationClock<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
    : Clock<long>, IObservationRecorder<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private long evaluations;

    internal override long ReadTime() => Interlocked.Read(ref evaluations);

    internal override void Install(ExecutionInstanceResolverBuilder builder) => builder.Observe(evaluator, this);

    public void Record(EvaluatorObservation<TCandidate, TSearchSpace, TProblem> observation) =>
        Interlocked.Add(ref evaluations, observation.ObjectiveVectors.Count);
}

file sealed class ElapsedTimeClock(TimeProvider timeProvider) : Clock<TimeSpan>
{
    private long startedAt;

    internal override TimeSpan ReadTime() => timeProvider.GetElapsedTime(Interlocked.Read(ref startedAt));

    internal override void Install(ExecutionInstanceResolverBuilder builder) =>
        Interlocked.CompareExchange(ref startedAt, timeProvider.GetTimestamp(), 0);
}
