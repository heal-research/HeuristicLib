using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public sealed record DurationMeasuringEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator => InnerEvaluator;
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, ObservationDuration duration)
        : this(evaluator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, ObservationDuration duration, TimeProvider timeProvider)
        : base(evaluator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, Duration, TimeProvider);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(innerEvaluator)
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return InnerEvaluator.Evaluate(candidates, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class EvaluatorDurationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public DurationMeasuringEvaluator<TCandidate, TSearchSpace, TProblem> MeasureEvaluatorDuration(ObservationDuration duration) => new(evaluator, duration);

        public DurationMeasuringEvaluator<TCandidate, TSearchSpace, TProblem> MeasureEvaluatorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(evaluator, duration, timeProvider);

        public DurationMeasuringEvaluator<TCandidate, TSearchSpace, TProblem> MeasureEvaluatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(evaluator, duration);
        }

        public DurationMeasuringEvaluator<TCandidate, TSearchSpace, TProblem> MeasureEvaluatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(evaluator, duration, timeProvider);
        }
    }
}
