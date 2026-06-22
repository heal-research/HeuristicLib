using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public static class SelectorDurationExtensions
{
    extension<TG, TS, TP>(ISelector<TG, TS, TP> selector)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public ISelector<TG, TS, TP> MeasureSelectorDuration(ObservationDuration duration)
            => selector.MeasureSelectorDuration(duration, TimeProvider.System);

        public ISelector<TG, TS, TP> MeasureSelectorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringSelector<TG, TS, TP>(selector, duration, timeProvider);

        public ISelector<TG, TS, TP> MeasureSelectorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return selector.MeasureSelectorDuration(duration);
        }

        public ISelector<TG, TS, TP> MeasureSelectorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return selector.MeasureSelectorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringSelector<TG, TS, TP>
        : WrappingSelector<TG, TS, TP>
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public DurationMeasuringSelector(
            ISelector<TG, TS, TP> selector,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(selector)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<Solution<TG>> Select(
            IReadOnlyList<Solution<TG>> population,
            Objective objective,
            int count,
            InnerSelect innerSelect,
            IRandomNumberGenerator random,
            TS searchSpace,
            TP problem)
        {
            var startTimestamp = TimeProvider.GetTimestamp();
            try
            {
                return innerSelect(population, objective, count, random, searchSpace, problem);
            }
            finally
            {
                Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}
