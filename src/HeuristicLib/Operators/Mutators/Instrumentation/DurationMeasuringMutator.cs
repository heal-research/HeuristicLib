using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public sealed record DurationMeasuringMutator<TCandidate> : WrappingMutator<TCandidate>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringMutator(IMutator<TCandidate> childMutator, ObservationDuration duration)
        : this(childMutator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringMutator(IMutator<TCandidate> childMutator, ObservationDuration duration, TimeProvider timeProvider)
        : base(childMutator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childMutator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childMutator, Duration, TimeProvider);

    private sealed class Instance<TSearchSpace, TProblem>(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutator)
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
    public static DurationMeasuringMutator<TCandidate> Create<TCandidate>(IMutator<TCandidate> childMutator, ObservationDuration duration) =>
        new(childMutator, duration);

    public static DurationMeasuringMutator<TCandidate> Create<TCandidate>(IMutator<TCandidate> childMutator, ObservationDuration duration, TimeProvider timeProvider) =>
        new(childMutator, duration, timeProvider);
}

public static class MutatorDurationExtensions
{
    extension<TCandidate>(IMutator<TCandidate> mutator)
    {
        public DurationMeasuringMutator<TCandidate> MeasureMutatorDuration(ObservationDuration duration) => new(mutator, duration);

        public DurationMeasuringMutator<TCandidate> MeasureMutatorDuration(ObservationDuration duration, TimeProvider timeProvider) => new(mutator, duration, timeProvider);

        public DurationMeasuringMutator<TCandidate> MeasureMutatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(mutator, duration);
        }

        public DurationMeasuringMutator<TCandidate> MeasureMutatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(mutator, duration, timeProvider);
        }
    }
}
