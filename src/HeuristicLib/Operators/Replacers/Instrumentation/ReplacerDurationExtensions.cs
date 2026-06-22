using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public static class ReplacerDurationExtensions
{
    extension<TG, TS, TP>(IReplacer<TG, TS, TP> replacer)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public IReplacer<TG, TS, TP> MeasureReplacerDuration(ObservationDuration duration)
            => replacer.MeasureReplacerDuration(duration, TimeProvider.System);

        public IReplacer<TG, TS, TP> MeasureReplacerDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringReplacer<TG, TS, TP>(replacer, duration, timeProvider);

        public IReplacer<TG, TS, TP> MeasureReplacerDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return replacer.MeasureReplacerDuration(duration);
        }

        public IReplacer<TG, TS, TP> MeasureReplacerDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return replacer.MeasureReplacerDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringReplacer<TG, TS, TP>
        : WrappingReplacer<TG, TS, TP>
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public DurationMeasuringReplacer(
            IReplacer<TG, TS, TP> replacer,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(replacer)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<Solution<TG>> Replace(
            IReadOnlyList<Solution<TG>> previousPopulation,
            IReadOnlyList<Solution<TG>> offspringPopulation,
            Objective objective,
            int count,
            InnerReplace innerReplace,
            IRandomNumberGenerator random,
            TS searchSpace,
            TP problem)
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
