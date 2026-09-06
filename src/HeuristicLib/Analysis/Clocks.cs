using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// One typed notion of time a trace can record its entries against.
/// </summary>
/// <remarks>
/// Derive from <see cref="Clock{TTime}"/> to add a clock of your own, such as one over a domain quantity the library
/// knows nothing about. The factory methods here cover the times every run has.
/// </remarks>
public abstract class Clock
{
    private protected Clock() { }

    internal abstract object Read();

    /// <summary>
    /// Declares whatever keeps this clock's time current, if anything does.
    /// </summary>
    /// <remarks>
    /// A trace installs its clocks before its own recorder, so what this declares observes a shared boundary first and
    /// the time is already current when the trace captures its moment. A clock whose source keeps the value itself
    /// installs nothing. Several traces may select one clock, so this must be safe to call more than once.
    /// </remarks>
    public abstract void Install(ExecutionInstanceResolverBuilder builder);
}

/// <summary>
/// A clock whose time has the type <typeparamref name="TTime"/>.
/// </summary>
/// <remarks>
/// This is the type a custom clock derives from. The clock object is also the key a trace is read back with, so one
/// instance means one axis: two clocks of the same kind stay distinguishable.
/// </remarks>
public abstract class Clock<TTime> : Clock
{
    protected Clock() { }

    /// <summary>
    /// Reads the time in effect now. Called once per recorded trace entry.
    /// </summary>
    protected abstract TTime ReadTime();

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

/// <summary>
/// Counts the iterations of one chosen algorithm.
/// </summary>
/// <remarks>
/// A nested algorithm has its own iteration count, which is why the algorithm is named rather than inferred.
/// </remarks>
public sealed class IterationClock<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
    : Clock<long>, IObservationRecorder<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private long latest;

    protected override long ReadTime() => Interlocked.Read(ref latest);

    public override void Install(ExecutionInstanceResolverBuilder builder) => builder.Observe(algorithm, this);

    public void Record(AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState> observation) =>
        Interlocked.Exchange(ref latest, observation.Iteration);
}

/// <summary>
/// Counts the candidates one chosen evaluator has evaluated.
/// </summary>
/// <remarks>
/// This is the axis that makes runs of different algorithms comparable. Several evaluator boundaries may count
/// different work, which is why the evaluator is named rather than inferred.
/// </remarks>
public sealed class EvaluationClock<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
    : Clock<long>, IObservationRecorder<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private long evaluations;

    protected override long ReadTime() => Interlocked.Read(ref evaluations);

    public override void Install(ExecutionInstanceResolverBuilder builder) => builder.Observe(evaluator, this);

    public void Record(EvaluatorObservation<TCandidate, TSearchSpace, TProblem> observation) =>
        Interlocked.Add(ref evaluations, observation.ObjectiveVectors.Count);
}

/// <summary>
/// Measures wall-clock time since the run started.
/// </summary>
public sealed class ElapsedTimeClock(TimeProvider timeProvider) : Clock<TimeSpan>
{
    private long startedAt;

    protected override TimeSpan ReadTime() => timeProvider.GetElapsedTime(Interlocked.Read(ref startedAt));

    public override void Install(ExecutionInstanceResolverBuilder builder) =>
        Interlocked.CompareExchange(ref startedAt, timeProvider.GetTimestamp(), 0);
}

/// <summary>
/// Creates the clocks the library ships.
/// </summary>
/// <remarks>
/// A clock of your own belongs beside the thing it reads, and is offered the same way: an extension on
/// <see cref="Clock"/> named for the time it reads, so every clock is reached through one name.
/// </remarks>
public static class Clocks
{
    extension(Clock)
    {
        /// <summary>
        /// Creates a clock counting the iterations of one chosen algorithm.
        /// </summary>
        public static IterationClock<TCandidate, TSearchSpace, TProblem, TSearchState> FromIterations<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            new(algorithm);

        /// <summary>
        /// Creates a clock counting the candidates one chosen evaluator has evaluated.
        /// </summary>
        public static EvaluationClock<TCandidate, TSearchSpace, TProblem> FromEvaluations<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            new(evaluator);

        /// <summary>
        /// Creates a clock measuring wall-clock time since the run started.
        /// </summary>
        public static ElapsedTimeClock FromElapsedTime(TimeProvider timeProvider) => new(timeProvider);
    }
}
