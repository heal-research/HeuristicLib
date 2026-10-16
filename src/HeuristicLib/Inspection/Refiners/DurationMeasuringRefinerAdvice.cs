using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class DurationMeasuringRefinerAdvice<TCandidate> : RefinerAdvice<TCandidate>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringRefinerAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringRefinerAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override WrapperExecutorFactory<IRefinementExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IRefiner<TCandidate> refiner) =>
        childRefiner => new Executor<TRunSearchSpace, TRunProblem>(childRefiner, Duration, TimeProvider);

    private sealed class Executor<TSearchSpace, TProblem>(IRefinementExecutor<TCandidate, TSearchSpace, TProblem> childRefiner, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingRefinementExecutor<TCandidate, TSearchSpace, TProblem>(childRefiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildRefiner.Refine(candidates, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public sealed record RefinerDurationMeasuring<TCandidate> : IOperatorDurationMeasuring<IRefiner<TCandidate>>
{
    public Advice<IRefiner<TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringRefinerAdvice<TCandidate>(duration, timeProvider);
}

public static class RefinerDurationExtensions
{
    extension(Aspect)
    {
        public static Aspect MeasureDuration<TCandidate>(IRefiner<TCandidate> refiner, DurationAccumulator duration) =>
            Aspect.Create(Pointcut.Reference(refiner), new DurationMeasuringRefinerAdvice<TCandidate>(duration));

        public static Aspect MeasureDuration<TCandidate>(IRefiner<TCandidate> refiner, DurationAccumulator duration, TimeProvider timeProvider) =>
            Aspect.Create(Pointcut.Reference(refiner), new DurationMeasuringRefinerAdvice<TCandidate>(duration, timeProvider));
    }
}
