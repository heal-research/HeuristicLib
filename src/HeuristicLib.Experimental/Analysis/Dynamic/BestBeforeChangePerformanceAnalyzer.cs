using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Defines the completed-epoch performance analysis of the observed evaluators.</summary>
/// <remarks>Each run owns a separate result. The problem is read from each evaluation observation.</remarks>
public sealed class BestBeforeChangePerformanceAnalyzer<TCandidate, TSearchSpace, TProblem>
    : Instrument<BestBeforeChangePerformance<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IDynamicProblem<TCandidate, TSearchSpace>
{
    private readonly ValueArray<IOperator<Evaluation, TCandidate>> evaluators;
    private readonly Func<ObjectiveVector, double> objectiveValueSelector;

    public BestBeforeChangePerformanceAnalyzer(IReadOnlyList<IOperator<Evaluation, TCandidate>> evaluators,
        Func<ObjectiveVector, double>? objectiveValueSelector = null, int predictionEpochMultiplier = 10)
    {
        if (evaluators.Count == 0)
            throw new ArgumentException("An analyzer needs at least one observation source.", nameof(evaluators));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(predictionEpochMultiplier);
        this.evaluators = evaluators.ToValueArray();
        this.objectiveValueSelector = objectiveValueSelector ?? (static vector => vector[0]);
        PredictionEpochMultiplier = predictionEpochMultiplier;
    }

    public int PredictionEpochMultiplier { get; }

    protected override BestBeforeChangePerformance<TCandidate> CreateResult(RunResults results) =>
        new(objectiveValueSelector, PredictionEpochMultiplier);

    protected override Aspect CreateAspect(BestBeforeChangePerformance<TCandidate> result, RunResults results) =>
        new Collection(evaluators, result);

    private sealed class Collection : Aspect
    {
        public Collection(IReadOnlyList<IOperator<Evaluation, TCandidate>> evaluators, BestBeforeChangePerformance<TCandidate> result)
        {
            Advices = [.. evaluators.SelectMany(evaluator => Aspect.Observe(evaluator.Invocations, observation =>
            {
                if (observation.Problem is not TProblem problem)
                    throw new InvalidOperationException($"The performance analysis requires {typeof(TProblem).Name}, but the evaluation used {observation.Problem.GetType().Name}.");
                result.ReadBatch(observation, problem.CurrentEpoch, problem.Objective.TotalOrderComparer);
            }))];
        }
    }
}

public readonly record struct BestBeforeChangePerformanceEntry<TCandidate>(
    TCandidate Candidate,
    ObjectiveVector ObjectiveVector,
    double ObjectiveValue,
    int Epoch);
