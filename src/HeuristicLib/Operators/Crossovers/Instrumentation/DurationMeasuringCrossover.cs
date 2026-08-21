using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public sealed record DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem>
    : WrappingCrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationDuration duration)
        : this(childCrossover, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationDuration duration, TimeProvider timeProvider)
        : base(childCrossover)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover) =>
        new Instance(childCrossover, Duration, TimeProvider);

    private sealed class Instance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(childCrossover)
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildCrossover.Cross(parents, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class DurationMeasuringCrossover
{
    public static DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationDuration duration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCrossover, duration);

    public static DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationDuration duration, TimeProvider timeProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCrossover, duration, timeProvider);
}

public static class CrossoverDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(ObservationDuration duration) =>
            new(crossover, duration);

        public DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(crossover, duration, timeProvider);

        public DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(crossover, duration);
        }

        public DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(crossover, duration, timeProvider);
        }
    }
}
