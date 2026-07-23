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
    public ICreator<TCandidate, TSearchSpace, TProblem> Creator => InnerCreator;
    public ObservationDuration Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringCreator(ICreator<TCandidate, TSearchSpace, TProblem> creator, ObservationDuration duration)
        : this(creator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringCreator(ICreator<TCandidate, TSearchSpace, TProblem> creator, ObservationDuration duration, TimeProvider timeProvider)
        : base(creator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> innerCreator) =>
        new Instance(innerCreator, Duration, TimeProvider);

    private sealed class Instance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> innerCreator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(innerCreator)
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return InnerCreator.Create(count, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
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
            return new DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem>(creator, duration);
        }

        public DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem> MeasureCreatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem>(creator, duration, timeProvider);
        }
    }
}
