using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public static class EvaluatorDurationExtensions
{
    extension<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public IEvaluator<TG, TS, TP> MeasureEvaluatorDuration(ObservationDuration duration)
            => evaluator.MeasureEvaluatorDuration(duration, TimeProvider.System);

        public IEvaluator<TG, TS, TP> MeasureEvaluatorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringEvaluator<TG, TS, TP>(evaluator, duration, timeProvider);

        public IEvaluator<TG, TS, TP> MeasureEvaluatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return evaluator.MeasureEvaluatorDuration(duration);
        }

        public IEvaluator<TG, TS, TP> MeasureEvaluatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return evaluator.MeasureEvaluatorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringEvaluator<TG, TS, TP>
        : WrappingEvaluator<TG, TS, TP>
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public DurationMeasuringEvaluator(
            IEvaluator<TG, TS, TP> evaluator,
            ObservationDuration duration,
            TimeProvider timeProvider)
            : base(evaluator)
        {
            Duration = duration;
            TimeProvider = timeProvider;
        }

        private ObservationDuration Duration { get; }
        private TimeProvider TimeProvider { get; }

        protected override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<TG> genotypes,
            InnerEvaluate innerEvaluate,
            IRandomNumberGenerator random,
            TS searchSpace,
            TP problem)
        {
            var startTimestamp = TimeProvider.GetTimestamp();
            try
            {
                return innerEvaluate(genotypes, random, searchSpace, problem);
            }
            finally
            {
                Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}
