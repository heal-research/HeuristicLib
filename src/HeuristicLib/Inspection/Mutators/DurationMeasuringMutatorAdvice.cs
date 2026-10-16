using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class DurationMeasuringMutatorAdvice<TCandidate> : MutatorAdvice<TCandidate>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringMutatorAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringMutatorAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override WrapperExecutorFactory<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IMutator<TCandidate> mutator) =>
        childMutator => new Executor<TRunSearchSpace, TRunProblem>(childMutator, Duration, TimeProvider);

    private sealed class Executor<TSearchSpace, TProblem>(IMutationExecutor<TCandidate, TSearchSpace, TProblem> childMutator, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingMutationExecutor<TCandidate, TSearchSpace, TProblem>(childMutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildMutator.Mutate(parents, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public sealed record MutatorDurationMeasuring<TCandidate> : IOperatorDurationMeasuring<IMutator<TCandidate>>
{
    public Advice<IMutator<TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringMutatorAdvice<TCandidate>(duration, timeProvider);
}

public static class MutatorDurationExtensions
{
    extension(Aspect)
    {
        /// <summary>Measures the time spent in one chosen mutator.</summary>
        public static Aspect MeasureDuration<TCandidate>(IMutator<TCandidate> mutator, DurationAccumulator duration) =>
            Aspect.Create(Pointcut.Reference(mutator), new DurationMeasuringMutatorAdvice<TCandidate>(duration));

        public static Aspect MeasureDuration<TCandidate>(IMutator<TCandidate> mutator, DurationAccumulator duration, TimeProvider timeProvider) =>
            Aspect.Create(Pointcut.Reference(mutator), new DurationMeasuringMutatorAdvice<TCandidate>(duration, timeProvider));
    }
}
