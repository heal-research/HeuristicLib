using System;
using System.Diagnostics.Contracts;
using HEAL.HeuristicLib.Random;
using YamlDotNet.Core.Tokens;

namespace HEAL.HeuristicLib;

public static class Extensions {
  public static bool IsAlmost(this double a, double b, double tolerance = 1E-10) {
    return Math.Abs(a - b) <= tolerance;
  }

  public static TValue GetOrInitialize<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue) {
    if (dict.TryGetValue(key, out var v)) return v;
    return dict[key] = defaultValue;
  }
}

public static class EnumerableExtensions {
  /// <summary>
  /// Selects all elements in the sequence that are maximal with respect to the given value.
  /// </summary>
  /// <remarks>
  /// Runtime complexity of the operation is O(N).
  /// </remarks>
  /// <typeparam name="T">The type of the elements.</typeparam>
  /// <param name="source">The enumeration in which the items with a maximal value should be found.</param>
  /// <param name="valueSelector">The function that selects the value to compare.</param>
  /// <returns>All elements in the enumeration where the selected value is the maximum.</returns>
  public static IEnumerable<T> MaxItems<T>(this IEnumerable<T> source, Func<T, IComparable> valueSelector) {
    using var enumerator = source.GetEnumerator();
    if (!enumerator.MoveNext())
      return [];
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
  /// Selects all elements in the sequence that are minimal with respect to the given value.
  /// </summary>
  /// <remarks>
  /// Runtime complexity of the operation is O(N).
  /// </remarks>
  /// <typeparam name="T">The type of the elements.</typeparam>
  /// <param name="source">The enumeration in which items with a minimal value should be found.</param>
  /// <param name="valueSelector">The function that selects the value.</param>
  /// <returns>All elements in the enumeration where the selected value is the minimum.</returns>
  public static IEnumerable<T> MinItems<T>(this IEnumerable<T> source, Func<T, IComparable> valueSelector) {
    using var enumerator = source.GetEnumerator();
    if (!enumerator.MoveNext())
      return [];
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

  /// <summary>
  /// Compute the n-ary cartesian product of arbitrarily many sequences: http://blogs.msdn.com/b/ericlippert/archive/2010/06/28/computing-a-cartesian-product-with-linq.aspx
  /// </summary>
  /// <typeparam name="T">The type of the elements inside each sequence</typeparam>
  /// <param name="sequences">The collection of sequences</param>
  /// <returns>An enumerable sequence of all the possible combinations of elements</returns>
  public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences) {
    IEnumerable<IEnumerable<T>> result = [[]];
    return sequences.Where(s => s.Any()).Aggregate(result, (current, s) => from seq in current from item in s select seq.Concat([item]));
  }

  /// <summary>
  /// Compute all k-combinations of elements from the provided collection.
  /// <param name="elements">The collection of elements</param>
  /// <param name="k">The combination group size</param>
  /// <returns>An enumerable sequence of all the possible k-combinations of elements</returns>
  /// </summary>
  public static IEnumerable<IEnumerable<T>> Combinations<T>(this IList<T> elements, int k) {
    if (k > elements.Count)
      throw new ArgumentException("k is larger than the number of elements", nameof(k));

    if (k == 1) {
      foreach (var element in elements)
        yield return [element];
      yield break;
    }

    var n = elements.Count;
    var range = Enumerable.Range(0, k).ToArray();
    var length = BinomialCoefficient(n, k);

    for (var i = 0; i < length; ++i) {
      yield return range.Select(x => elements[x]).ToArray();

      if (i == length - 1)
        break;
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

  /// <summary>
  /// This function gets the total number of unique combinations based upon N and K,
  /// where N is the total number of items and K is the size of the group.
  /// It calculates the total number of unique combinations C(N, K) = N! / ( K! (N - K)! )
  /// using the  recursion C(N+1, K+1) = (N+1 / K+1) * C(N, K).
  /// <remarks>http://blog.plover.com/math/choose.html</remarks>
  /// <remark>https://en.wikipedia.org/wiki/Binomial_coefficient#Multiplicative_formula</remark>
  /// <param name="n">The number of elements</param>
  /// <param name="k">The size of the group</param>
  /// <returns>The binomial coefficient C(N, K)</returns>
  /// </summary>
  public static long BinomialCoefficient(long n, long k) {
    if (k > n)
      return 0;
    if (k == n)
      return 1;
    if (k > n - k)
      k = n - k;

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
}

public static class EnumerableStatisticExtensions {
  /// <summary>
  /// Calculates the median element of the enumeration.
  /// </summary>
  /// <param name="values"></param>
  /// <returns></returns>
  public static double Median(this IEnumerable<double> values) {
    // See unit tests for comparison with naive implementation
    return Quantile(values, 0.5);
  }

  /// <summary>
  /// Calculates the alpha-quantile element of the enumeration.
  /// </summary>
  /// <param name="values"></param>
  /// <param name="alpha"></param>
  /// <returns></returns>
  public static double Quantile(this IEnumerable<double> values, double alpha) {
    // See unit tests for comparison with naive implementation
    var valuesArr = values.ToArray();
    var n = valuesArr.Length;
    if (n == 0)
      throw new InvalidOperationException("Enumeration contains no elements.");

    // "When N is even, statistics books define the median as the arithmetic mean of the elements k = N/2 
    // and k = N/2 + 1 (that is, N/2 from the bottom and N/2 from the top). 
    // If you accept such pedantry, you must perform two separate selections to find these elements."

    // return the element at Math.Ceiling (if n*alpha is fractional) or the average of two elements if n*alpha is integer.
    var pos = n * alpha;
    Contract.Assert(pos >= 0);
    Contract.Assert(pos < n);
    var isInteger = Math.Round(pos).IsAlmost(pos);
    if (isInteger) {
      return 0.5 * (Select((int)pos - 1, valuesArr) + Select((int)pos, valuesArr));
    }

    return Select((int)Math.Ceiling(pos) - 1, valuesArr);
  }

  // Numerical Recipes in C++, §8.5 Selecting the Mth Largest, O(n)
  // Given k in [0..n-1] returns an array value from array arr[0..n-1] such that k array values are 
  // less than or equal to the one returned. The input array will be rearranged to have this value in 
  // location arr[k], with all smaller elements moved to arr[0..k-1] (in arbitrary order) and all 
  // larger elements in arr[k+1..n-1] (also in arbitrary order).
  // 
  // Could be changed to Select<T> where T is IComparable but in this case is significantly slower for double values
  private static double Select(int k, double[] arr) {
    Contract.Assert(arr.GetLowerBound(0) == 0);
    Contract.Assert(k >= 0 && k < arr.Length);
    int i, j, mid, n = arr.Length;
    double a;
    var l = 0;
    var ir = n - 1;
    for (;;) {
      if (ir <= l + 1) {
        // Active partition contains 1 or 2 elements.
        if (ir == l + 1 && arr[ir] < arr[l]) {
          // if (ir == l + 1 && arr[ir].CompareTo(arr[l]) < 0) {
          // Case of 2 elements.
          // SWAP(arr[l], arr[ir]);
          (arr[l], arr[ir]) = (arr[ir], arr[l]);
        }

        return arr[k];
      }

      mid = l + ir >> 1; // Choose median of left, center, and right elements
      // SWAP(arr[mid], arr[l + 1]); // as partitioning element a. Also
      (arr[mid], arr[l + 1]) = (arr[l + 1], arr[mid]);

      if (arr[l] > arr[ir]) {
        // if (arr[l].CompareTo(arr[ir]) > 0) {  // rearrange so that arr[l] arr[ir] <= arr[l+1],
        // SWAP(arr[l], arr[ir]); . arr[ir] >= arr[l+1]
        (arr[l], arr[ir]) = (arr[ir], arr[l]);
      }

      if (arr[l + 1] > arr[ir]) {
        // if (arr[l + 1].CompareTo(arr[ir]) > 0) {
        // SWAP(arr[l + 1], arr[ir]);
        (arr[l + 1], arr[ir]) = (arr[ir], arr[l + 1]);
      }

      if (arr[l] > arr[l + 1]) {
        //if (arr[l].CompareTo(arr[l + 1]) > 0) {
        // SWAP(arr[l], arr[l + 1]);
        (arr[l], arr[l + 1]) = (arr[l + 1], arr[l]);
      }

      i = l + 1; // Initialize pointers for partitioning.
      j = ir;
      a = arr[l + 1]; // Partitioning element.
      for (;;) { // Beginning of innermost loop.
        do
          i++;
        while (arr[i] < a /* arr[i].CompareTo(a) < 0 */); // Scan up to find element > a.
        do
          j--;
        while (arr[j] > a /* arr[j].CompareTo(a) > 0 */); // Scan down to find element < a.
        if (j < i)
          break; // Pointers crossed. Partitioning complete.
        // SWAP(arr[i], arr[j]);
        (arr[i], arr[j]) = (arr[j], arr[i]);
      } // End of innermost loop.

      arr[l + 1] = arr[j]; // Insert partitioning element.
      arr[j] = a;
      if (j >= k)
        ir = j - 1; // Keep active the partition that contains the
      if (j <= k)
        l = i; // kth element.
    }
  }

  /// <summary>
  /// Calculates the range (max - min) of the enumeration.
  /// </summary>
  /// <param name="values"></param>
  /// <returns></returns>
  public static double Range(this IEnumerable<double> values) {
    var min = double.PositiveInfinity;
    var max = double.NegativeInfinity;
    var i = 0;
    foreach (var e in values) {
      if (min > e)
        min = e;
      if (max < e)
        max = e;
      i++;
    }

    if (i < 1)
      throw new ArgumentException("The enumerable must contain at least two elements", nameof(values));
    return max - min;
  }

  /// <summary>
  /// Calculates the sample standard deviation of values.
  /// </summary>
  /// <param name="values"></param>
  /// <returns></returns>
  public static double StandardDeviation(this IEnumerable<double> values) {
    return Math.Sqrt(Variance(values));
  }

  /// <summary>
  /// Calculates the population standard deviation of values.
  /// </summary>
  /// <param name="values"></param>
  /// <returns></returns>
  public static double StandardDeviationPop(this IEnumerable<double> values) {
    return Math.Sqrt(VariancePop(values));
  }

  /// <summary>
  /// Calculates the sample variance of values. (sum (x - x_mean)² / (n-1))
  /// </summary>
  /// <param name="values"></param>
  /// <returns></returns>
  public static double Variance(this IEnumerable<double> values) {
    return Variance(values, true);
  }

  /// <summary>
  /// Calculates the population variance of values. (sum (x - x_mean)² / n)
  /// </summary>
  /// <param name="values"></param>
  /// <returns></returns>
  public static double VariancePop(this IEnumerable<double> values) {
    return Variance(values, false);
  }

  private static double Variance(IEnumerable<double> values, bool sampleVariance) {
    var mN = 0;
    var mOldM = 0.0;
    var mOldS = 0.0;
    var mNewS = 0.0;
    foreach (var x in values) {
      mN++;
      if (mN == 1) {
        mOldM = x;
        mOldS = 0.0;
      } else {
        var mNewM = mOldM + (x - mOldM) / mN;
        mNewS = mOldS + (x - mOldM) * (x - mNewM);

        // set up for next iteration
        mOldM = mNewM;
        mOldS = mNewS;
      }
    }

    switch (mN) {
      case 0:
        return double.NaN;
      case 1:
        return 0.0;
    }

    if (sampleVariance)
      return mNewS / (mN - 1);
    else
      return mNewS / mN;
  }

  public static IEnumerable<double> LimitToRange(this IEnumerable<double> values, double min, double max) {
    if (min > max)
      throw new ArgumentException($"Minimum {min} is larger than maximum {max}.");
    foreach (var x in values) {
      if (double.IsNaN(x))
        yield return (max + min) / 2.0;
      else if (x < min)
        yield return min;
      else if (x > max)
        yield return max;
      else
        yield return x;
    }
  }

  public static double Kurtosis(this IEnumerable<double> valuesE, bool unbiased = true) {
    // http://www.ats.ucla.edu/stat/mult_pkg/faq/general/kurtosis.htm
    var values = valuesE.ToArray();
    var mean = values.Average();
    double n = values.Length;

    double s2 = 0;
    double s4 = 0;

    for (var i = 0; i < values.Length; i++) {
      var dev = values[i] - mean;
      s2 += dev * dev;
      s4 += dev * dev * dev * dev;
    }

    var m2 = s2 / n;
    var m4 = s4 / n;

    if (!unbiased)
      return m4 / (m2 * m2) - 3;
    var v = s2 / (n - 1);
    var a = n * (n + 1) / ((n - 1) * (n - 2) * (n - 3));
    var b = s4 / (v * v);
    var c = (n - 1) * (n - 1) / ((n - 2) * (n - 3));
    return a * b - 3 * c;
  }

  public static double Skewness(this IEnumerable<double> valuesE, bool unbiased = true) {
    var values = valuesE.ToArray();
    var mean = values.Average();
    double n = values.Length;

    double s2 = 0;
    double s3 = 0;

    for (var i = 0; i < values.Length; i++) {
      var dev = values[i] - mean;
      s2 += dev * dev;
      s3 += dev * dev * dev;
    }

    var m2 = s2 / n;
    var m3 = s3 / n;

    var g = m3 / Math.Pow(m2, 3 / 2.0);

    if (!unbiased)
      return g;
    var a = Math.Sqrt(n * (n - 1));
    var b = n - 2;
    return a / b * g;
  }
}

public static class RandomEnumerable {
  //algorithm taken from programming pearls page 127
  //IMPORTANT because IEnumerables with yield are used the seed must be specified to return always 
  //the same sequence of numbers without caching the values.
  public static IEnumerable<int> SampleRandomNumbers(this IRandom generator, int start, int end, int count) {
    var remaining = end - start;
    for (var i = start; i < end && count > 0; i++) {
      var probability = generator.Random();
      if (probability < (double)count / remaining) {
        count--;
        yield return i;
      }

      remaining--;
    }
  }

  /// <summary>
  /// Chooses one element from a sequence giving each element an equal chance.
  /// </summary>
  /// <remarks>
  /// Runtime complexity is O(1) for sequences that are of type <see cref="IList{T}"/> and
  /// O(N) for all others.
  /// </remarks>
  /// <exception cref="ArgumentException">If the sequence is empty.</exception>
  /// <typeparam name="T">The type of the items to be selected.</typeparam>
  /// <param name="source">The sequence of elements.</param>
  /// <param name="random">The random number generator to use, its NextDouble() method must produce values in the range [0;1)</param>
  /// <param name="count">The number of items to be selected.</param>
  /// <returns>An element that has been chosen randomly from the sequence.</returns>
  public static T SampleRandom<T>(this IEnumerable<T> source, IRandom random) {
    return source.SampleRandom(random, 1).First();
  }

  /// <summary>
  /// Chooses <paramref name="count"/> elements from a sequence with repetition with equal chances for each element.
  /// </summary>
  /// <remarks>
  /// Runtime complexity is O(count) for sequences that are <see cref="IList{T}"/> and
  /// O(N * count) for all other. No exception is thrown if the sequence is empty.
  /// 
  /// The method is online.
  /// </remarks>
  /// <typeparam name="T">The type of the items to be selected.</typeparam>
  /// <param name="source">The sequence of elements.</param>
  /// <param name="random">The random number generator to use, its NextDouble() method must produce values in the range [0;1)</param>
  /// <param name="count">The number of items to be selected.</param>
  /// <returns>A sequence of elements that have been chosen randomly.</returns>
  public static IEnumerable<T> SampleRandom<T>(this IEnumerable<T> source, IRandom random, int count) {
    if (source is IList<T> listSource) {
      while (count > 0) {
        yield return listSource[random.Integer(listSource.Count)];
        count--;
      }
    } else {
      while (count > 0) {
        using var enumerator = source.GetEnumerator();
        enumerator.MoveNext();
        var selectedItem = enumerator.Current;
        var counter = 1;
        while (enumerator.MoveNext()) {
          counter++;
          if (counter * random.Random() < 1.0)
            selectedItem = enumerator.Current;
        }

        yield return selectedItem;
        count--;
      }
    }
  }

  /// <summary>
  /// Chooses <paramref name="count"/> elements from a sequence without repetition with equal chances for each element.
  /// The items are returned in the same order as they appear in the sequence.
  /// </summary>
  /// <remarks>
  /// Runtime complexity is O(N) for all sequences.
  /// No exception is thrown if the sequence contains less items than there are to be selected.
  /// 
  /// The method is online.
  /// </remarks>
  /// <typeparam name="T">The type of the items to be selected.</typeparam>
  /// <param name="source">The sequence of elements.</param>
  /// <param name="random">The random number generator to use, its NextDouble() method must produce values in the range [0;1)</param>
  /// <param name="count">The number of items to be selected.</param>
  /// <param name="sourceCount">Optional parameter specifying the number of elements in the source enumerations</param>
  /// <returns>A sequence of elements that have been chosen randomly.</returns>
  public static IEnumerable<T> SampleRandomWithoutRepetition<T>(this IEnumerable<T> source, IRandom random, int count, int sourceCount = -1) {
    if (sourceCount == -1) sourceCount = source.Count();
    var remaining = sourceCount;
    foreach (var item in source) {
      if (random.Random() * remaining < count) {
        count--;
        yield return item;
        if (count <= 0) break;
      }

      remaining--;
    }
  }

  public static T SampleProportional<T>(this IEnumerable<T> source, IRandom random, IEnumerable<double> weights) {
    return source.SampleProportional(random, weights, false, false).First();
  }

  public static T SampleProportional<T>(this IEnumerable<T> source, IEnumerable<double> weights, IRandom random) {
    return source.SampleProportional(random, weights);
  }

  /// <summary>
  /// Chooses elements out of a sequence with repetition. The chance that an item is selected is proportional or inverse-proportional
  /// to the <paramref name="weights"/>.
  /// </summary>
  /// <remarks>
  /// In case both <paramref name="inverseProportional"/> and <paramref name="windowing"/> are false values must be &gt; 0,
  /// otherwise an InvalidOperationException is thrown.
  /// 
  /// The method internally holds two arrays: One that is the sequence itself and another one for the values.
  /// </remarks>
  /// <typeparam name="T">The type of the items to be selected.</typeparam>
  /// <param name="source">The sequence of elements.</param>
  /// <param name="random">The random number generator to use, its NextDouble() method must produce values in the range [0;1)</param>
  /// <param name="count">The number of items to be selected.</param>
  /// <param name="weights">The weight values for the items.</param>
  /// <param name="windowing">Whether to scale the proportional values or not.</param>
  /// <param name="inverseProportional">Determines whether to choose proportionally (false) or inverse-proportionally (true).</param>
  /// <returns>A sequence of selected items. The sequence might contain the same item more than once.</returns>
  public static IEnumerable<T> SampleProportional<T>(this IEnumerable<T> source, IRandom random, int count, IEnumerable<double> weights, bool windowing = true, bool inverseProportional = false) {
    return source.SampleProportional(random, weights, windowing, inverseProportional).Take(count);
  }

  /// <summary>
  /// Same as <see also cref="SampleProportional<T>"/>, but chooses an item exactly once.
  /// </summary>
  /// <remarks>
  /// In case both <paramref name="inverseProportional"/> and <paramref name="windowing"/> are false values must be &gt; 0,
  /// otherwise an InvalidOperationException is thrown.
  /// 
  /// The method internally holds two arrays: One that is the sequence itself and another one for the values.
  /// 
  /// The method does not check if the number of elements in source and weights are the same.
  /// </remarks>
  /// <typeparam name="T">The type of the items to be selected.</typeparam>
  /// <param name="source">The sequence of elements.</param>
  /// <param name="random">The random number generator to use, its NextDouble() method must produce values in the range [0;1)</param>
  /// <param name="count">The number of items to be selected.</param>
  /// <param name="weights">The weight values for the items.</param>
  /// <param name="windowing">Whether to scale the proportional values or not.</param>
  /// <param name="inverseProportional">Determines whether to choose proportionally (true) or inverse-proportionally (false).</param>
  /// <returns>A sequence of selected items. Might actually be shorter than <paramref name="count"/> elements if source has less than <paramref name="count"/> elements.</returns>
  public static IEnumerable<T> SampleProportionalWithoutRepetition<T>(this IEnumerable<T> source, IRandom random, int count, IEnumerable<double> weights, bool windowing = true, bool inverseProportional = false) {
    return source.SampleProportionalWithoutRepetition(random, weights, windowing, inverseProportional).Take(count);
  }

  #region Proportional Helpers
  private static IEnumerable<T> SampleProportional<T>(this IEnumerable<T> source, IRandom random, IEnumerable<double> weights, bool windowing, bool inverseProportional) {
    var sourceArray = source.ToArray();
    var valueArray = PrepareProportional(weights, windowing, inverseProportional);
    var total = valueArray.Sum();

    while (true) {
      var index = 0;
      double ball = valueArray[index], sum = random.Random() * total;
      while (ball < sum)
        ball += valueArray[++index];
      yield return sourceArray[index];
    }
  }

  private static IEnumerable<T> SampleProportionalWithoutRepetition<T>(this IEnumerable<T> source, IRandom random, IEnumerable<double> weights, bool windowing, bool inverseProportional) {
    var valueArray = PrepareProportional(weights, windowing, inverseProportional);
    var list = new LinkedList<Tuple<T, double>>(source.Zip(valueArray, Tuple.Create));
    var total = valueArray.Sum();

    while (list.Count > 0) {
      var cur = list.First;
      double ball = cur.Value.Item2, sum = random.Random() * total; // assert: sum < total. When there is only one item remaining: sum < ball
      while (ball < sum && cur.Next != null) {
        cur = cur.Next;
        ball += cur.Value.Item2;
      }

      yield return cur.Value.Item1;
      list.Remove(cur);
      total -= cur.Value.Item2;
    }
  }

  private static double[] PrepareProportional(IEnumerable<double> weights, bool windowing, bool inverseProportional) {
    double maxValue = double.MinValue, minValue = double.MaxValue;
    var valueArray = weights.ToArray();

    for (var i = 0; i < valueArray.Length; i++) {
      if (valueArray[i] > maxValue) maxValue = valueArray[i];
      if (valueArray[i] < minValue) minValue = valueArray[i];
    }

    if (minValue == maxValue) { // all values are equal
      for (var i = 0; i < valueArray.Length; i++) {
        valueArray[i] = 1.0;
      }
    } else {
      if (windowing) {
        if (inverseProportional) InverseProportionalScale(valueArray, maxValue);
        else ProportionalScale(valueArray, minValue);
      } else {
        if (minValue < 0.0) throw new InvalidOperationException("Proportional selection without windowing does not work with values < 0.");
        if (inverseProportional) InverseProportionalScale(valueArray, 2 * maxValue);
      }
    }

    return valueArray;
  }

  private static void ProportionalScale(double[] values, double minValue) {
    for (var i = 0; i < values.Length; i++) {
      values[i] = values[i] - minValue;
    }
  }

  private static void InverseProportionalScale(double[] values, double maxValue) {
    for (var i = 0; i < values.Length; i++) {
      values[i] = maxValue - values[i];
    }
  }
  #endregion

  /// <summary>
  /// Shuffles an enumerable and returns a new enumerable according to the Fisher-Yates shuffle.
  /// </summary>
  /// <remarks>
  /// Note that the source enumerable is transformed into an array.
  /// 
  /// The implementation is described in http://stackoverflow.com/questions/1287567/c-is-using-random-and-orderby-a-good-shuffle-algorithm.
  /// </remarks>
  /// <typeparam name="T">The type of the items that are to be shuffled.</typeparam>
  /// <param name="source">The enumerable that contains the items.</param>
  /// <param name="random">The random number generator, its Next(n) method must deliver uniformly distributed random numbers in the range [0;n).</param>
  /// <returns>An enumerable with the elements shuffled.</returns>
  public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, IRandom random) {
    var elements = source.ToArray();
    for (var i = elements.Length - 1; i > 0; i--) {
      // Swap element "i" with a random earlier element (including itself)
      var swapIndex = random.Integer(i + 1);
      yield return elements[swapIndex];
      elements[swapIndex] = elements[i];
      // we don't actually perform the swap, we can forget about the
      // swapped element because we already returned it.
    }

    if (elements.Length > 0)
      yield return elements[0];
  }
}
