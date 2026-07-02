using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public static class SelectorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ISelector<TCandidate, TSearchSpace, TProblem> MeasureSelectorDuration(ObservationDuration duration)
            => selector.MeasureSelectorDuration(duration, TimeProvider.System);

        public ISelector<TCandidate, TSearchSpace, TProblem> MeasureSelectorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem>(selector, duration, timeProvider);

        public ISelector<TCandidate, TSearchSpace, TProblem> MeasureSelectorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return selector.MeasureSelectorDuration(duration);
        }

        public ISelector<TCandidate, TSearchSpace, TProblem> MeasureSelectorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return selector.MeasureSelectorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem>
        : WrappingSelector<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringSelector(
            ISelector<TCandidate, TSearchSpace, TProblem> selector,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(selector)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(
            IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
            ObjectiveDirections objective,
            int count,
            InnerSelect innerSelect,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
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
