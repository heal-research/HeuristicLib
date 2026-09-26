using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public sealed record DurationMeasuringRefiner<TCandidate>
    : WrappingRefiner<TCandidate>
{
    public DurationAccumulator Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringRefiner(IRefiner<TCandidate> childRefiner, DurationAccumulator duration)
        : this(childRefiner, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringRefiner(IRefiner<TCandidate> childRefiner, DurationAccumulator duration, TimeProvider timeProvider)
        : base(childRefiner)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> childRefiner) =>
        new Instance<TRunSearchSpace, TRunProblem>(childRefiner, Duration, TimeProvider);

    private sealed class Instance<TSearchSpace, TProblem>(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static DurationMeasuringRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> childRefiner, DurationAccumulator duration) =>
        new(childRefiner, duration);

    public static DurationMeasuringRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> childRefiner, DurationAccumulator duration, TimeProvider timeProvider) =>
        new(childRefiner, duration, timeProvider);
}

public static class RefinerDurationExtensions
{
    extension<TCandidate>(IRefiner<TCandidate> refiner)
    {
        public DurationMeasuringRefiner<TCandidate> MeasureDuration(DurationAccumulator duration) =>
            new(refiner, duration);

        public DurationMeasuringRefiner<TCandidate> MeasureDuration(DurationAccumulator duration, TimeProvider timeProvider) =>
            new(refiner, duration, timeProvider);

        public DurationMeasuringRefiner<TCandidate> MeasureDuration(out DurationAccumulator duration)
        {
            duration = new DurationAccumulator();
            return new(refiner, duration);
        }

        public DurationMeasuringRefiner<TCandidate> MeasureDuration(out DurationAccumulator duration, TimeProvider timeProvider)
        {
            duration = new DurationAccumulator();
            return new(refiner, duration, timeProvider);
        }
    }
}
