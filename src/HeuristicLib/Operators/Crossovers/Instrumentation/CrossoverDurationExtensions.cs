using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public static class CrossoverDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(ObservationDuration duration)
            => crossover.MeasureCrossoverDuration(duration, TimeProvider.System);

        public ICrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem>(crossover, duration, timeProvider);

        public ICrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return crossover.MeasureCrossoverDuration(duration);
        }

        public ICrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return crossover.MeasureCrossoverDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem>
        : WrappingCrossover<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringCrossover(
            ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(crossover)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<TCandidate> Cross(
            IReadOnlyList<IParents<TCandidate>> parents,
            InnerCross innerCross,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
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
