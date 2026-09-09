using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public sealed record DurationMeasuringCrossover<TCandidate>
    : WrappingCrossover<TCandidate>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringCrossover(ICrossover<TCandidate> childCrossover, ObservationDuration duration)
        : this(childCrossover, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringCrossover(ICrossover<TCandidate> childCrossover, ObservationDuration duration, TimeProvider timeProvider)
        : base(childCrossover)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> childCrossover) =>
        new Instance<TRunSearchSpace, TRunProblem>(childCrossover, Duration, TimeProvider);

    private sealed class Instance<TSearchSpace, TProblem>(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(childCrossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static DurationMeasuringCrossover<TCandidate> Create<TCandidate>(ICrossover<TCandidate> childCrossover, ObservationDuration duration) =>
        new(childCrossover, duration);

    public static DurationMeasuringCrossover<TCandidate> Create<TCandidate>(ICrossover<TCandidate> childCrossover, ObservationDuration duration, TimeProvider timeProvider) =>
        new(childCrossover, duration, timeProvider);
}

public static class CrossoverDurationExtensions
{
    extension<TCandidate>(ICrossover<TCandidate> crossover)
    {
        public DurationMeasuringCrossover<TCandidate> MeasureDuration(ObservationDuration duration) =>
            new(crossover, duration);

        public DurationMeasuringCrossover<TCandidate> MeasureDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(crossover, duration, timeProvider);

        public DurationMeasuringCrossover<TCandidate> MeasureDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(crossover, duration);
        }

        public DurationMeasuringCrossover<TCandidate> MeasureDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(crossover, duration, timeProvider);
        }
    }
}
