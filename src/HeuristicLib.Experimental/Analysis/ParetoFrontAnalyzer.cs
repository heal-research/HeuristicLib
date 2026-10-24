using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Accumulates the Pareto front over every candidate the observed evaluators produce.
/// </summary>
/// <remarks>
/// This analyzer holds its own data. It merges observations from several evaluators into one front, rather than
/// retaining a front snapshot after every observation. The front is an accumulator, so read it once the run has
/// finished.
/// </remarks>
public sealed class ParetoFrontAnalyzer<T> : AccumulatingAnalyzer
{
    private readonly ParetoFront<T> front;

    public ParetoFrontAnalyzer(ObjectiveDirections problemObjective,
                               ObjectiveVector referencePoint,
                               params IReadOnlyList<IOperator<Evaluation, T>> evaluators)
    {
        if (evaluators.Count == 0)
            throw new ArgumentException("An analyzer needs at least one observation source.", nameof(evaluators));

        front = new ParetoFront<T>(referencePoint, problemObjective);
        Advices = [.. evaluators.SelectMany(evaluator => Aspect.Observe(evaluator.Calls, Record))];
    }

    public ObjectiveDirections Objective => front.Objective;
    public ObjectiveVector ReferencePoint => front.ReferencePoint;

    /// <summary>The front this analyzer accumulates.</summary>
    public ParetoFront<T> Front => front;

    private void Record(EvaluationObservation<T> observation)
    {
        lock (Sync)
            front.AddPoints(observation.Candidates.ToEvaluated(observation.ObjectiveVectors));
    }
}
