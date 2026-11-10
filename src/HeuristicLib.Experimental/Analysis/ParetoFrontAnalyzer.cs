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
public sealed class ParetoFrontAnalyzer<T> : Instrument<ParetoFront<T>>, IObservationInstrument<EvaluationObservation<T>>
{
    private readonly ValueArray<IOperator<Evaluation, T>> evaluators;

    public ParetoFrontAnalyzer(ObjectiveDirections problemObjective,
                               ObjectiveVector referencePoint,
                               params IReadOnlyList<IOperator<Evaluation, T>> evaluators)
    {
        if (evaluators.Count == 0)
            throw new ArgumentException("An analyzer needs at least one observation source.", nameof(evaluators));

        Objective = problemObjective;
        ReferencePoint = referencePoint;
        this.evaluators = evaluators.ToValueArray();
    }

    public ObjectiveDirections Objective { get; }
    public ObjectiveVector ReferencePoint { get; }

    protected override IReadOnlyList<Substitution> CreateSubstitutions() => [.. evaluators.Select(evaluator => evaluator.Invocations.RecordingInto(this))];

    Action<EvaluationObservation<T>> IObservationInstrument<EvaluationObservation<T>>.CreateRecorder(RunResults results) => Record(results.Result(this));

    protected override ParetoFront<T> CreateResult(RunResults results) => new(ReferencePoint, Objective);

    private static Action<EvaluationObservation<T>> Record(ParetoFront<T> front) =>
        observation => front.AddPoints(observation.Candidates.ToEvaluated(observation.ObjectiveVectors));
}
