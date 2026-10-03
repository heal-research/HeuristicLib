using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public sealed record DurationMeasuringSelector<TCandidate>
    : WrappingSelector<TCandidate>
{
    public DurationAccumulator Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringSelector(ISelector<TCandidate> childSelector, DurationAccumulator duration)
        : this(childSelector, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringSelector(ISelector<TCandidate> childSelector, DurationAccumulator duration, TimeProvider timeProvider)
        : base(childSelector)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrapperExecutionFactory<ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        childSelector => new Execution<TRunSearchSpace, TRunProblem>(childSelector, Duration, TimeProvider);

    private sealed class Execution<TSearchSpace, TProblem>(ISelectorExecution<TCandidate, TSearchSpace, TProblem> childSelector, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingSelectorExecution<TCandidate, TSearchSpace, TProblem>(childSelector)
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
    public static DurationMeasuringSelector<TCandidate> Create<TCandidate>(ISelector<TCandidate> childSelector, DurationAccumulator duration) =>
        new(childSelector, duration);

    public static DurationMeasuringSelector<TCandidate> Create<TCandidate>(ISelector<TCandidate> childSelector, DurationAccumulator duration, TimeProvider timeProvider) =>
        new(childSelector, duration, timeProvider);
}

public static class SelectorDurationExtensions
{
    extension<TCandidate>(ISelector<TCandidate> selector)
    {
        public DurationMeasuringSelector<TCandidate> MeasureDuration(DurationAccumulator duration) => new(selector, duration);

        public DurationMeasuringSelector<TCandidate> MeasureDuration(DurationAccumulator duration, TimeProvider timeProvider) =>
            new(selector, duration, timeProvider);

        public DurationMeasuringSelector<TCandidate> MeasureDuration(out DurationAccumulator duration)
        {
            duration = new DurationAccumulator();
            return new(selector, duration);
        }

        public DurationMeasuringSelector<TCandidate> MeasureDuration(out DurationAccumulator duration, TimeProvider timeProvider)
        {
            duration = new DurationAccumulator();
            return new(selector, duration, timeProvider);
        }
    }
}
