// ReSharper disable UnusedMember.Global
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery

#pragma warning disable S2368

namespace HEAL.HeuristicLib.Problems.TestFunctions.BBoB;

public static class Functions
{
  public static double FAttractiveSectorRaw(IReadOnlyList<double> x, FAttractiveSectorData data)
  {
    var result = 0.0;

    for (var i = 0; i < x.Count; i++) {
      if (data.XOpt[i] * x[i] > 0.0) {
        result += 100.0 * 100.0 * x[i] * x[i];
      } else {
        result += x[i] * x[i];
      }
    }

    return result;
  }

  public static double FBentCigarRaw(IReadOnlyList<double> x)
  {
    const double condition = 1.0e6;

    var result = x[0] * x[0];

    for (var i = 1; i < x.Count; i++) {
      result += condition * x[i] * x[i];
    }

    return result;
  }

  public static double FBentCigarGeneralizedRaw(IReadOnlyList<double> x, FBentCigarGeneralizedVersatileData data)
  {
    const double condition = 1.0e6;

    var result = 0.0;

    var nbLongAxes = x.Count / data.ProportionLongAxesDenom;
    if (x.Count % data.ProportionLongAxesDenom != 0) {
      nbLongAxes += 1;
    }

    for (var i = 0; i < nbLongAxes; i++) {
      result += x[i] * x[i];
    }

    for (var i = nbLongAxes; i < x.Count; i++) {
      result += condition * x[i] * x[i];
    }

    return result;
  }

  public static double FBuecheRastriginRaw(IReadOnlyList<double> x)
  {
    var tmp = 0.0;
    var tmp2 = 0.0;

    for (var i = 0; i < x.Count; i++) {
      tmp += Math.Cos(2.0 * Math.PI * x[i]);
      tmp2 += x[i] * x[i];
    }

    var result = 10.0 * (x.Count - tmp) + tmp2;

    return result;
  }

  public static double FDifferentPowersRaw(IReadOnlyList<double> x)
  {
    var sum = 0.0;

    for (var i = 0; i < x.Count; i++) {
      var exponent = 2.0 + 4.0 * i / (x.Count - 1.0);
      sum += Math.Pow(Math.Abs(x[i]), exponent);
    }

    var result = Math.Sqrt(sum);

    return result;
  }

  public static double FDiscusRaw(IReadOnlyList<double> x)
  {
    const double condition = 1.0e6;

    var result = condition * x[0] * x[0];

    for (var i = 1; i < x.Count; i++) {
      result += x[i] * x[i];
    }

    return result;
  }

  public static double FDiscusGeneralizedRaw(IReadOnlyList<double> x, FDiscusGeneralizedVersatileData data)
  {
    const double condition = 1.0e6;

    var nbShortAxes = x.Count / data.ProportionShortAxesDenom;
    if (x.Count % data.ProportionShortAxesDenom != 0) {
      nbShortAxes += 1;
    }

    var result = 0.0;

    // Short axes (scaled by condition)
    for (var i = 0; i < nbShortAxes; i++) {
      result += x[i] * x[i];
    }

    result *= condition;

    // Long axes
    for (var i = nbShortAxes; i < x.Count; i++) {
      result += x[i] * x[i];
    }

    return result;
  }

  public static double FEllipsoidRaw(IReadOnlyList<double> x)
  {
    const double condition = 1.0e6;

    var result = x[0] * x[0];

    for (var i = 1; i < x.Count; i++) {
      var exponent = i / (x.Count - 1.0);
      result += Math.Pow(condition, exponent) * x[i] * x[i];
    }

    return result;
  }

  public static double FGallagherRaw(IReadOnlyList<double> x, FGallagherData data)
  {
    var n = x.Count;
    const double a = 0.1;
    var f = 0.0;
    double tmp;
    var fPen = 0.0;
    double fTrue;

    var fac = -0.5 / n;

    // Boundary handling
    for (var i = 0; i < n; i++) {
      tmp = Math.Abs(x[i]) - 5.0;
      if (tmp > 0.0) {
        fPen += tmp * tmp;
      }
    }

    var fAdd = fPen;

    // Transformation in search space
    var tmx = new double[n];
    for (var i = 0; i < n; i++) {
      tmx[i] = 0.0;
      for (var j = 0; j < n; j++) {
        tmx[i] += data.Rotation[i][j] * x[j];
      }
    }

    // Computation core
    for (var i = 0; i < data.NumberOfPeaks; i++) {
      var tmp2 = 0.0;
      for (var j = 0; j < n; j++) {
        tmp = tmx[j] - data.XLocal[j][i];
        tmp2 += data.ArrScales[i][j] * tmp * tmp;
      }

      tmp2 = data.PeakValues[i] * Math.Exp(fac * tmp2);
      f = Math.Max(f, tmp2);
    }

    f = 10.0 - f;
    if (f > 0.0) {
      fTrue = Math.Log(f) / a;
      fTrue = Math.Pow(
        Math.Exp(fTrue + (0.49 * (Math.Sin(fTrue) + Math.Sin(0.79 * fTrue)))),
        a
      );
    } else if (f < 0.0) {
      fTrue = Math.Log(-f) / a;
      fTrue = -Math.Pow(
        Math.Exp(fTrue + (0.49 * (Math.Sin(0.55 * fTrue) + Math.Sin(0.31 * fTrue)))),
        a
      );
    } else {
      fTrue = f;
    }

    fTrue *= fTrue;
    fTrue += fAdd;

    return fTrue;
  }

  public static double FGriewankRosenbrockRaw(IReadOnlyList<double> x)
  {
    var result = 0.0;

    for (var i = 0; i < x.Count - 1; i++) {
      var c1 = (x[i] * x[i]) - x[i + 1];
      var c2 = 1.0 - x[i];
      var tmp = (100.0 * c1 * c1) + (c2 * c2);

      result += (tmp / 4000.0) - Math.Cos(tmp);
    }

    result = 10.0 + (10.0 * result / (x.Count - 1));

    return result;
  }

  public static double FKatsuuraRaw(IReadOnlyList<double> x)
  {
    var n = x.Count;
    var result = 1.0;

    for (var i = 0; i < n; i++) {
      var tmp = 0.0;

      for (var j = 1; j < 33; j++) {
        var tmp2 = Math.Pow(2.0, j);
        var value = tmp2 * x[i];
        tmp += Math.Abs(value - CocoDoubleRound(value)) / tmp2;
      }

      tmp = 1.0 + ((i + 1) * tmp);

      result *= Math.Pow(tmp, 10.0 / Math.Pow(n, 1.2));
    }

    result = 10.0 / (n * n) * (-1.0 + result);

    return result;
  }

  public static double FLinearSlopeRaw(IReadOnlyList<double> x, double[] bestParameter)
  {
    const double alpha = 100.0;

    var n = x.Count;
    var result = 0.0;

    for (var i = 0; i < n; i++) {
      var baseVal = Math.Sqrt(alpha);
      var exponent = i / (n - 1.0);

      var si = bestParameter[i] > 0.0
        ? Math.Pow(baseVal, exponent)
        : -Math.Pow(baseVal, exponent);

      // Boundary handling
      if (x[i] * bestParameter[i] < 25.0) {
        result += (5.0 * Math.Abs(si)) - (si * x[i]);
      } else {
        result += (5.0 * Math.Abs(si)) - (si * bestParameter[i]);
      }
    }

    return result;
  }

  public static double FLunacekBiRastriginRaw(IReadOnlyList<double> x, FLunacekBiRastriginData data)
  {
    var n = x.Count;

    if (n <= 1) {
      throw new ArgumentException("number_of_variables must be > 1", nameof(x));
    }

    const double condition = 100.0;
    const double mu0 = 2.5;
    const double d = 1.0;

    var penalty = 0.0;
    var sum1 = 0.0;
    var sum2 = 0.0;
    var sum3 = 0.0;

    var s = 1.0 - (0.5 / (Math.Sqrt(n + 20.0) - 4.1));
    var mu1 = -Math.Sqrt(((mu0 * mu0) - d) / s);

    // Boundary penalty
    for (var i = 0; i < n; i++) {
      var tmp = Math.Abs(x[i]) - 5.0;
      if (tmp > 0.0) {
        penalty += tmp * tmp;
      }
    }

    // x_hat
    for (var i = 0; i < n; i++) {
      data.XHat[i] = 2.0 * x[i];
      if (data.XOpt[i] < 0.0) {
        data.XHat[i] *= -1.0;
      }
    }

    var tmpVector = new double[n];

    // affine transformation: rot2, scaling, mu0 shift
    for (var i = 0; i < n; i++) {
      var c1 = Math.Pow(Math.Sqrt(condition), i / (n - 1.0));
      tmpVector[i] = 0.0;

      for (var j = 0; j < n; j++) {
        tmpVector[i] += c1 * data.Rot2[i][j] * (data.XHat[j] - mu0);
      }
    }

    // z = rot1 * tmpVector
    for (var i = 0; i < n; i++) {
      data.Z[i] = 0.0;
      for (var j = 0; j < n; j++) {
        data.Z[i] += data.Rot1[i][j] * tmpVector[j];
      }
    }

    // Core computation
    for (var i = 0; i < n; i++) {
      var dx0 = data.XHat[i] - mu0;
      var dx1 = data.XHat[i] - mu1;

      sum1 += dx0 * dx0;
      sum2 += dx1 * dx1;
      sum3 += Math.Cos(2.0 * Math.PI * data.Z[i]);
    }

    var result = Math.Min(sum1, (d * n) + (s * sum2))
                 + (10.0 * (n - sum3))
                 + (1e4 * penalty);

    return result;
  }

  public static double FRastriginRaw(IReadOnlyList<double> x)
  {
    var sum1 = 0.0;
    var sum2 = 0.0;

    for (var i = 0; i < x.Count; i++) {
      sum1 += Math.Cos(2.0 * Math.PI * x[i]);
      sum2 += x[i] * x[i];
    }

    // cos(inf) → NaN, so return early like original
    if (double.IsInfinity(sum2)) {
      return sum2;
    }

    var result = (10.0 * (x.Count - sum1)) + sum2;

    return result;
  }

  public static double FRosenbrockRaw(IReadOnlyList<double> x)
  {
    if (x.Count <= 1) {
      throw new ArgumentException("Rosenbrock function requires dimension > 1", nameof(x));
    }

    var s1 = 0.0;
    var s2 = 0.0;

    for (var i = 0; i < x.Count - 1; i++) {
      var tmp1 = (x[i] * x[i]) - x[i + 1];
      s1 += tmp1 * tmp1;

      var tmp2 = x[i] - 1.0;
      s2 += tmp2 * tmp2;
    }

    var result = (100.0 * s1) + s2;
    return result;
  }

  public static double FSchaffersRaw(IReadOnlyList<double> x)
  {
    if (x.Count <= 1) {
      throw new ArgumentException("Schaffers function requires dimension > 1.", nameof(x));
    }

    var result = 0.0;

    for (var i = 0; i < x.Count - 1; i++) {
      var tmp = (x[i] * x[i]) + (x[i + 1] * x[i + 1]);

      // Handle sin(inf) → NaN case, same logic as original C code
      var sinArg = 50.0 * Math.Pow(tmp, 0.1);
      if (double.IsInfinity(tmp) && double.IsNaN(Math.Sin(sinArg))) {
        return tmp;
      }

      var term = Math.Pow(tmp, 0.25) *
                 (1.0 + Math.Pow(Math.Sin(sinArg), 2.0));

      result += term;
    }

    result = Math.Pow(result / (x.Count - 1.0), 2.0);

    return result;
  }

  public static double FSchwefelRaw(IReadOnlyList<double> x)
  {
    var n = x.Count;

    var penalty = 0.0;
    var sum = 0.0;

    // Boundary handling
    for (var i = 0; i < n; i++) {
      var tmp = Math.Abs(x[i]) - 500.0;
      if (tmp > 0.0) {
        penalty += tmp * tmp;
      }
    }

    // Core computation
    for (var i = 0; i < n; i++) {
      sum += x[i] * Math.Sin(Math.Sqrt(Math.Abs(x[i])));
    }

    var result = 0.01 * (penalty + 418.9828872724339 - (sum / n));

    return result;
  }

  public static double FSchwefelGeneralizedRaw(IReadOnlyList<double> x)
  {
    var n = x.Count;
    const double schwefelConstant = 4.189828872724339;

    var penalty = 0.0;
    var sum = 0.0;

    // Boundary handling
    for (var i = 0; i < n; i++) {
      var tmp = Math.Abs(x[i]) - 500.0;
      if (tmp > 0.0) {
        penalty += tmp * tmp;
      }
    }

    // Core computation
    for (var i = 0; i < n; i++) {
      sum += x[i] * Math.Sin(Math.Sqrt(Math.Abs(x[i])));
    }

    var result = 0.01 * (penalty + (schwefelConstant * 100.0) - (sum / n));

    return result;
  }

  public static double FSharpRidgeRaw(IReadOnlyList<double> x)
  {
    var n = x.Count;
    if (n <= 1) {
      throw new ArgumentException("Sharp Ridge requires dimension > 1.", nameof(x));
    }

    const double alpha = 100.0;

    // In this simplified version: number_of_variables <= 40 ? 1 : number_of_variables / 40.0;
    // But code fixes d_vars_40 to 1.0:
    const double dVars40 = 1.0;
    var vars40 = (int)Math.Ceiling(dVars40); // always 1

    var result = 0.0;

    // Sum over indices >= vars_40
    for (var i = vars40; i < n; i++) {
      result += x[i] * x[i];
    }

    result = alpha * Math.Sqrt(result / dVars40);

    // Add contribution of first vars_40 elements
    for (var i = 0; i < vars40; i++) {
      result += x[i] * x[i] / dVars40;
    }

    return result;
  }

  public static double FSharpRidgeGeneralizedRaw(IReadOnlyList<double> x, FSharpRidgeGeneralizedVersatileData data)
  {
    const double alpha = 100.0;

    var n = x.Count;
    if (n <= 1) {
      throw new ArgumentException("Sharp Ridge (generalized) requires dimension > 1.", nameof(x));
    }

    var result = 0.0;

    var numberLinearDimensions = n / data.ProportionOfLinearDims;
    if (n % data.ProportionOfLinearDims != 0) {
      numberLinearDimensions += 1;
    }

    // Quadratic part (non-linear dimensions)
    for (var i = numberLinearDimensions; i < n; i++) {
      result += x[i] * x[i];
    }

    result = alpha * Math.Sqrt(result);

    // Linear-ish part (first numberLinearDimensions dimensions)
    for (var i = 0; i < numberLinearDimensions; i++) {
      result += x[i] * x[i];
    }

    return result;
  }

  public static double FSphereRaw(IReadOnlyList<double> x)
  {
    var result = 0.0;

    for (var i = 0; i < x.Count; i++) {
      result += x[i] * x[i];
    }

    return result;
  }

  public static double FStepEllipsoidRaw(IReadOnlyList<double> x, FStepEllipsoidData data)
  {
    const double condition = 100.0;
    const double alpha = 10.0;

    var n = x.Count;
    if (n <= 1) {
      throw new ArgumentException("Step Ellipsoid requires dimension > 1.", nameof(x));
    }

    var penalty = 0.0;

    // Boundary penalty
    for (var i = 0; i < n; i++) {
      var tmp = Math.Abs(x[i]) - 5.0;
      if (tmp > 0.0) {
        penalty += tmp * tmp;
      }
    }

    // Per-call local buffers instead of shared state
    var localX = new double[n];
    var localXx = new double[n];

    // First transformation: x -> localX
    for (var i = 0; i < n; i++) {
      var c1 = Math.Sqrt(Math.Pow(condition / 10.0, i / (n - 1.0)));
      var acc = 0.0;

      for (var j = 0; j < n; j++) {
        acc += c1 * data.Rot2[i][j] * (x[j] - data.XOpt[j]);
      }

      localX[i] = acc;
    }

    var x1 = localX[0];

    // Step / rounding
    for (var i = 0; i < n; i++) {
      if (Math.Abs(localX[i]) > 0.5) {
        localX[i] = CocoDoubleRound(localX[i]);
      } else {
        localX[i] = CocoDoubleRound(alpha * localX[i]) / alpha;
      }
    }

    // Second transformation: localX -> localXx
    for (var i = 0; i < n; i++) {
      var acc = 0.0;
      for (var j = 0; j < n; j++) {
        acc += data.Rot1[i][j] * localX[j];
      }

      localXx[i] = acc;
    }

    // Ellipsoid core
    var result = 0.0;
    for (var i = 0; i < n; i++) {
      var exponent = i / (n - 1.0);
      result += Math.Pow(condition, exponent) * localXx[i] * localXx[i];
    }

    result = (0.1 * Math.Max(Math.Abs(x1) * 1.0e-4, result)) + penalty + data.FOpt;

    return result;
  }

  public static double FWeierstrassRaw(IReadOnlyList<double> x, FWeierstrassData data)
  {
    var n = x.Count;
    var result = 0.0;

    for (var i = 0; i < n; i++) {
      for (var j = 0; j < 12; j++) {
        result += Math.Cos(2.0 * Math.PI * (x[i] + 0.5) * data.Bk[j]) * data.Ak[j];
      }
    }

    result = 10.0 * Math.Pow((result / n) - data.F0, 3.0);

    return result;
  }

  private static double CocoDoubleRound(double number) => Math.Floor(number + 0.5);
}
