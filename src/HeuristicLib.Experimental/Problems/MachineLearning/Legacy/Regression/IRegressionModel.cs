namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public interface IRegressionModel
{
    IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows);

    double Predict(Dataset data, int row) => Predict(data, [row]).First();
}
