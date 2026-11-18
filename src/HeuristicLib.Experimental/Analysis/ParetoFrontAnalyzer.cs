using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Accumulates the Pareto front over every candidate the observed evaluators produce.
/// </summary>
/// <remarks>
/// Each run creates its own front. It merges observations from several evaluators into one front, rather than
/// retaining a front snapshot after every observation. The front is an accumulator, so read it once the run has
/// finished.
/// </remarks>
public sealed class ParetoFrontAnalyzer<T> : Analyzer<ParetoFront<T>>, IObservationReceiver<EvaluationObservation<T>>
{
    private readonly ValueArray<IOperator<Evaluation, T>> evaluators;

    /// <param name="problemObjective">Directions used to compare all observed objective vectors.</param>
    /// <param name="referencePoint">The reference point retained by the resulting front.</param>
    /// <param name="evaluators">At least one evaluator, matched and deduplicated by reference.</param>
    /// <exception cref="InvalidOperationException"><paramref name="evaluators"/> is empty.</exception>
    public ParetoFrontAnalyzer(ObjectiveDirections problemObjective,
                               ObjectiveVector referencePoint,
                               params IReadOnlyList<IOperator<Evaluation, T>> evaluators)
    {
        if (evaluators.Count == 0)
            throw new InvalidOperationException("An analyzer needs at least one observation source.");

        Objective = problemObjective;
        ReferencePoint = referencePoint;
        this.evaluators = [.. evaluators.Distinct<IOperator<Evaluation, T>>(ReferenceEqualityComparer.Instance)];
    }

    public ObjectiveDirections Objective { get; }
    public ObjectiveVector ReferencePoint { get; }

    protected override IReadOnlyList<ISubstitution> CreateSubstitutions() => [.. evaluators.Select(evaluator => evaluator.Invocations.RecordingInto(this))];

    Action<EvaluationObservation<T>> IObservationReceiver<EvaluationObservation<T>>.CreateRecorder(RunResults results) => Record(results.Result(this));

    /// <inheritdoc/>
    public override ParetoFront<T> CreateExecutionNode(ResolutionScope scope) => new(ReferencePoint, Objective);

    private static Action<EvaluationObservation<T>> Record(ParetoFront<T> front) =>
        observation => front.AddPoints(observation.Candidates.ToEvaluated(observation.ObjectiveVectors));
}
