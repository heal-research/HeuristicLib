namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

/// <summary>
/// Represents a regression data analysis solution
/// </summary>
public class CachedRegressionSolution(IRegressionModel solution) : IRegressionModel {
  protected readonly Dictionary<int, double> EvaluationCache = new();
  protected readonly IRegressionModel Solution = solution;

  public IEnumerable<double> GetEstimatedValues(Dataset data, IEnumerable<int> rows) {
    var rowsP = rows as ICollection<int> ?? rows.ToArray();
    var rowsToEvaluate = rowsP.Where(row => !EvaluationCache.ContainsKey(row)).ToList();
    var rowsEnumerator = rowsToEvaluate.GetEnumerator();
    using var valuesEnumerator = Solution.Predict(data, rowsToEvaluate).GetEnumerator();
    while (rowsEnumerator.MoveNext() & valuesEnumerator.MoveNext()) {
      EvaluationCache.Add(rowsEnumerator.Current, valuesEnumerator.Current);
    }

    return rowsP.Select(row => EvaluationCache[row]);
  }

  public IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows) => GetEstimatedValues(data, rows);
}
