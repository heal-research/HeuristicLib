using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;

public class PolynomialMutator : SingleSolutionMutator<RealVector, RealVectorSearchSpace>
{
  private readonly bool atLeastOnce;
  private readonly double eta;
  public double GetVarProb(RealVectorSearchSpace searchSpace) => Math.Min(0.5, 1.0 / searchSpace.Length);

  public PolynomialMutator(double eta = 20, bool atLeastOnce = false)
  {
    this.atLeastOnce = atLeastOnce;
    this.eta = eta;
  }

  private static bool[] mut_binomial(
    int n,
    double prob,
    bool atLeastOnce,
    IRandomNumberGenerator randomState)
  {
    ArgumentNullException.ThrowIfNull(randomState);
    // Create an n�m boolean matrix for mutations
    var matrix = new bool[n];

    // Fill random mask (true with probability 'prob')
    for (var i = 0; i < n; i++) {
      if (randomState.NextDouble() < prob) {
        matrix[i] = true;
      }
    }

    if (atLeastOnce) {
      matrix = RowAtLeastOnceTrue(matrix, randomState);
    }

    return matrix;
  }

  private static bool[] RowAtLeastOnceTrue(bool[] matrix, IRandomNumberGenerator randomState)
  {
    var n = matrix.Length;
    var atLeastOnce = false;
    for (var i = 0; i < n; i++) {
      if (!matrix[i]) {
        continue;
      }

      atLeastOnce = true;
      break;
    }

    if (atLeastOnce) {
      return matrix;
    }

    var j1 = randomState.NextInt(n); // inclusive lower, exclusive upper
    matrix[j1] = true;
    return matrix;
  }

  public override RealVector Mutate(RealVector parent, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace)
  {
    var probVar = GetVarProb(searchSpace);
    // Current vector and bounds
    var x = parent.ToArray(); // assume double[] (or expose as such)
    var xl = searchSpace.Minimum;
    var xu = searchSpace.Maximum;
    var nVar = x.Length;

    var xp = new double[nVar];
    Array.Copy(x, xp, nVar);
    var mut = mut_binomial(nVar, probVar, atLeastOnce, random);

    // Do not mutate fixed variables (xl == xu)
    for (var j = 0; j < nVar; j++) {
      if (xl[j % xl.Count] == xu[j % xu.Count]) {
        mut[j] = false;
      }
    }

    // If nothing mutates, still run the (cheap) repair for consistency and return
    var any = false;
    for (var j = 0; j < nVar; j++) {
      if (mut[j]) {
        any = true;
        break;
      }
    }

    if (!any)
      // Very unlikely
    {
      return RealVector.Clamp(xp, xl, xu);
    }

    var mutPow = 1.0 / (eta + 1.0);

    for (var j = 0; j < nVar; j++) {
      if (!mut[j]) {
        continue;
      }

      var xj = x[j];
      var lb = xl[j % xl.Count];
      var ub = xu[j % xu.Count];
      var denom = ub - lb;

      // Safety (shouldn�t happen because fixed variables got masked out)
      if (denom == 0.0) {
        xp[j] = xj;
        continue;
      }

      var delta1 = (xj - lb) / denom;
      var delta2 = (ub - xj) / denom;

      var r = random.NextDouble();
      double deltaq;

      if (r <= 0.5) {
        var xy = 1.0 - delta1;
        var val = (2.0 * r) + ((1.0 - (2.0 * r)) * Math.Pow(xy, eta + 1.0));
        deltaq = Math.Pow(val, mutPow) - 1.0;
      } else {
        var xy = 1.0 - delta2;
        var val = (2.0 * (1.0 - r)) + (2.0 * (r - 0.5) * Math.Pow(xy, eta + 1.0));
        deltaq = 1.0 - Math.Pow(val, mutPow);
      }

      var y = xj + (deltaq * denom);

      // Clamp to [lb, ub] (floating-point drift)
      if (y < lb) {
        y = lb;
      } else if (y > ub) {
        y = ub;
      }

      xp[j] = y;
    }

    // Final safety repair (very unlikely to do anything)
    return RealVector.Clamp(xp, xl, xu);
  }
}
