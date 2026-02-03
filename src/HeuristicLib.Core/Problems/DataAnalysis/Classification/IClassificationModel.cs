namespace HEAL.HeuristicLib.Problems.DataAnalysis.Classification;

/// <summary>
/// Interface for all classification models.
/// <remarks>All methods and properties in this interface must be implemented thread safely</remarks>
/// </summary>
public interface IClassificationModel {
  IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows);

  double Predict(Dataset data, int rows) => Predict(data, [rows]).First();
}
