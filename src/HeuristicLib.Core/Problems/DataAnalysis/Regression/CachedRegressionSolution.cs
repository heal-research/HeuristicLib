namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

/// <summary>
///   Represents a regression data analysis Solution
/// </summary>
public class CachedRegressionSolution : IRegressionModel
{
  private readonly Dictionary<int, double> cache = new();
  private readonly IRegressionModel inner;

  public CachedRegressionSolution(IRegressionModel inner) => this.inner = inner;

  public IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows)
  {
    var rowList = rows as IList<int> ?? rows.ToList();
    var missing = rowList.Where(r => !cache.ContainsKey(r)).Distinct().ToList();
    if (missing.Count > 0) {
      foreach (var (p, r) in inner.Predict(data, missing).Zip(missing)) {
        cache[r] = p;
      }
    }

    return rowList.Select(r => cache[r]);
  }
}
