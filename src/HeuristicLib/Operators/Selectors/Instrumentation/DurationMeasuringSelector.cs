using HEAL.HeuristicLib.Analysis;
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
    public ObservationDuration Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringSelector(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, ObservationDuration duration)
        : this(childSelector, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringSelector(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, ObservationDuration duration, TimeProvider timeProvider)
        : base(childSelector)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector) =>
        new Instance(childSelector, Duration, TimeProvider);

    private sealed class Instance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelector)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildSelector.Select(population, objective, count, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class DurationMeasuringSelector
{
    public static DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, ObservationDuration duration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childSelector, duration);

    public static DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> childSelector, ObservationDuration duration, TimeProvider timeProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childSelector, duration, timeProvider);
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
            return new(selector, duration);
        }

        public DurationMeasuringSelector<TCandidate, TSearchSpace, TProblem> MeasureSelectorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(selector, duration, timeProvider);
        }
    }
}
