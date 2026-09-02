using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public sealed record DurationMeasuringEvaluator<TCandidate>
    : WrappingEvaluator<TCandidate>
{
    public ObservationDuration Duration { get; init; }
    public TimeProvider TimeProvider { get; init; }

    public DurationMeasuringEvaluator(IEvaluator<TCandidate> childEvaluator, ObservationDuration duration)
        : this(childEvaluator, duration, TimeProvider.System)
    {
    }

    public DurationMeasuringEvaluator(IEvaluator<TCandidate> childEvaluator, ObservationDuration duration, TimeProvider timeProvider)
        : base(childEvaluator)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    protected override IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childEvaluator, Duration, TimeProvider);

    private sealed class Instance<TSearchSpace, TProblem>(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, ObservationDuration duration, TimeProvider timeProvider)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var startTimestamp = timeProvider.GetTimestamp();
            try
            {
                return ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
            }
            finally
            {
                duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp));
            }
        }
    }
}

public static class DurationMeasuringEvaluator
{
    public static DurationMeasuringEvaluator<TCandidate> Create<TCandidate>(IEvaluator<TCandidate> childEvaluator, ObservationDuration duration) =>
        new(childEvaluator, duration);

    public static DurationMeasuringEvaluator<TCandidate> Create<TCandidate>(IEvaluator<TCandidate> childEvaluator, ObservationDuration duration, TimeProvider timeProvider) =>
        new(childEvaluator, duration, timeProvider);
}

public static class EvaluatorDurationExtensions
{
    extension<TCandidate>(IEvaluator<TCandidate> evaluator)
    {
        public DurationMeasuringEvaluator<TCandidate> MeasureEvaluatorDuration(ObservationDuration duration) => new(evaluator, duration);

        public DurationMeasuringEvaluator<TCandidate> MeasureEvaluatorDuration(ObservationDuration duration, TimeProvider timeProvider) =>
            new(evaluator, duration, timeProvider);

        public DurationMeasuringEvaluator<TCandidate> MeasureEvaluatorDuration(out ObservationDuration duration)
        {
            duration = new ObservationDuration();
            return new(evaluator, duration);
        }

        public DurationMeasuringEvaluator<TCandidate> MeasureEvaluatorDuration(out ObservationDuration duration, TimeProvider timeProvider)
        {
            duration = new ObservationDuration();
            return new(evaluator, duration, timeProvider);
        }
    }
}
