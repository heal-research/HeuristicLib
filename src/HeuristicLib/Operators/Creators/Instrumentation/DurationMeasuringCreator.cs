using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public sealed record DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem>
    : WrappingCreator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringCreator(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, ObservationDuration duration)
        : this(childCreator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringCreator(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, ObservationDuration duration, TimeProvider timeProvider)
        : base(childCreator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator) =>
        new Instance(childCreator, Duration, TimeProvider);

    private sealed class Instance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(childCreator)
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
    public static DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, ObservationDuration duration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreator, duration);

    public static DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> childCreator, ObservationDuration duration, TimeProvider timeProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreator, duration, timeProvider);
}

public static class CreatorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem> MeasureCreatorDuration(ObservationDuration duration) => new(creator, duration);

        public DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem> MeasureCreatorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(creator, duration, timeProvider);

        public DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem> MeasureCreatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(creator, duration);
        }

        public DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem> MeasureCreatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(creator, duration, timeProvider);
        }
    }
}
