using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public static class MutatorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(ObservationDuration duration)
            => mutator.MeasureMutatorDuration(duration, TimeProvider.System);

        public IMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem>(mutator, duration, timeProvider);

        public IMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return mutator.MeasureMutatorDuration(duration);
        }

        public IMutator<TCandidate, TSearchSpace, TProblem> MeasureMutatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return mutator.MeasureMutatorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem>
        : WrappingMutator<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringMutator(
            IMutator<TCandidate, TSearchSpace, TProblem> mutator,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(mutator)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<TCandidate> Mutate(
            IReadOnlyList<TCandidate> parents,
            InnerMutate innerMutate,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
        {
            var startTimestamp = TimeProvider.GetTimestamp();
            try
            {
                return innerMutate(parents, random, searchSpace, problem);
            }
            finally
            {
                Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}
