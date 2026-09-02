using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public sealed record DurationMeasuringCreator<TCandidate>
    : WrappingCreator<TCandidate>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringCreator(ICreator<TCandidate> childCreator, ObservationDuration duration)
        : this(childCreator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringCreator(ICreator<TCandidate> childCreator, ObservationDuration duration, TimeProvider timeProvider)
        : base(childCreator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childCreator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childCreator, Duration, TimeProvider);

    private sealed class Instance<TSearchSpace, TProblem>(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(childCreator)
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
    public static DurationMeasuringCreator<TCandidate> Create<TCandidate>(ICreator<TCandidate> childCreator, ObservationDuration duration)
 =>
        new(childCreator, duration);

    public static DurationMeasuringCreator<TCandidate> Create<TCandidate>(ICreator<TCandidate> childCreator, ObservationDuration duration, TimeProvider timeProvider)
 =>
        new(childCreator, duration, timeProvider);
}

public static class CreatorDurationExtensions
{
    extension<TCandidate>(ICreator<TCandidate> creator)
    {
        public DurationMeasuringCreator<TCandidate> MeasureCreatorDuration(ObservationDuration duration) => new(creator, duration);

        public DurationMeasuringCreator<TCandidate> MeasureCreatorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(creator, duration, timeProvider);

        public DurationMeasuringCreator<TCandidate> MeasureCreatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(creator, duration);
        }

        public DurationMeasuringCreator<TCandidate> MeasureCreatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(creator, duration, timeProvider);
        }
    }
}
