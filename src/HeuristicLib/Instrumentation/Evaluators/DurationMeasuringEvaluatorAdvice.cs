using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Instrumentation;

public sealed class DurationMeasuringEvaluatorAdvice<TCandidate>
    : EvaluatorAdvice<TCandidate>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringEvaluatorAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringEvaluatorAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override WrapperExecutionFactory<IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IEvaluator<TCandidate> evaluator) =>
        childEvaluator => new Execution<TRunSearchSpace, TRunProblem>(childEvaluator, Duration, TimeProvider);

    private sealed class Execution<TSearchSpace, TProblem>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> childEvaluator, DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingEvaluatorExecution<TCandidate, TSearchSpace, TProblem>(childEvaluator)
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

public sealed record EvaluatorDurationMeasuring<TCandidate> : IOperatorDurationMeasuring<IEvaluator<TCandidate>>
{
    public Advice<IEvaluator<TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringEvaluatorAdvice<TCandidate>(duration, timeProvider);
}

public static class EvaluatorDurationExtensions
{
    extension(Aspect)
    {
        /// <summary>Measures the time spent in one chosen evaluator.</summary>
        public static Aspect MeasureDuration<TCandidate>(IEvaluator<TCandidate> evaluator, DurationAccumulator duration) =>
            Aspect.Create(Pointcut.Reference(evaluator), new DurationMeasuringEvaluatorAdvice<TCandidate>(duration));

        public static Aspect MeasureDuration<TCandidate>(IEvaluator<TCandidate> evaluator, DurationAccumulator duration, TimeProvider timeProvider) =>
            Aspect.Create(Pointcut.Reference(evaluator), new DurationMeasuringEvaluatorAdvice<TCandidate>(duration, timeProvider));
    }
}
