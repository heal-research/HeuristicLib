using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class DurationMeasuringCreatorAdvice<TCandidate> : CreatorAdvice<TCandidate>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringCreatorAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringCreatorAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override WrapperExecutorFactory<ICreationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ICreator<TCandidate> creator) =>
        childCreator => new Executor<TRunSearchSpace, TRunProblem>(childCreator, Duration, TimeProvider);

    private sealed class Executor<TSearchSpace, TProblem>(ICreationExecutor<TCandidate, TSearchSpace, TProblem> childCreator, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingCreationExecutor<TCandidate, TSearchSpace, TProblem>(childCreator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildCreator.Create(count, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public sealed record CreatorDurationMeasuring<TCandidate> : IOperatorDurationMeasuring<ICreator<TCandidate>>
{
    public Advice<ICreator<TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringCreatorAdvice<TCandidate>(duration, timeProvider);
}

public static class CreatorDurationExtensions
{
    extension(Aspect)
    {
        public static Aspect MeasureDuration<TCandidate>(ICreator<TCandidate> creator, DurationAccumulator duration) =>
            Aspect.Create(Pointcut.Reference(creator), new DurationMeasuringCreatorAdvice<TCandidate>(duration));

        public static Aspect MeasureDuration<TCandidate>(ICreator<TCandidate> creator, DurationAccumulator duration, TimeProvider timeProvider) =>
            Aspect.Create(Pointcut.Reference(creator), new DurationMeasuringCreatorAdvice<TCandidate>(duration, timeProvider));
    }
}
