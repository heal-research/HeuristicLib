namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public static class Metrics
{
    public static IRegressionMetric RMSE { get; } = new RootMeanSquaredErrorMetric();
    public static IRegressionMetric R2 { get; } = new R2ScoreMetric();
}
