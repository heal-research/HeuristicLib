namespace HEAL.HeuristicLib.Problems.DataAnalysis.Clustering;

public interface IClusteringModel
{
  IEnumerable<int> GetClusterValues(Dataset dataset, IEnumerable<int> rows);

  double GetClusterValue(Dataset data, int rows) => GetClusterValues(data, [rows]).First();
}
