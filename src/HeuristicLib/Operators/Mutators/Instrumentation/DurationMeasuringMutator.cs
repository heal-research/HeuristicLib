using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public sealed record DurationMeasuringMutator<TCandidate> : WrappingMutator<TCandidate>
{
    public DurationAccumulator Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringMutator(IMutator<TCandidate> childMutator, DurationAccumulator duration)
        : this(childMutator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringMutator(IMutator<TCandidate> childMutator, DurationAccumulator duration, TimeProvider timeProvider)
        : base(childMutator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrapperExecutionFactory<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        childMutator => new Execution<TRunSearchSpace, TRunProblem>(childMutator, Duration, TimeProvider);

    private sealed class Execution<TSearchSpace, TProblem>(IMutatorExecution<TCandidate, TSearchSpace, TProblem> childMutator, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingMutatorExecution<TCandidate, TSearchSpace, TProblem>(childMutator)
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

public static class DurationMeasuringMutator
{
    public static DurationMeasuringMutator<TCandidate> Create<TCandidate>(IMutator<TCandidate> childMutator, DurationAccumulator duration) =>
        new(childMutator, duration);

    public static DurationMeasuringMutator<TCandidate> Create<TCandidate>(IMutator<TCandidate> childMutator, DurationAccumulator duration, TimeProvider timeProvider) =>
        new(childMutator, duration, timeProvider);
}

public static class MutatorDurationExtensions
{
    extension<TCandidate>(IMutator<TCandidate> mutator)
    {
        public DurationMeasuringMutator<TCandidate> MeasureDuration(DurationAccumulator duration) => new(mutator, duration);

        public DurationMeasuringMutator<TCandidate> MeasureDuration(DurationAccumulator duration, TimeProvider timeProvider) => new(mutator, duration, timeProvider);

        public DurationMeasuringMutator<TCandidate> MeasureDuration(out DurationAccumulator duration)
        {
            duration = new DurationAccumulator();
            return new(mutator, duration);
        }

        public DurationMeasuringMutator<TCandidate> MeasureDuration(out DurationAccumulator duration, TimeProvider timeProvider)
        {
            duration = new DurationAccumulator();
            return new(mutator, duration, timeProvider);
        }
    }
}
