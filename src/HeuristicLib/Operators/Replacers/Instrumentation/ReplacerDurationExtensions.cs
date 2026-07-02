using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public static class ReplacerDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReplacer<TCandidate, TSearchSpace, TProblem> MeasureReplacerDuration(ObservationDuration duration)
            => replacer.MeasureReplacerDuration(duration, TimeProvider.System);

        public IReplacer<TCandidate, TSearchSpace, TProblem> MeasureReplacerDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem>(replacer, duration, timeProvider);

        public IReplacer<TCandidate, TSearchSpace, TProblem> MeasureReplacerDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return replacer.MeasureReplacerDuration(duration);
        }

        public IReplacer<TCandidate, TSearchSpace, TProblem> MeasureReplacerDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return replacer.MeasureReplacerDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem>
        : WrappingReplacer<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringReplacer(
            IReplacer<TCandidate, TSearchSpace, TProblem> replacer,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(replacer)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
            IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
            IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
            ObjectiveDirections objective,
            int count,
            InnerReplace innerReplace,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
        {
            var startTimestamp = TimeProvider.GetTimestamp();
            try
            {
                return innerReplace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
            }
            finally
            {
                Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}
