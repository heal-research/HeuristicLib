using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public static class MutatorDurationExtensions
{
    extension<TG, TS, TP>(IMutator<TG, TS, TP> mutator)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public IMutator<TG, TS, TP> MeasureMutatorDuration(ObservationDuration duration)
            => mutator.MeasureMutatorDuration(duration, TimeProvider.System);

        public IMutator<TG, TS, TP> MeasureMutatorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringMutator<TG, TS, TP>(mutator, duration, timeProvider);

        public IMutator<TG, TS, TP> MeasureMutatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return mutator.MeasureMutatorDuration(duration);
        }

        public IMutator<TG, TS, TP> MeasureMutatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return mutator.MeasureMutatorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringMutator<TG, TS, TP>
        : WrappingMutator<TG, TS, TP>
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public DurationMeasuringMutator(
            IMutator<TG, TS, TP> mutator,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(mutator)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<TG> Mutate(
            IReadOnlyList<TG> parents,
            InnerMutate innerMutate,
            IRandomNumberGenerator random,
            TS searchSpace,
            TP problem)
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
