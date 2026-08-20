namespace HEAL.HeuristicLib.DataAnalysis.Regression;

public static class Metrics
{
    public static IRegressionMetric MSE { get; } = new MeanSquaredErrorMetric();
    public static IRegressionMetric RMSE { get; } = new RootMeanSquaredErrorMetric();
    public static IRegressionMetric MAE { get; } = new MeanAbsoluteErrorMetric();
    public static IRegressionMetric MaxAbsoluteError { get; } = new MaximumAbsoluteErrorMetric();
    public static IRegressionMetric MeanLogError { get; } = new MeanLogErrorMetric();
    public static IRegressionMetric MeanRelativeError { get; } = new MeanRelativeErrorMetric();
    public static IRegressionMetric NMSE { get; } = new NormalizedMeanSquaredErrorMetric();
    public static IRegressionMetric R2 { get; } = new R2ScoreMetric();
    public static IRegressionMetric PearsonR2 { get; } = new PearsonR2Metric();
}
