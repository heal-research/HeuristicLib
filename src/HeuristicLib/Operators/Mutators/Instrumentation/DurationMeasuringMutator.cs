using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public sealed record DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem>
    : WrappingMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringMutator(IMutator<TCandidate, TSearchSpace, TProblem> childMutator, ObservationDuration duration)
        : this(childMutator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringMutator(IMutator<TCandidate, TSearchSpace, TProblem> childMutator, ObservationDuration duration, TimeProvider timeProvider)
        : base(childMutator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator) =>
        new Instance(childMutator, Duration, TimeProvider);

    private sealed class Instance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutator)
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
    public static DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> childMutator, ObservationDuration duration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutator, duration);

    public static DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> childMutator, ObservationDuration duration, TimeProvider timeProvider)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutator, duration, timeProvider);
}

public static class MutatorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(ObservationDuration duration) =>
            new(mutator, duration);

        public DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(mutator, duration, timeProvider);

        public DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(mutator, duration);
        }

        public DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(mutator, duration, timeProvider);
        }
    }
}
