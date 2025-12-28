using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

// ReSharper disable UnusedParameter.Local
#pragma warning disable S1172
#pragma warning disable S1172

namespace HEAL.HeuristicLib.Problems.TestFunctions.BBoB;

public static partial class BBoBSuite {
  public static string GetInstancesByYear(int year) => year switch {
    >= 2023 => "1-5,101-110",
    >= 2021 => "1-5,91-100",
    >= 2018 => "1-5,71-80",
    2017 => "1-5,61-70",
    2016 or 0 => "1-5,51-60",
    2015 => "1-5,41-50",
    >= 2013 => "1-5,31-40",
    2012 => "1-5,21-30",
    >= 2010 => "1-15",
    2009 => "1-5,1-5,1-5",
    _ => throw new ArgumentOutOfRangeException(nameof(year), $"Year {year} not defined for BBoB suite.")
  };
}

public static partial class BBoBSuite {
  /// <summary>
  /// Returns the BBoB problem corresponding to (function, dimension, instance).
  /// This mirrors coco_get_bbob_problem from COCO.
  /// </summary>
  public static BBoBFunction GetProblem(int function, int dimension, int instance) {
    // Seeds (copied from COCO logic)
    long randomSeed = function + 10000L * instance;
    long randomSeed3 = 3 + 10000L * instance;
    long randomSeed17 = 17 + 10000L * instance;

    return function switch {
      1 => CreateSphereProblem(function, dimension, instance, randomSeed),
      2 => CreateEllipsoidProblem(function, dimension, instance, randomSeed),
      3 => CreateRastriginProblem(function, dimension, instance, randomSeed),
      4 => CreateBuecheRastriginProblem(function, dimension, instance, randomSeed3),
      5 => CreateLinearSlopeProblem(function, dimension, instance, randomSeed),
      6 => CreateAttractiveSectorProblem(function, dimension, instance, randomSeed),
      7 => CreateStepEllipsoidProblem(function, dimension, instance, randomSeed),
      8 => CreateRosenbrockProblem(function, dimension, instance, randomSeed),
      9 => CreateRosenbrockRotatedProblem(function, dimension, instance, randomSeed),
      10 => CreateEllipsoidRotatedProblem(function, dimension, instance, randomSeed),
      11 => CreateDiscusProblem(function, dimension, instance, randomSeed),
      12 => CreateBentCigarProblem(function, dimension, instance, randomSeed),
      13 => CreateSharpRidgeProblem(function, dimension, instance, randomSeed),
      14 => CreateDifferentPowersProblem(function, dimension, instance, randomSeed),
      15 => CreateRastriginRotatedProblem(function, dimension, instance, randomSeed),
      16 => CreateWeierstrassProblem(function, dimension, instance, randomSeed),
      17 => CreateSchaffersProblem(function, dimension, instance, randomSeed, 10),
      18 => CreateSchaffersProblem(function, dimension, instance, randomSeed17, 1000),
      19 => CreateGriewankRosenbrockProblem(function, dimension, instance, randomSeed),
      20 => CreateSchwefelProblem(function, dimension, instance, randomSeed),
      21 => CreateGallagherProblem(function, dimension, instance, randomSeed, 101),
      22 => CreateGallagherProblem(function, dimension, instance, randomSeed, 21),
      23 => CreateKatsuuraProblem(function, dimension, instance, randomSeed),
      24 => CreateLunacekBiRastriginProblem(function, dimension, instance, randomSeed),

      _ => throw new ArgumentOutOfRangeException(
        nameof(function),
        $"Cannot retrieve BBoB problem f{function} i{instance} d{dimension}.")
    };
  }
}

public static partial class BBoBSuite {
  private static BBoBFunction CreateSphereProblem(int function, int dimension, int instance, long randomSeed) {
    return new SphereFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateEllipsoidProblem(int function, int dimension, int instance, long randomSeed) {
    return new EllipsoidFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateRastriginProblem(int function, int dimension, int instance, long randomSeed) {
    return new RastriginFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateBuecheRastriginProblem(int function, int dimension, int instance, long randomSeed3) {
    return new BuecheRastriginFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateLinearSlopeProblem(int function, int dimension, int instance, long randomSeed) {
    return new LinearSlopeFunction(CreateXOpt(dimension, randomSeed)) { Dimension = dimension };
  }

  private static BBoBFunction CreateAttractiveSectorProblem(int function, int dimension, int instance, long randomSeed) {
    var data = new FAttractiveSectorData(CreateXOpt(dimension, randomSeed));
    return new AttractiveSectorFunction(data) { Dimension = dimension };
  }

  private static BBoBFunction CreateStepEllipsoidProblem(int function, int dimension, int instance, long randomSeed) {
    var data = new FStepEllipsoidData(
      CreateXOpt(dimension, randomSeed),
      Bbob2009ComputeFOpt(function, instance),
      CreateRotationMatrix(dimension, randomSeed + 1000000),
      CreateRotationMatrix(dimension, randomSeed));

    return new StepEllipsoidFunction(data) { Dimension = dimension };
  }

  private static BBoBFunction CreateRosenbrockProblem(int function, int dimension, int instance, long randomSeed) {
    // Plain Rosenbrock (unrotated)
    return new RosenbrockFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateRosenbrockRotatedProblem(int function, int dimension, int instance, long randomSeed) {
    // NOTS: BBOB has a rotated Rosenbrock variant.
    //var M = CreateRotationMatrix(dimension, randomSeed);
    return new RosenbrockFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateEllipsoidRotatedProblem(int function, int dimension, int instance, long randomSeed) {
    // Similar comment as above: BBOB's rotated ellipsoid.
    //var M = CreateRotationMatrix(dimension, randomSeed);
    return new EllipsoidFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateDiscusProblem(int function, int dimension, int instance, long randomSeed) {
    return new DiscusFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateBentCigarProblem(int function, int dimension, int instance, long randomSeed) {
    return new BentCigarFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateSharpRidgeProblem(int function, int dimension, int instance, long randomSeed) {
    return new SharpRidgeFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateDifferentPowersProblem(int function, int dimension, int instance, long randomSeed) {
    return new DifferentPowersFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateRastriginRotatedProblem(int function, int dimension, int instance, long randomSeed) {
    // BBOB rotated Rastrigin.
    return new RastriginFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateWeierstrassProblem(int function, int dimension, int instance, long randomSeed) {
    var data = new FWeierstrassData();
    const int fWeierstrassSummands = 12;
    for (var i = 0; i < fWeierstrassSummands; ++i) {
      data.Ak[i] = Math.Pow(0.5, i);
      data.Bk[i] = Math.Pow(3.0, i);
      data.F0 += data.Ak[i] * Math.Cos(2 * Math.PI * data.Bk[i] * 0.5);
    }

    return new WeierstrassFunction(data) { Dimension = dimension };
  }

  private static BBoBFunction CreateSchaffersProblem(int function, int dimension, int instance, long randomSeed, int condition) {
    // BBOB has two Schaffers F7 variants:
    // - f17 with condition 10
    // - f18 with condition 1000
    //
    // In COCO, that condition influences the transformations / scaling,
    // not the raw Schaffers function itself.
    //
    return new SchaffersFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateGriewankRosenbrockProblem(int function, int dimension, int instance, long randomSeed) {
    return new GriewankRosenbrockFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateSchwefelProblem(int function, int dimension, int instance, long randomSeed) {
    return new SchwefelFunction { Dimension = dimension };
  }

  public class FGallagherPermutation {
    public double Value;
    public int Index;
  }

  private static BBoBFunction CreateGallagherProblem(int function, int dimension, int instance, long randomSeed, int numberOfPeaks) {
    const double maxCondition = 1000.0;
    double maxCondition1 = 1000.0;
    double[] fitvalues = { 1.1, 9.1 };
    double b, c;

    switch (numberOfPeaks) {
      // Choose b, c and maxCondition1 depending on numberOfPeaks (21 or 101)
      case 101:
        maxCondition1 = Math.Sqrt(maxCondition1);
        b = 10.0;
        c = 5.0;
        break;
      case 21:
        b = 9.8;
        c = 4.9;
        break;
      default:
        throw new ArgumentException(
          $"Unsupported number of Gallagher peaks: {numberOfPeaks}",
          nameof(numberOfPeaks));
    }

    var data = new FGallagherData(randomSeed,
      NumberOfPeaks: numberOfPeaks,
      XOpt: new double[dimension],
      Rotation: AllocateMatrix(dimension, dimension),
      XLocal: AllocateMatrix(dimension, numberOfPeaks),
      ArrScales: AllocateMatrix(numberOfPeaks, dimension),
      PeakValues: new double[numberOfPeaks]);

    // 1) Random rotation
    ComputeRotation(data.Rotation, randomSeed, dimension);

    // 2) Build arrCondition and peak_values using a permutation of peaks-1
    var randomNumbers = new double[numberOfPeaks * dimension]; // large enough buffer
    Bbob2009Unif(randomNumbers, numberOfPeaks - 1, data.RandomSeed);

    var rpermPeaks = new FGallagherPermutation[numberOfPeaks - 1];
    for (int i = 0; i < numberOfPeaks - 1; i++) {
      rpermPeaks[i].Value = randomNumbers[i];
      rpermPeaks[i].Index = i;
    }

    Array.Sort(rpermPeaks, (a, b2) => a.Value.CompareTo(b2.Value));

    var arrCondition = new double[numberOfPeaks];
    arrCondition[0] = maxCondition1;
    data.PeakValues[0] = 10.0;

    for (int i = 1; i < numberOfPeaks; i++) {
      arrCondition[i] = Math.Pow(
        maxCondition,
        (double)rpermPeaks[i - 1].Index / (numberOfPeaks - 2));

      data.PeakValues[i] =
        ((double)(i - 1) / (numberOfPeaks - 2)) * (fitvalues[1] - fitvalues[0])
        + fitvalues[0];
    }

    // 3) For each peak, generate a permutation over dimensions and compute arr_scales
    var rpermDims = new FGallagherPermutation[dimension];

    for (int i = 0; i < numberOfPeaks; i++) {
      // fresh uniform numbers for this peak
      Bbob2009Unif(randomNumbers, dimension, data.RandomSeed + 1000L * i);

      for (int j = 0; j < dimension; j++) {
        rpermDims[j].Value = randomNumbers[j];
        rpermDims[j].Index = j;
      }

      Array.Sort(rpermDims, (a, b2) => a.Value.CompareTo(b2.Value));

      for (int j = 0; j < dimension; j++) {
        data.ArrScales[i][j] =
          Math.Pow(
            arrCondition[i],
            ((double)rpermDims[j].Index / (dimension - 1)) - 0.5);
      }
    }

    // 4) Generate xopt and local optima x_local
    Bbob2009Unif(randomNumbers, dimension * numberOfPeaks, data.RandomSeed);

    for (int i = 0; i < dimension; i++) {
      // Global optimum location (also used as problem->best_parameter in COCO)
      double xi = 0.8 * (b * randomNumbers[i] - c);
      data.XOpt[i] = xi;

      // Local peaks
      for (int j = 0; j < numberOfPeaks; j++) {
        double sum = 0.0;
        int baseIndex = j * dimension;

        for (int k = 0; k < dimension; k++) {
          double val = b * randomNumbers[baseIndex + k] - c;
          sum += data.Rotation[i][k] * val;
        }

        if (j == 0) {
          // First peak is treated specially (scaled by 0.8)
          sum *= 0.8;
        }

        data.XLocal[i][j] = sum;
      }
    }

    return new GallagherFunction(data) { Dimension = dimension };
  }

  private static BBoBFunction CreateKatsuuraProblem(int function, int dimension, int instance, long randomSeed) {
    return new KatsuuraFunction { Dimension = dimension };
  }

  private static BBoBFunction CreateLunacekBiRastriginProblem(
    int function,
    int dimension,
    int instance,
    long rseed) {
    const double mu0 = 2.5;

    // Allocate and fill data (mirrors f_lunacek_bi_rastrigin_bbob_problem_allocate)
    var data = new FLunacekBiRastriginData(
      XHat: new double[dimension],
      Z: new double[dimension],
      XOpt: new double[dimension],
      Rot1: AllocateMatrix(dimension, dimension),
      Rot2: AllocateMatrix(dimension, dimension)
    );

    // These two lines are in the original C code.
    // Note: xopt is later overwritten with ±0.5 * mu0 based on a Gaussian sign.
    ComputeXOpt(data.XOpt, rseed);
    ComputeRotation(data.Rot1, rseed + 1000000L, dimension);
    ComputeRotation(data.Rot2, rseed, dimension);

    // Compute best solution and final xopt: +/- 0.5 * mu0 depending on Gaussian sign
    var tmpvect = new double[dimension];
    Bbob2009Gauss(tmpvect, dimension, rseed);

    for (int i = 0; i < dimension; i++) {
      double xi = 0.5 * mu0;
      if (tmpvect[i] < 0.0)
        xi *= -1.0;
      data.XOpt[i] = xi;
    }

    return new LunacekBiRastriginFunction(data) { Dimension = dimension };
  }

  #region Helpers to mimic BBoB creation
  private static void ComputeXOpt(double[] xopt, long seed) {
    int dim = xopt.Length;

    // Generate uniform values in [0,1) — you must implement this to match COCO
    Bbob2009Unif(xopt, dim, seed);

    for (int i = 0; i < dim; i++) {
      // xopt[i] = 8 * floor(1e4 * xopt[i]) / 1e4 - 4;
      double v = xopt[i];
      double floored = Math.Floor(1e4 * v);
      xopt[i] = 8.0 * (floored / 1e4) - 4.0;

      // If exactly 0.0 → set to -1e−5
#pragma warning disable S1244
      if (xopt[i] == 0.0)
#pragma warning restore S1244
        xopt[i] = -1e-5;
    }
  }

  private static double Bbob2009ComputeFOpt(int function, int instance) {
    long rseed = function switch {
      // Special seeds for certain functions (copied exactly from COCO)
      4 => 3,
      18 => 17,
      101 or 102 or 103 or 107 or 108 or 109 => 1,
      104 or 105 or 106 or 110 or 111 or 112 => 8,
      113 or 114 or 115 => 7,
      116 or 117 or 118 => 10,
      119 or 120 or 121 => 14,
      122 or 123 or 124 => 17,
      125 or 126 or 127 => 19,
      128 or 129 or 130 => 21,
      _ => function
    };

    long rrseed = rseed + 10000L * instance;

    double gval = Bbob2009GaussSingle(rrseed);
    double gval2 = Bbob2009GaussSingle(rrseed + 1);

    // bbob2009_round(100. * 100. * gval / gval2) / 100.
    double ratio = 100.0 * 100.0 * gval / gval2;
    double rounded = Bbob2009Round(ratio) / 100.0;

    // clamp to [-1000, 1000]
    double clamped = Math.Max(-1000.0, Math.Min(1000.0, rounded));

    return clamped;
  }

  private static void Bbob2009Unif(double[] r, int N, long inseed) {
    long aktseed = inseed;

    if (aktseed < 0)
      aktseed = -aktseed;
    if (aktseed < 1)
      aktseed = 1;

    long[] rgrand = new long[32];
    long tmp;

    // Seed warm-up loop
    for (int i = 39; i >= 0; i--) {
      tmp = (long)Math.Floor(aktseed / 127773.0);

      aktseed = 16807 * (aktseed - tmp * 127773) - 2836 * tmp;

      if (aktseed < 0)
        aktseed += 2147483647;

      if (i < 32)
        rgrand[i] = aktseed;
    }

    long aktrand = rgrand[0];

    // Generate N uniform values
    for (int i = 0; i < N; i++) {
      tmp = (long)Math.Floor(aktseed / 127773.0);

      aktseed = 16807 * (aktseed - tmp * 127773) - 2836 * tmp;

      if (aktseed < 0)
        aktseed += 2147483647;

      tmp = (long)Math.Floor(aktrand / 67108865.0);
      aktrand = rgrand[tmp];
      rgrand[tmp] = aktseed;

      r[i] = aktrand / 2.147483647e9; // exactly the same divisor as COCO

      if (r[i] == 0.0)
        r[i] = 1e-99;
    }
  }

  private static void Bbob2009Gauss(double[] g, int N, long seed) {
    if (g == null)
      throw new ArgumentNullException(nameof(g));
    if (N < 0 || N > g.Length)
      throw new ArgumentOutOfRangeException(nameof(N));
    if (2 * N >= 6000)
      throw new ArgumentException("2 * N must be < 6000, as in original COCO code.", nameof(N));

    // Generate 2N uniforms using the COCO RNG we translated earlier
    double[] uniftmp = new double[2 * N];
    Bbob2009Unif(uniftmp, 2 * N, seed);

    for (int i = 0; i < N; i++) {
      double u1 = uniftmp[i];
      double u2 = uniftmp[N + i];

      // Box-Muller transform
      double val = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

      if (val == 0.0)
        val = 1e-99;

      g[i] = val;
    }
  }

  private static double Bbob2009GaussSingle(long seed) {
    var g = new double[1];
    Bbob2009Gauss(g, 1, seed);
    return g[0];
  }

  private static double Bbob2009Round(double x) {
    return Math.Floor(x + 0.5);
  }

  private static double[][] AllocateMatrix(int rows, int cols) {
    var m = new double[rows][];
    for (int r = 0; r < rows; r++)
      m[r] = new double[cols];
    return m;
  }

  private static void Reshape(double[][] B, double[] vector, int rows, int cols) {
    for (int col = 0; col < cols; col++)
    for (int row = 0; row < rows; row++)
      B[row][col] = vector[col * rows + row];
  }

  private static void ComputeRotation(double[][] B, long seed, int dim) {
    if (dim * dim >= 2000)
      throw new ArgumentException("DIM * DIM must be < 2000 per COCO constraint.");

    double[] gvect = new double[dim * dim];

    // Fill with Gaussians using the COCO RNG
    Bbob2009Gauss(gvect, dim * dim, seed);

    // Reshape gvect → matrix B
    Reshape(B, gvect, dim, dim);

    // Gram-Schmidt orthonormalization of columns
    for (int i = 0; i < dim; i++) {
      // Subtract projections onto previous columns
      for (int j = 0; j < i; j++) {
        double dot = 0.0;

        for (int k = 0; k < dim; k++)
          dot += B[k][i] * B[k][j];

        for (int k = 0; k < dim; k++)
          B[k][i] -= dot * B[k][j];
      }

      // Normalize column i
      double norm = 0.0;
      for (int k = 0; k < dim; k++)
        norm += B[k][i] * B[k][i];

      norm = Math.Sqrt(norm);

      for (int k = 0; k < dim; k++)
        B[k][i] /= norm;
    }
  }

  private static double[][] CreateRotationMatrix(int dim, long seed) {
    var B = AllocateMatrix(dim, dim);
    ComputeRotation(B, seed, dim);
    return B;
  }

  private static double[] CreateXOpt(int dimension, long randomSeed) {
    double[] bestParameter = new double[dimension]; // placeholder
    ComputeXOpt(bestParameter, randomSeed);
    return bestParameter;
  }
  #endregion
}

public abstract record BBoBFunction() : ITestFunction {
  public required int Dimension { get; init; }
  public double Min { get; init; } = -5;
  public double Max { get; init; } = 5;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;
  public abstract double Evaluate(RealVector solution);
}

public record AttractiveSectorFunction(FAttractiveSectorData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FAttractiveSectorRaw(solution, Data);
}

public record BentCigarFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FBentCigarRaw(solution);
}

public record BentCigarGeneralizedFunction(FBentCigarGeneralizedVersatileData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FBentCigarGeneralizedRaw(solution, Data);
}

public record BuecheRastriginFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FBuecheRastriginRaw(solution);
}

public record DifferentPowersFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FDifferentPowersRaw(solution);
}

public record DiscusFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FDiscusRaw(solution);
}

public record DiscusGeneralizedFunction(FDiscusGeneralizedVersatileData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FDiscusGeneralizedRaw(solution, Data);
}

public record EllipsoidFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FEllipsoidRaw(solution);
}

public record GallagherFunction(FGallagherData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FGallagherRaw(solution, Data);
}

public record GriewankRosenbrockFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FGriewankRosenbrockRaw(solution);
}

public record KatsuuraFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FKatsuuraRaw(solution);
}

public record LinearSlopeFunction(double[] BestParameter) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FLinearSlopeRaw(solution, BestParameter);
}

public record LunacekBiRastriginFunction(FLunacekBiRastriginData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FLunacekBiRastriginRaw(solution, Data);
}

public record RastriginFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FRastriginRaw(solution);
}

public record RosenbrockFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FRosenbrockRaw(solution);
}

public record SchaffersFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSchaffersRaw(solution);
}

public record SchwefelFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSchwefelRaw(solution);
}

public record SchwefelGeneralizedFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSchwefelGeneralizedRaw(solution);
}

public record SharpRidgeFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSharpRidgeRaw(solution);
}

public record SharpRidgeGeneralizedFunction(FSharpRidgeGeneralizedVersatileData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSharpRidgeGeneralizedRaw(solution, Data);
}

public record SphereFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSphereRaw(solution);
}

public record StepEllipsoidFunction(FStepEllipsoidData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FStepEllipsoidRaw(solution, Data);
}

public record WeierstrassFunction(FWeierstrassData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FWeierstrassRaw(solution, Data);
}
