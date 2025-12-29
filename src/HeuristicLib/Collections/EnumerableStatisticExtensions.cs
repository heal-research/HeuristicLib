using System.Diagnostics.Contracts;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Collections;

public static class EnumerableStatisticExtensions {
  /// <param name="values"></param>
  extension(IEnumerable<double> values) {
    /// <summary>
    /// Calculates the median element of the enumeration.
    /// </summary>
    /// <returns></returns>
    public double Median() {
      // See unit tests for comparison with naive implementation
      return Quantile(values, 0.5);
    }

    /// <summary>
    /// Calculates the alpha-quantile element of the enumeration.
    /// </summary>
    /// <param name="alpha"></param>
    /// <returns></returns>
    public double Quantile(double alpha) {
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
    while (true) {
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
      while (true) { // Beginning of innermost loop.
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

  /// <param name="values"></param>
  extension(IEnumerable<double> values) {
    /// <summary>
    /// Calculates the range (max - min) of the enumeration.
    /// </summary>
    /// <returns></returns>
    public double Range() {
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
    /// <returns></returns>
    public double StandardDeviation() {
      return Math.Sqrt(Variance(values));
    }

    /// <summary>
    /// Calculates the population standard deviation of values.
    /// </summary>
    /// <returns></returns>
    public double StandardDeviationPop() {
      return Math.Sqrt(VariancePop(values));
    }

    /// <summary>
    /// Calculates the sample variance of values. (sum (x - x_mean)² / (n-1))
    /// </summary>
    /// <returns></returns>
    public double Variance() {
      return Variance(values, true);
    }

    /// <summary>
    /// Calculates the population variance of values. (sum (x - x_mean)² / n)
    /// </summary>
    /// <returns></returns>
    public double VariancePop() {
      return Variance(values, false);
    }
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

  extension(IEnumerable<double> values) {
    public IEnumerable<double> LimitToRange(double min, double max) {
      ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
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

    public double Kurtosis(bool unbiased = true) {
      // http://www.ats.ucla.edu/stat/mult_pkg/faq/general/kurtosis.htm
      var values1 = values.ToArray();
      var mean = values1.Average();
      double n = values1.Length;

      double s2 = 0;
      double s4 = 0;

      for (var i = 0; i < values1.Length; i++) {
        var dev = values1[i] - mean;
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

    public double Skewness(bool unbiased = true) {
      var values1 = values.ToArray();
      var mean = values1.Average();
      double n = values1.Length;

      double s2 = 0;
      double s3 = 0;

      for (var i = 0; i < values1.Length; i++) {
        var dev = values1[i] - mean;
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
}
