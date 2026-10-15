using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class DurationMeasuringSelectorAdvice<TCandidate> : SelectorAdvice<TCandidate>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringSelectorAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringSelectorAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override WrapperExecutionFactory<ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ISelector<TCandidate> selector) =>
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

public sealed record SelectorDurationMeasuring<TCandidate> : IOperatorDurationMeasuring<ISelector<TCandidate>>
{
    public Advice<ISelector<TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringSelectorAdvice<TCandidate>(duration, timeProvider);
}

public static class SelectorDurationExtensions
{
    extension(Aspect)
    {
        public static Aspect MeasureDuration<TCandidate>(ISelector<TCandidate> selector, DurationAccumulator duration) =>
            Aspect.Create(Pointcut.Reference(selector), new DurationMeasuringSelectorAdvice<TCandidate>(duration));

        public static Aspect MeasureDuration<TCandidate>(ISelector<TCandidate> selector, DurationAccumulator duration, TimeProvider timeProvider) =>
            Aspect.Create(Pointcut.Reference(selector), new DurationMeasuringSelectorAdvice<TCandidate>(duration, timeProvider));
    }
}
