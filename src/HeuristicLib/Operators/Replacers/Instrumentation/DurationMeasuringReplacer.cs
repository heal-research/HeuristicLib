using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public sealed record DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem>
    : WrappingReplacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationDuration duration)
        : this(childReplacer, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationDuration duration, TimeProvider timeProvider)
        : base(childReplacer)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer) =>
        new Instance(childReplacer, Duration, TimeProvider);

    private sealed class Instance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(childReplacer)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
            IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
            ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class DurationMeasuringReplacer
{
    public static DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationDuration duration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childReplacer, duration);

    public static DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationDuration duration, TimeProvider timeProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childReplacer, duration, timeProvider);
}

public static class ReplacerDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem> MeasureReplacerDuration(ObservationDuration duration) => new(replacer, duration);

        public DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem> MeasureReplacerDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(replacer, duration, timeProvider);

        public DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem> MeasureReplacerDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(replacer, duration);
        }

        public DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem> MeasureReplacerDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(replacer, duration, timeProvider);
        }
    }
}
