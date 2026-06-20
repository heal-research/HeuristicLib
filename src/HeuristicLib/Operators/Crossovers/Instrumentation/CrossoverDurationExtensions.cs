using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public static class CrossoverDurationExtensions
{
    extension<TG, TS, TP>(ICrossover<TG, TS, TP> crossover)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public ICrossover<TG, TS, TP> MeasureCrossoverDuration(ObservationDuration duration)
            => crossover.MeasureCrossoverDuration(duration, TimeProvider.System);

        public ICrossover<TG, TS, TP> MeasureCrossoverDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringCrossover<TG, TS, TP>(crossover, duration, timeProvider);

        public ICrossover<TG, TS, TP> MeasureCrossoverDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return crossover.MeasureCrossoverDuration(duration);
        }

        public ICrossover<TG, TS, TP> MeasureCrossoverDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return crossover.MeasureCrossoverDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringCrossover<TG, TS, TP>
        : WrappingCrossover<TG, TS, TP>
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public DurationMeasuringCrossover(
            ICrossover<TG, TS, TP> crossover,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(crossover)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<TG> Cross(
            IReadOnlyList<IParents<TG>> parents,
            InnerCross innerCross,
            IRandomNumberGenerator random,
            TS searchSpace,
            TP problem)
        {
            var startTimestamp = TimeProvider.GetTimestamp();
            try
            {
                return innerCross(parents, random, searchSpace, problem);
            }
            finally
            {
                Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}
