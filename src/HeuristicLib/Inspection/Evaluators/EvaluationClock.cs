using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>
/// Counts the candidates one chosen evaluator has evaluated.
/// </summary>
/// <remarks>
/// This is the axis that makes runs of different algorithms comparable. Several evaluator boundaries may count
/// different work, which is why the evaluator is named rather than inferred.
/// </remarks>
public sealed class EvaluationClock<TCandidate>
    : Clock<long>
{
    private long evaluations;

    public EvaluationClock(IOperator<Evaluation, TCandidate> evaluator)
    {
        Advices = Aspect.Observe(evaluator, Record).Advices;
    }

    protected override long ReadTime() => Interlocked.Read(ref evaluations);

    private void Record(OperatorObservation<Evaluation, TCandidate> observation) =>
        Interlocked.Add(ref evaluations, observation.ObjectiveVectors.Count);
}

public static class EvaluatorClockExtensions
{
    extension(Clock)
    {
        /// <summary>
        /// Creates a clock counting the candidates one chosen evaluator has evaluated.
        /// </summary>
        public static EvaluationClock<TCandidate> FromEvaluations<TCandidate>(IOperator<Evaluation, TCandidate> evaluator) =>
            new(evaluator);
    }
}
