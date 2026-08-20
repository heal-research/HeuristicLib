namespace HEAL.HeuristicLib.DataAnalysis.Regression;

public sealed class RegressionData(DataFrame inputs, Series<double> target)
    : SupervisedData<double>(inputs, target);
