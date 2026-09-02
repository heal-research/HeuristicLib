using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public sealed record DurationMeasuringSelector<TCandidate>
    : WrappingSelector<TCandidate>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringSelector(ISelector<TCandidate> childSelector, ObservationDuration duration)
        : this(childSelector, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringSelector(ISelector<TCandidate> childSelector, ObservationDuration duration, TimeProvider timeProvider)
        : base(childSelector)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> childSelector) =>
        new Instance<TRunSearchSpace, TRunProblem>(childSelector, Duration, TimeProvider);

    private sealed class Instance<TSearchSpace, TProblem>(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelector)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static DurationMeasuringSelector<TCandidate> Create<TCandidate>(ISelector<TCandidate> childSelector, ObservationDuration duration) =>
        new(childSelector, duration);

    public static DurationMeasuringSelector<TCandidate> Create<TCandidate>(ISelector<TCandidate> childSelector, ObservationDuration duration, TimeProvider timeProvider) =>
        new(childSelector, duration, timeProvider);
}

public static class SelectorDurationExtensions
{
    extension<TCandidate>(ISelector<TCandidate> selector)
    {
        public DurationMeasuringSelector<TCandidate> MeasureSelectorDuration(ObservationDuration duration) => new(selector, duration);

        public DurationMeasuringSelector<TCandidate> MeasureSelectorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(selector, duration, timeProvider);

        public DurationMeasuringSelector<TCandidate> MeasureSelectorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(selector, duration);
        }

        public DurationMeasuringSelector<TCandidate> MeasureSelectorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(selector, duration, timeProvider);
        }
    }
}
