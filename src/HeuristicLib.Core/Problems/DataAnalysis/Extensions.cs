namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public static class Extensions {
  public static IEnumerable<int> Enumerate(this Range r) {
    var s = r.Start.Value;
    var e = r.End.Value;
    return s <= e ? Enumerable.Range(s, e - s) : Enumerable.Range(e, s - e).Reverse();
  }

  public static IEnumerable<TRes> PairwiseRoundRobin<T, TRes>(this IEnumerable<T> source, Func<T, T, TRes> func) {
    T first = default!;
    var count = 0;
    T last = default!;
    foreach (var item in source) {
      if (count == 0) {
        first = item;
      } else {
        yield return func(last, item);
      }

      last = item;
      count++;
    }

    if (count > 1)
      yield return func(last, first);
  }
}
