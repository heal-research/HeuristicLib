using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class DurationMeasuringCrossoverAdvice<TCandidate> : CrossoverAdvice<TCandidate>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringCrossoverAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringCrossoverAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override WrapperExecutionFactory<ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ICrossover<TCandidate> crossover) =>
        childCrossover => new Execution<TRunSearchSpace, TRunProblem>(childCrossover, Duration, TimeProvider);

    private sealed class Execution<TSearchSpace, TProblem>(ICrossoverExecution<TCandidate, TSearchSpace, TProblem> childCrossover, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingCrossoverExecution<TCandidate, TSearchSpace, TProblem>(childCrossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildCrossover.Cross(parents, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public sealed record CrossoverDurationMeasuring<TCandidate> : IOperatorDurationMeasuring<ICrossover<TCandidate>>
{
    public Advice<ICrossover<TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringCrossoverAdvice<TCandidate>(duration, timeProvider);
}

public static class CrossoverDurationExtensions
{
    extension(Aspect)
    {
        public static Aspect MeasureDuration<TCandidate>(ICrossover<TCandidate> crossover, DurationAccumulator duration) =>
            Aspect.Create(Pointcut.Reference(crossover), new DurationMeasuringCrossoverAdvice<TCandidate>(duration));

        public static Aspect MeasureDuration<TCandidate>(ICrossover<TCandidate> crossover, DurationAccumulator duration, TimeProvider timeProvider) =>
            Aspect.Create(Pointcut.Reference(crossover), new DurationMeasuringCrossoverAdvice<TCandidate>(duration, timeProvider));
    }
}
