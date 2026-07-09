using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public static class CreatorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICreator<TCandidate, TSearchSpace, TProblem> MeasureCreatorDuration(ObservationDuration duration)
            => creator.MeasureCreatorDuration(duration, TimeProvider.System);

        public ICreator<TCandidate, TSearchSpace, TProblem> MeasureCreatorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem>(creator, duration, timeProvider);

        public ICreator<TCandidate, TSearchSpace, TProblem> MeasureCreatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return creator.MeasureCreatorDuration(duration);
        }

        public ICreator<TCandidate, TSearchSpace, TProblem> MeasureCreatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return creator.MeasureCreatorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringCreator<TCandidate, TSearchSpace, TProblem>
        : WrappingCreator<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringCreator(
            ICreator<TCandidate, TSearchSpace, TProblem> creator,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(creator)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<TCandidate> Create(
            int count,
            InnerCreate innerCreate,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
        {
            var startTimestamp = TimeProvider.GetTimestamp();
            try
            {
                return innerCreate(count, random, searchSpace, problem);
            }
            finally
            {
                Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}
