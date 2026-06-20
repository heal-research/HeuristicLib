using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public static class CreatorDurationExtensions
{
    extension<TG, TS, TP>(ICreator<TG, TS, TP> creator)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public ICreator<TG, TS, TP> MeasureCreatorDuration(ObservationDuration duration)
            => creator.MeasureCreatorDuration(duration, TimeProvider.System);

        public ICreator<TG, TS, TP> MeasureCreatorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringCreator<TG, TS, TP>(creator, duration, timeProvider);

        public ICreator<TG, TS, TP> MeasureCreatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return creator.MeasureCreatorDuration(duration);
        }

        public ICreator<TG, TS, TP> MeasureCreatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return creator.MeasureCreatorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringCreator<TG, TS, TP>
        : WrappingCreator<TG, TS, TP>
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public DurationMeasuringCreator(
            ICreator<TG, TS, TP> creator,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(creator)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<TG> Create(
            int count,
            InnerCreate innerCreate,
            IRandomNumberGenerator random,
            TS searchSpace,
            TP problem)
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
