using System.Diagnostics;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Optimization;

public static class Extensions {
  public static IReadOnlyList<TOut> ParallelSelect<TOut, TIn>(this IReadOnlyList<TIn> source,
                                                              IRandomNumberGenerator random, Func<int, TIn, IRandomNumberGenerator, TOut> action,
                                                              ParallelOptions? parallelOptions = null) {
    var selected = new TOut[source.Count];
    var randoms = random.Spawn(source.Count);

    if (parallelOptions == null) {
      Parallel.For(0, source.Count, i => {
        selected[i] = action(i, source[i], randoms[i]);
      });
    } else if (parallelOptions.MaxDegreeOfParallelism == 1) { //avoid parallel overhead
      for (var i = 0; i < source.Count; i++) {
        selected[i] = action(i, source[i], randoms[i]);
      }
    } else {
      Parallel.For(0, source.Count, parallelOptions, i => {
        selected[i] = action(i, source[i], randoms[i]);
      });
    }

    return selected;
  }

  public static IReadOnlyList<TOut> ParallelSelect<TOut, TIn>(this IEnumerable<TIn> source, IRandomNumberGenerator random, Func<int, TIn, IRandomNumberGenerator, TOut> action) => source.ToArray().ParallelSelect(random, action);

  public class DebugException(string message) : Exception(message);

  [Conditional("DEBUG")]
  public static void CheckDebug(bool value, string text) {
    if (!value)
      throw new DebugException(text);
  }

  extension<TGenotype>(IReadOnlyList<ISolution<TGenotype>> parents) {
    public IParents<TGenotype>[] ToGenotypePairs() {
      var offspringCount = parents.Count / 2;
      var parentPairs = new IParents<TGenotype>[offspringCount];
      for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
        parentPairs[i] = new Parents<TGenotype>(parents[j].Genotype, parents[j + 1].Genotype);
      return parentPairs;
    }

    public (ISolution<TGenotype>, ISolution<TGenotype>)[] ToSolutionPairs() {
      var offspringCount = parents.Count / 2;
      var parentPairs = new (ISolution<TGenotype>, ISolution<TGenotype>)[offspringCount];
      for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
        parentPairs[i] = (parents[j], parents[j + 1]);
      return parentPairs;
    }
  }

  public static bool IsAlmost(this double a, double b, double tolerance = 1E-10) {
    return Math.Abs(a - b) <= tolerance;
  }

  public static T1[] GetMinBy<T1, T2>(
    this IEnumerable<T1> source,
    Func<T1, T2> selector,
    IComparer<T2> comparer,
    int m) {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(selector);
    ArgumentNullException.ThrowIfNull(comparer);
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
