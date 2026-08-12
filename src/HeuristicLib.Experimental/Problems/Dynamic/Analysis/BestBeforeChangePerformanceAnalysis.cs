using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public sealed record BestBeforeChangePerformanceAnalysis<TCandidate, TSearchSpace, TProblem>
    : DynamicAnalysis<TCandidate, TSearchSpace, TProblem, BestBeforeChangePerformanceAnalysisResult<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
{
    private readonly Func<ObjectiveVector, double> objectiveValueSelector;

    public BestBeforeChangePerformanceAnalysis(TProblem problem,
                                               IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>>
                                                   evaluators,
                                               Func<ObjectiveVector, double>? objectiveValueSelector = null,
                                               int predictionEpochMultiplier = 10) : base(problem, evaluators)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(predictionEpochMultiplier);
        this.objectiveValueSelector = objectiveValueSelector ?? (static objectiveVector => objectiveVector[0]);
        PredictionEpochMultiplier = predictionEpochMultiplier;
    }

    public int PredictionEpochMultiplier { get; }

    public override BestBeforeChangePerformanceAnalysisResult<TCandidate> CreateInitialResult() =>
        new(Problem.Objective, objectiveValueSelector, PredictionEpochMultiplier);
}

public sealed class BestBeforeChangePerformanceAnalysisResult<TCandidate>(
    ObjectiveDirections objective,
    Func<ObjectiveVector, double> objectiveValueSelector,
    int predictionEpochMultiplier) : IDynamicAnalysisResult<TCandidate>
{
    private readonly List<BestBeforeChangePerformanceEntry<TCandidate>> bestBeforeChange = [];
    private readonly OnlineWeibullCurveModel predictionModel = new(double.NaN);
    private (TCandidate candidate, ObjectiveVector objective, EvaluationTiming timing)? currentEpochBest;
    private double objectiveValueSum;

    public IReadOnlyList<BestBeforeChangePerformanceEntry<TCandidate>> BestBeforeChange => bestBeforeChange;
    public double Performance => bestBeforeChange.Count == 0 ? double.NaN : objectiveValueSum / bestBeforeChange.Count;
    public double Prediction { get; private set; } = double.NaN;

    public void AfterEvaluationLog(object? sender,
                                   IReadOnlyList<(TCandidate candidate, ObjectiveVector objective,
                                       EvaluationTiming timing)> evaluationLog)
    {
        foreach (var evaluation in evaluationLog.Where(x => x.timing.Valid))
        {
            if (currentEpochBest is null)
            {
                currentEpochBest = evaluation;
                continue;
            }

            if (evaluation.timing.Epoch != currentEpochBest.Value.timing.Epoch)
            {
                Record(currentEpochBest.Value);
                currentEpochBest = evaluation;
                continue;
            }

            if (objective.TotalOrderComparer.Compare(evaluation.objective, currentEpochBest.Value.objective) < 0)
                currentEpochBest = evaluation;
        }
    }

    private void Record((TCandidate candidate, ObjectiveVector objective, EvaluationTiming timing) best)
    {
        var objectiveValue = objectiveValueSelector(best.objective);
        bestBeforeChange.Add(new BestBeforeChangePerformanceEntry<TCandidate>(
            best.candidate,
            best.objective,
            objectiveValue,
            best.timing));
        objectiveValueSum += objectiveValue;

        predictionModel.AddObservation(best.timing.Epoch, objectiveValue);
        Prediction = predictionModel.Predict((best.timing.Epoch + 1) * predictionEpochMultiplier);
    }
}

public readonly record struct BestBeforeChangePerformanceEntry<TCandidate>(
    TCandidate Candidate,
    ObjectiveVector ObjectiveVector,
    double ObjectiveValue,
    EvaluationTiming Timing);
