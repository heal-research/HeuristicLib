using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public sealed record DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem>
    : WrappingMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IMutator<TCandidate, TSearchSpace, TProblem> Mutator => InnerMutator;
    public ObservationDuration Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringMutator(IMutator<TCandidate, TSearchSpace, TProblem> mutator, ObservationDuration duration)
        : this(mutator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringMutator(IMutator<TCandidate, TSearchSpace, TProblem> mutator, ObservationDuration duration, TimeProvider timeProvider)
        : base(mutator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateMutatorInstance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> innerMutator) =>
        new Instance(innerMutator, Duration, TimeProvider);

    private sealed class Instance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> innerMutator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(innerMutator)
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return InnerMutator.Mutate(parents, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class MutatorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(ObservationDuration duration) => new(mutator, duration);

        public DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(mutator, duration, timeProvider);

        public DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem>(mutator, duration);
        }

        public DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem>(mutator, duration, timeProvider);
        }
    }
}
