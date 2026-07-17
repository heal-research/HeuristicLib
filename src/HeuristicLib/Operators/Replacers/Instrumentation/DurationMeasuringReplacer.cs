using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public sealed record DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem>
    : WrappingReplacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IReplacer<TCandidate, TSearchSpace, TProblem> Replacer => InnerReplacer;
    public ObservationDuration Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> replacer, ObservationDuration duration)
        : this(replacer, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> replacer, ObservationDuration duration, TimeProvider timeProvider)
        : base(replacer)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateReplacerInstance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> innerReplacer) =>
        new Instance(innerReplacer, Duration, TimeProvider);

    private sealed class Instance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> innerReplacer, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(innerReplacer)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
            IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
            ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return InnerReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
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
            return new DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem>(replacer, duration);
        }

        public DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem> MeasureReplacerDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new DurationMeasuringReplacer<TCandidate, TSearchSpace, TProblem>(replacer, duration, timeProvider);
        }
    }
}
