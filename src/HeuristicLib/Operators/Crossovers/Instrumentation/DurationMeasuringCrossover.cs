using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public sealed record DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem>
    : WrappingCrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ICrossover<TCandidate, TSearchSpace, TProblem> Crossover => InnerCrossover;
    public ObservationDuration Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> crossover, ObservationDuration duration)
        : this(crossover, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> crossover, ObservationDuration duration, TimeProvider timeProvider)
        : base(crossover)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> innerCrossover) =>
        new Instance(innerCrossover, Duration, TimeProvider);

    private sealed class Instance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> innerCrossover, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(innerCrossover)
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return InnerCrossover.Cross(parents, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class CrossoverDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(ObservationDuration duration) => new(crossover, duration);

        public DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(crossover, duration, timeProvider);

        public DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(crossover, duration);
        }

        public DurationMeasuringCrossover<TCandidate, TSearchSpace, TProblem> MeasureCrossoverDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(crossover, duration, timeProvider);
        }
    }
}
