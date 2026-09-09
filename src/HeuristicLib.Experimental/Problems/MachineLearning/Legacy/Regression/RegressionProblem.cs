using HEAL.HeuristicLib.Numerics;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public abstract class RegressionProblem<TSelf, TProblemData, TCandidate, TSearchSpace> : DataAnalysisProblem<TSelf, TProblemData, TCandidate, TSearchSpace>
    where TSelf : Problem<TSelf, TCandidate, TSearchSpace>
    where TProblemData : RegressionProblemData
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public const double PunishmentFactor = 10.0;
    private readonly int[] rowIndicesCache; // unsure if this is faster than using the enumerable directly

    private readonly double[] trainingTargetCache;

    protected RegressionProblem(TProblemData problemData, ICollection<IRegressionEvaluator<TCandidate>> objective, IComparer<ObjectiveVector> a, TSearchSpace encoding) : base(problemData, new ObjectiveDirections(objective.Select(x => x.Direction).ToArray(), a), encoding)
    {
        Evaluators = objective.ToList();
        trainingTargetCache = problemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training).ToArray();
        rowIndicesCache = problemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray();
        if (trainingTargetCache.Length == 0)
        {
            return;
        }
        var mean = trainingTargetCache.Average();
        var range = trainingTargetCache.Range();
        UpperPredictionBound = mean + PunishmentFactor * range;
        LowerPredictionBound = mean - PunishmentFactor * range;
    }
    public IReadOnlyList<IRegressionEvaluator<TCandidate>> Evaluators { get; set; }

    public double UpperPredictionBound { get; set; }

    public double LowerPredictionBound { get; set; }

    public override ObjectiveVector Evaluate(TCandidate candidate) => Evaluate(candidate, rowIndicesCache, trainingTargetCache);

    public ObjectiveVector Evaluate(TCandidate candidate, IReadOnlyList<int> rows, IReadOnlyList<double> targets)
    {
        var predictions = PredictAndTrain(candidate, rows, targets)
            .LimitToRange(LowerPredictionBound, UpperPredictionBound);
        if (Evaluators.Count == 1)
        {
            return new ObjectiveVector(Evaluators[0].Evaluate(candidate, predictions, targets));
        }
        if (predictions is not ICollection<double> materialPredictions)
        {
            materialPredictions = predictions.ToArray();
        }

        return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(candidate, materialPredictions, trainingTargetCache)));
    }

    public abstract IEnumerable<double> PredictAndTrain(TCandidate candidate, IReadOnlyList<int> rows, IReadOnlyList<double> targets);
}
