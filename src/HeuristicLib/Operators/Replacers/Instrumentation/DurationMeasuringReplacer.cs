using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public sealed record DurationMeasuringReplacer<TCandidate>
    : WrappingReplacer<TCandidate>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringReplacer(IReplacer<TCandidate> childReplacer, ObservationDuration duration)
        : this(childReplacer, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringReplacer(IReplacer<TCandidate> childReplacer, ObservationDuration duration, TimeProvider timeProvider)
        : base(childReplacer)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> childReplacer) =>
        new Instance<TRunSearchSpace, TRunProblem>(childReplacer, Duration, TimeProvider);

    private sealed class Instance<TSearchSpace, TProblem>(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(childReplacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static DurationMeasuringReplacer<TCandidate> Create<TCandidate>(IReplacer<TCandidate> childReplacer, ObservationDuration duration) =>
        new(childReplacer, duration);

    public static DurationMeasuringReplacer<TCandidate> Create<TCandidate>(IReplacer<TCandidate> childReplacer, ObservationDuration duration, TimeProvider timeProvider) =>
        new(childReplacer, duration, timeProvider);
}

public static class ReplacerDurationExtensions
{
    extension<TCandidate>(IReplacer<TCandidate> replacer)
    {
        public DurationMeasuringReplacer<TCandidate> MeasureDuration(ObservationDuration duration) => new(replacer, duration);

        public DurationMeasuringReplacer<TCandidate> MeasureDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(replacer, duration, timeProvider);

        public DurationMeasuringReplacer<TCandidate> MeasureDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(replacer, duration);
        }

        public DurationMeasuringReplacer<TCandidate> MeasureDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(replacer, duration, timeProvider);
        }
    }
}
