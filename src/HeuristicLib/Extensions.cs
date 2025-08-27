using System.Diagnostics.Contracts;

namespace HEAL.HeuristicLib;

public static class Extensions {
  public static bool IsAlmost(this double a, double b, double tolerance = 1E-10) {
    return Math.Abs(a - b) <= tolerance;
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
