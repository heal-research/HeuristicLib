using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public sealed record DurationMeasuringRefiner<TCandidate, TSearchSpace, TProblem>
    : WrappingRefiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringRefiner(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, ObservationDuration duration)
        : this(childRefiner, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringRefiner(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, ObservationDuration duration, TimeProvider timeProvider)
        : base(childRefiner)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner) =>
        new Instance(childRefiner, Duration, TimeProvider);

    private sealed class Instance(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiner)
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildRefiner.Refine(candidates, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class DurationMeasuringRefiner
{
    public static DurationMeasuringRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, ObservationDuration duration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childRefiner, duration);

    public static DurationMeasuringRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner, ObservationDuration duration, TimeProvider timeProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childRefiner, duration, timeProvider);
}

public static class RefinerDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringRefiner<TCandidate, TSearchSpace, TProblem> MeasureRefinerDuration(ObservationDuration duration) =>
            new(refiner, duration);

        public DurationMeasuringRefiner<TCandidate, TSearchSpace, TProblem> MeasureRefinerDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(refiner, duration, timeProvider);

        public DurationMeasuringRefiner<TCandidate, TSearchSpace, TProblem> MeasureRefinerDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(refiner, duration);
        }

        public DurationMeasuringRefiner<TCandidate, TSearchSpace, TProblem> MeasureRefinerDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(refiner, duration, timeProvider);
        }
    }
}
