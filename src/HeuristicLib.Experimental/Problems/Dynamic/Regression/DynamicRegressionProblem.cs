using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Regression;

/// <summary>
/// Base class for dynamic regression problems that expose the regression data and prediction metrics of their current
/// epoch.
/// </summary>
public abstract class DynamicRegressionProblem<TSelf, TCandidate, TSearchSpace>
    : DynamicProblem<TSelf, TCandidate, TSearchSpace>
    where TSelf : DynamicRegressionProblem<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected DynamicRegressionProblem(
        ObjectiveDirections objective,
        TSearchSpace searchSpace,
        IRandomNumberGenerator environmentRandom,
        IEpochSchedule? epochSchedule = null,
        UpdatePolicy updatePolicy = UpdatePolicy.AfterEachEvaluation)
        : base(objective, searchSpace, environmentRandom, epochSchedule ?? new EvaluationCountSchedule(int.MaxValue), updatePolicy)
    {
    }

    /// <summary>
    /// Gets the regression data used to evaluate candidates in the current epoch.
    /// </summary>
    public abstract RegressionData CurrentData { get; }

    /// <summary>
    /// Gets the prediction metrics used to evaluate candidates.
    /// </summary>
    public abstract ImmutableArray<IRegressionMetric> PredictionMetrics { get; }
}
