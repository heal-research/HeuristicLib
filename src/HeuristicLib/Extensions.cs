using System.Collections;
using YamlDotNet.Core.Tokens;

namespace HEAL.HeuristicLib;

public static class Extensions {
  public static bool IsAlmost(this double a, double b, double tolerance = 1E-10) {
    return Math.Abs(a - b) <= tolerance;
  }

  public static T1[] GetMinBy<T1, T2>(
    this IEnumerable<T1> source,
    Func<T1, T2> selector,
    IComparer<T2> comparer,
    int m) {
    if (source == null)
      throw new ArgumentNullException(nameof(source));
    if (selector == null)
      throw new ArgumentNullException(nameof(selector));
    if (comparer == null)
      throw new ArgumentNullException(nameof(comparer));
    if (m <= 0)
      return [];

    // Reverse comparer so PriorityQueue (a min-heap) acts like a max-heap by T2.
    var rev = Comparer<T2>.Create((a, b) => comparer.Compare(b, a));

    var pq = new PriorityQueue<(T1 item, T2 key), T2>(rev);

    foreach (var x in source) {
      var k = selector(x);

      if (pq.Count < m) {
        pq.Enqueue((x, k), k);
      } else {
        // Push then drop the worst; if 'x' isn't better, it'll be the one that gets removed.
        pq.Enqueue((x, k), k);
        pq.Dequeue(); // removes current "largest by comparer", i.e., the worst
      }
    }

    // Drain and sort ascending by your original comparer (optional; drop Sort if any order is fine)
    var result = new List<(T1 item, T2 key)>(pq.Count);
    while (pq.TryDequeue(out var e, out _))
      result.Add(e);

    result.Sort((a, b) => comparer.Compare(a.key, b.key));
    return result.Select(e => e.item).ToArray();
  }

  // Convenience overload using default comparer for T2
  public static T1[] GetMinBy<T1, T2>(
    this IEnumerable<T1> source,
    Func<T1, T2> selector,
    int m)
    where T2 : IComparable<T2>
    => GetMinBy(source, selector, Comparer<T2>.Default, m);

  public static TValue GetOrInitialize<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue) {
    if (dict.TryGetValue(key, out var v))
      return v;
    return dict[key] = defaultValue;
  }

  public static int Count(this Range r) {
    return r.End.Value - r.Start.Value;
  }
}
