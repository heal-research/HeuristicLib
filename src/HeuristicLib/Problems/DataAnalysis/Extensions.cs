namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public static class Extensions {
  public static IEnumerable<int> Enumerate(this Range r) {
    var s = r.Start.Value;
    var e = r.End.Value;
    return s <= e ? Enumerable.Range(s, e - s) : Enumerable.Range(e, s - e).Reverse();
  }
}
