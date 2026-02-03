namespace HEAL.HeuristicLib.Collections;

public static class EnumerableExtensions
{

  /// <summary>
  ///   Compute the n-ary cartesian product of arbitrarily many sequences:
  ///   http://blogs.msdn.com/b/ericlippert/archive/2010/06/28/computing-a-cartesian-product-with-linq.aspx
  /// </summary>
  /// <typeparam name="T">The type of the elements inside each sequence</typeparam>
  /// <param name="sequences">The collection of sequences</param>
  /// <returns>An enumerable sequence of all the possible combinations of elements</returns>
  public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
  {
    IEnumerable<IEnumerable<T>> result = [[]];

    return sequences.Where(s => s.Any()).Aggregate(result, func: (current, s) => from seq in current from item in s select seq.Concat([item]));
  }

  /// <summary>
  ///   Compute all k-combinations of elements from the provided collection.
  ///   <param name="elements">The collection of elements</param>
  ///   <param name="k">The combination group size</param>
  ///   <returns>An enumerable sequence of all the possible k-combinations of elements</returns>
  /// </summary>
  public static IEnumerable<IEnumerable<T>> Combinations<T>(this IList<T> elements, int k)
  {
    ArgumentOutOfRangeException.ThrowIfGreaterThan(k, elements.Count);
    if (k == 1) {
      foreach (var element in elements) {
        yield return [element];
      }

      yield break;
    }

    var n = elements.Count;
    var range = Enumerable.Range(0, k).ToArray();
    var length = BinomialCoefficient(n, k);

    for (var i = 0; i < length; ++i) {
      yield return range.Select(x => elements[x]).ToArray();

      if (i == length - 1) {
        break;
      }
      var m = k - 1;
      var max = n - 1;

      while (range[m] == max) {
        --m;
        --max;
      }

      range[m]++;
      for (var j = m + 1; j < k; ++j) {
        range[j] = range[j - 1] + 1;
      }
    }
  }

  public static double AverageOrNaN<T>(this IEnumerable<T> source, Func<T, double> func)
  {
    double sum = 0;
    long count = 0;
    foreach (var x in source) {
      sum += func(x);
      count++;
    }

    return count == 0 ? double.NaN : sum / count;
  }

  /// <summary>
  ///   This function gets the total number of unique combinations based upon N and K,
  ///   where N is the total number of items and K is the size of the group.
  ///   It calculates the total number of unique combinations C(N, K) = N! / ( K! (N - K)! )
  ///   using the  recursion C(N+1, K+1) = (N+1 / K+1) * C(N, K).
  ///   <remarks>http://blog.plover.com/math/choose.html</remarks>
  ///   <remark>https://en.wikipedia.org/wiki/Binomial_coefficient#Multiplicative_formula</remark>
  ///   <param name="n">The number of elements</param>
  ///   <param name="k">The size of the group</param>
  ///   <returns>The binomial coefficient C(N, K)</returns>
  /// </summary>
  public static long BinomialCoefficient(long n, long k)
  {
    if (k > n) {
      return 0;
    }

    if (k == n) {
      return 1;
    }
    if (k > n - k) {
      k = n - k;
    }

    // enable explicit overflow checking for very large coefficients
    checked {
      long r = 1;
      for (long d = 1; d <= k; d++) {
        r *= n--;
        r /= d;
      }

      return r;
    }
  }

  /// <param name="source">The enumeration in which the items with a maximal value should be found.</param>
  /// <typeparam name="T">The type of the elements.</typeparam>
  extension<T>(IEnumerable<T> source)
  {
    /// <summary>
    ///   Selects all elements in the sequence that are maximal with respect to the given value.
    /// </summary>
    /// <remarks>
    ///   Runtime complexity of the operation is O(N).
    /// </remarks>
    /// <param name="valueSelector">The function that selects the value to compare.</param>
    /// <returns>All elements in the enumeration where the selected value is the maximum.</returns>
    public IEnumerable<T> MaxItems(Func<T, IComparable> valueSelector)
    {
      using var enumerator = source.GetEnumerator();
      if (!enumerator.MoveNext()) {
        return [];
      }
      var max = valueSelector(enumerator.Current);
      var result = new List<T> { enumerator.Current };

      while (enumerator.MoveNext()) {
        var item = enumerator.Current;
        var comparison = valueSelector(item);
        switch (comparison.CompareTo(max)) {
          case > 0:
            result.Clear();
            result.Add(item);
            max = comparison;

            break;
          case 0:
            result.Add(item);

            break;
        }
      }

      return result;
    }

    /// <summary>
    ///   Selects all elements in the sequence that are minimal with respect to the given value.
    /// </summary>
    /// <remarks>
    ///   Runtime complexity of the operation is O(N).
    /// </remarks>
    /// <param name="valueSelector">The function that selects the value.</param>
    /// <returns>All elements in the enumeration where the selected value is the minimum.</returns>
    public IEnumerable<T> MinItems(Func<T, IComparable> valueSelector)
    {
      using var enumerator = source.GetEnumerator();
      if (!enumerator.MoveNext()) {
        return [];
      }
      var min = valueSelector(enumerator.Current);
      var result = new List<T> { enumerator.Current };

      while (enumerator.MoveNext()) {
        var item = enumerator.Current;
        var comparison = valueSelector(item);
        switch (comparison.CompareTo(min)) {
          case < 0:
            result.Clear();
            result.Add(item);
            min = comparison;

            break;
          case 0:
            result.Add(item);

            break;
        }
      }

      return result;
    }
  }
}
