using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public sealed record DurationMeasuringCreator<TCandidate>
    : WrappingCreator<TCandidate>
{
    public DurationAccumulator Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringCreator(ICreator<TCandidate> childCreator, DurationAccumulator duration)
        : this(childCreator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringCreator(ICreator<TCandidate> childCreator, DurationAccumulator duration, TimeProvider timeProvider)
        : base(childCreator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem> childCreator) =>
        new Execution<TRunSearchSpace, TRunProblem>(childCreator, Duration, TimeProvider);

    private sealed class Execution<TSearchSpace, TProblem>(ICreatorExecution<TCandidate, TSearchSpace, TProblem> childCreator, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingCreatorExecution<TCandidate, TSearchSpace, TProblem>(childCreator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildCreator.Create(count, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class DurationMeasuringCreator
{
    public static DurationMeasuringCreator<TCandidate> Create<TCandidate>(ICreator<TCandidate> childCreator, DurationAccumulator duration) =>
        new(childCreator, duration);

    public static DurationMeasuringCreator<TCandidate> Create<TCandidate>(ICreator<TCandidate> childCreator, DurationAccumulator duration, TimeProvider timeProvider) =>
        new(childCreator, duration, timeProvider);
}

public static class CreatorDurationExtensions
{
    extension<TCandidate>(ICreator<TCandidate> creator)
    {
        public DurationMeasuringCreator<TCandidate> MeasureDuration(DurationAccumulator duration) => new(creator, duration);

        public DurationMeasuringCreator<TCandidate> MeasureDuration(DurationAccumulator duration, TimeProvider timeProvider) =>
            new(creator, duration, timeProvider);

        public DurationMeasuringCreator<TCandidate> MeasureDuration(out DurationAccumulator duration)
        {
            duration = new DurationAccumulator();
            return new(creator, duration);
        }

        public DurationMeasuringCreator<TCandidate> MeasureDuration(out DurationAccumulator duration, TimeProvider timeProvider)
        {
            duration = new DurationAccumulator();
            return new(creator, duration, timeProvider);
        }
    }
}
