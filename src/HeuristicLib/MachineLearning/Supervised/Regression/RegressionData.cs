using HEAL.HeuristicLib.Data;

namespace HEAL.HeuristicLib.MachineLearning;

public sealed class RegressionData(DataFrame inputs, Series<double> target)
    : SupervisedData<double>(inputs, target);
