namespace HEAL.HeuristicLib.Problems.DataAnalysis.Clustering;

public interface IDistance<in TKey>
{
  double GetDistance(TKey a, TKey b);
}
