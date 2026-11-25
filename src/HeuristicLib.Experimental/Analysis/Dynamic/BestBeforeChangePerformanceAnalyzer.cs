using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Defines the completed-epoch performance analysis of the observed evaluators.</summary>
/// <remarks>
/// Each run owns a separate result. The problem is read from each evaluation observation and must be
/// <typeparamref name="TProblem"/>. Updates are synchronized; read the live result after execution has stopped.
/// The objective value selector is a delegate, so configurations holding this analyzer are not persistable.
/// </remarks>
public sealed class BestBeforeChangePerformanceAnalyzer<TCandidate, TSearchSpace, TProblem>
    : Analyzer<BestBeforeChangePerformance<TCandidate>>, IObservationReceiver<EvaluationObservation<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IDynamicProblem<TCandidate, TSearchSpace>
{
    private readonly ValueArray<IOperator<Evaluation, TCandidate>> evaluators;
    private readonly Func<ObjectiveVector, double> objectiveValueSelector;

    /// <param name="evaluators">At least one evaluator, matched and deduplicated by reference.</param>
    /// <param name="objectiveValueSelector">Reads a scalar performance value; omitted means the first objective value.</param>
    /// <param name="predictionEpochMultiplier">Multiplies the epoch after the last completed epoch to set the prediction horizon.</param>
    /// <exception cref="InvalidOperationException"><paramref name="evaluators"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="predictionEpochMultiplier"/> is not positive.</exception>
    public BestBeforeChangePerformanceAnalyzer(IReadOnlyList<IOperator<Evaluation, TCandidate>> evaluators,
        Func<ObjectiveVector, double>? objectiveValueSelector = null, int predictionEpochMultiplier = 10)
    {
        if (evaluators.Count == 0)
            throw new InvalidOperationException("An analyzer needs at least one observation source.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(predictionEpochMultiplier);
        this.evaluators = [.. evaluators.Distinct<IOperator<Evaluation, TCandidate>>(ReferenceEqualityComparer.Instance)];
        this.objectiveValueSelector = objectiveValueSelector ?? (static vector => vector[0]);
        PredictionEpochMultiplier = predictionEpochMultiplier;
    }

    public int PredictionEpochMultiplier { get; }

    protected override IReadOnlyList<ISubstitution> CreateSubstitutions() => [.. evaluators.Select(evaluator => evaluator.Invocations.RecordingInto(this))];

    Action<EvaluationObservation<TCandidate>> IObservationReceiver<EvaluationObservation<TCandidate>>.CreateRecorder(RunResults results) => Record(results.Result(this));

    /// <inheritdoc/>
    public override BestBeforeChangePerformance<TCandidate> CreateExecutionNode(ResolutionScope scope) =>
        new(objectiveValueSelector, PredictionEpochMultiplier);

    private static Action<EvaluationObservation<TCandidate>> Record(BestBeforeChangePerformance<TCandidate> result) => observation =>
    {
        if (observation.Problem is not TProblem problem)
            throw new InvalidOperationException($"The performance analysis requires {typeof(TProblem).Name}, but the evaluation used {observation.Problem.GetType().Name}.");
        result.ReadBatch(observation, problem.CurrentEpoch, problem.Objective.TotalOrderComparer);
    };
}

/// <summary>The best evaluated candidate of a published epoch and its selected scalar performance value.</summary>
public readonly record struct BestBeforeChangePerformanceEntry<TCandidate>(
    TCandidate Candidate,
    ObjectiveVector ObjectiveVector,
    double ObjectiveValue,
    int Epoch);
