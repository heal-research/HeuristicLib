using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public sealed record DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem>
    : WrappingSelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ISelector<TCandidate, TSearchSpace, TProblem> Selector => InnerSelector;
    public ObservationDuration Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringSelector(ISelector<TCandidate, TSearchSpace, TProblem> selector, ObservationDuration duration)
        : this(selector, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringSelector(ISelector<TCandidate, TSearchSpace, TProblem> selector, ObservationDuration duration, TimeProvider timeProvider)
        : base(selector)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateSelectorInstance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> innerSelector) =>
        new Instance(innerSelector, Duration, TimeProvider);

    private sealed class Instance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> innerSelector, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(innerSelector)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(
            IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count,
            IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return InnerSelector.Select(population, objective, count, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class SelectorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem> MeasureSelectorDuration(ObservationDuration duration) => new(selector, duration);

        public DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem> MeasureSelectorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(selector, duration, timeProvider);

        public DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem> MeasureSelectorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem>(selector, duration);
        }

        public DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem> MeasureSelectorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem>(selector, duration, timeProvider);
        }
    }
}
