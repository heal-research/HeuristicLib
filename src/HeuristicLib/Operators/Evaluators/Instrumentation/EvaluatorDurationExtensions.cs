using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public static class EvaluatorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IEvaluator<TCandidate, TSearchSpace, TProblem> MeasureEvaluatorDuration(ObservationDuration duration)
            => evaluator.MeasureEvaluatorDuration(duration, TimeProvider.System);

        public IEvaluator<TCandidate, TSearchSpace, TProblem> MeasureEvaluatorDuration(ObservationDuration duration, TimeProvider timeProvider)
            => new DurationMeasuringEvaluator<TCandidate, TSearchSpace, TProblem>(evaluator, duration, timeProvider);

        public IEvaluator<TCandidate, TSearchSpace, TProblem> MeasureEvaluatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return evaluator.MeasureEvaluatorDuration(duration);
        }

        public IEvaluator<TCandidate, TSearchSpace, TProblem> MeasureEvaluatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return evaluator.MeasureEvaluatorDuration(duration, timeProvider);
        }
    }

    private sealed record DurationMeasuringEvaluator<TCandidate, TSearchSpace, TProblem>
        : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringEvaluator(
            IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
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
            IReadOnlyList<TCandidate> candidates,
            InnerEvaluate innerEvaluate,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
        {
            var startTimestamp = TimeProvider.GetTimestamp();
            try
            {
                return innerEvaluate(candidates, random, searchSpace, problem);
            }
            finally
            {
                Duration.AddDuration(TimeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}
