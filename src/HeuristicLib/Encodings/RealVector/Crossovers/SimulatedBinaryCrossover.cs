using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Encodings.RealVector.Crossovers;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVector.Crossovers;

public class SimulatedBinaryCrossover : Crossover<RealVector, RealVectorEncoding> {
  /// <summary>
  /// Performs the simulated binary crossover on a real vector. Each position is crossed with a probability of 50% and if crossed either a contracting crossover or an expanding crossover is performed, again with equal probability.
  /// For more details refer to the paper by Deb and Agrawal.
  /// </summary>
  /// <exception cref="ArgumentException">Thrown when the parents' vectors are of unequal length or when <paramref name="contiguity"/> is smaller than 0.</exception>
  /// <remarks>
  /// The manipulated value is not restricted by the (possibly) specified lower and upper bounds. Use the <see cref="BoundsChecker"/> to correct the values after performing the crossover.
  /// </remarks>
  /// <param name="random">The random number generator to use.</param>
  /// <param name="parent1">The first parent vector.</param>
  /// <param name="parent2">The second parent vector.</param>
  /// <param name="contiguity">The contiguity value that specifies how close a child should be to its parents (larger value means closer). The value must be greater or equal than 0. Typical values are in the range [2;5].</param>
  /// <returns>The vector resulting from the crossover.</returns>
  public static RealVector Apply(IRandomNumberGenerator random, RealVector parent1, RealVector parent2, double contiguity) {
    var length = parent1.Count;
    if (length != parent2.Count)
      throw new ArgumentException("SimulatedBinaryCrossover: Parents are of unequal length");
    if (contiguity < 0)
      throw new ArgumentException("SimulatedBinaryCrossover: Contiguity value is smaller than 0", "contiguity");
    var result = new double[length];
    for (var i = 0; i < length; i++) {
      if (length == 1 || random.Random() < 0.5) { // cross this variable
        var u = random.Random();
        var beta = u switch {
          < 0.5 => Math.Pow(2 * u, 1.0 / (contiguity + 1)),
          > 0.5 => Math.Pow(0.5 / (1.0 - u), 1.0 / (contiguity + 1)),
          0.5 => 1,
          _ => 0
        };

        if (random.Random() < 0.5)
          result[i] = (parent1[i] + parent2[i]) / 2.0 - beta * 0.5 * Math.Abs(parent1[i] - parent2[i]);
        else
          result[i] = (parent1[i] + parent2[i]) / 2.0 + beta * 0.5 * Math.Abs(parent1[i] - parent2[i]);
      } else
        result[i] = parent1[i];
    }

    return result;
  }

  /// <summary>
  /// Checks number of parents, availability of the parameters and forwards the call to <see cref="Apply(IRandom, RealVector, RealVector, DoubleValue)"/>.
  /// </summary>
  /// <exception cref="ArgumentException">Thrown when there are not exactly 2 parents or when the contiguity parameter could not be found.</exception>
  /// <param name="random">The random number generator.</param>
  /// <param name="parents">The collection of parents (must be of size 2).</param>
  /// <returns>The real vector resulting from the crossover.</returns>
  protected RealVector Cross(IRandomNumberGenerator random, RealVector[] parents) {
    if (parents.Length != 2)
      throw new ArgumentException("SimulatedBinaryCrossover: The number of parents is not equal to 2");
    return Apply(random, parents[0], parents[1], Contiguity);
  }

  public double Contiguity { get; } = 2;

  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorEncoding encoding) {
    return Apply(random, parents.Parent1, parents.Parent2, Contiguity);
  }
}

public static class Sbx {
  /// <summary>
  /// Simulated Binary Crossover (SBX) for two parents p1, p2.
  /// - RealVector length is nVar and must match encoding.Minimum/Maximum length.
  /// - Scalar eta, probVar, probBin to mirror the Python source.
  /// Returns (child1, child2).
  /// </summary>
  public static (RealVector child1, RealVector child2) CrossSbx(
    RealVector p1,
    RealVector p2,
    RealVectorEncoding encoding,
    double eta,
    double probVar,
    double probBin,
    IRandomNumberGenerator rng,
    double eps = 1.0e-14) {
    var nVar = p1.Count;
    if (p2.Count != nVar)
      throw new ArgumentException("p1 and p2 must have the same length.");

    var xl = encoding.Minimum; // IReadOnlyList<double>
    var xu = encoding.Maximum;

    // Children start as copies (preserve parents where crossover does not apply)
    var c1 = p1.ToArray();
    var c2 = p2.ToArray();

    for (var v = 0; v < nVar; v++) {
      var x1 = p1[v];
      var x2 = p2[v];

      // skip fixed variables (equal bounds)
      if (xl[v % xl.Count] == xu[v % xu.Count])
        continue;

      // per-variable crossover decision (scalar probVar)
      var doCross = rng.Random() < probVar;

      // disable if too close
      if (Math.Abs(x1 - x2) <= eps)
        doCross = false;

      if (!doCross)
        continue;

      // SBX core
      var y1 = Math.Min(x1, x2);
      var y2 = Math.Max(x1, x2);
      var delta = y2 - y1;

      if (delta <= eps)
        continue;

      var rv = rng.Random(); // one random per locus (as in Python)

      // lower side
      var beta = 1.0 + 2.0 * (y1 - xl[v % xl.Count]) / delta;
      var betaq = CalcBetaQ(beta, eta, rv);
      var cc1 = 0.5 * (y1 + y2 - betaq * delta);

      // upper side
      beta = 1.0 + 2.0 * (xu[v % xu.Count] - y2) / delta;
      betaq = CalcBetaQ(beta, eta, rv);
      var cc2 = 0.5 * (y1 + y2 + betaq * delta);

      // map back to each parent’s side
      var ch1 = x1 < x2 ? cc1 : cc2; // child for parent 1
      var ch2 = x1 < x2 ? cc2 : cc1; // child for parent 2

      // exchange with XOR: (rng < probBin) XOR (x1 > x2)
      if ((rng.Random() < probBin) ^ (x1 > x2))
        (ch1, ch2) = (ch2, ch1);

      c1[v] = ch1;
      c2[v] = ch2;
    }

    return (RealVector.Clamp(c1, encoding.Minimum, encoding.Maximum),
      RealVector.Clamp(c2, encoding.Minimum, encoding.Maximum));
  }

  public static double CalcBetaQ(double beta, double d, double rv) {
    var alpha = 2.0 - Math.Pow(beta, -(d + 1.0));
    return rv <= 1.0 / alpha
      ? Math.Pow(rv * alpha, 1.0 / (d + 1.0))
      : Math.Pow(1.0 / (2.0 - rv * alpha), 1.0 / (d + 1.0));
  }
}

public class SelfAdaptiveSimulatedBinaryCrossover : Crossover<RealVector, RealVectorEncoding> {
  public double ProbVar { get; set; } = 0.5;
  public double Eta { get; set; } = 15.0;
  public double ProbExch { get; set; } = 1.0; // single draw gating probBin (whole call)
  public double ProbBin { get; set; } = 0.5;

  public (RealVector child1, RealVector child2) Do(
    RealVector p1,
    RealVector p2,
    RealVectorEncoding encoding,
    IRandomNumberGenerator rng,
    double? eta = null,
    double? probVar = null,
    double? probBin = null) {
    var e = eta ?? Eta;
    var pv = probVar ?? ProbVar;
    var pb = probBin ?? ProbBin;

    // Gate exchange once per call (like your simplified version)
    if (rng.Random() > ProbExch)
      pb = 0.0;

    var (c1, c2) = Sbx.CrossSbx(p1, p2, encoding, e, pv, pb, rng);
    return (c1, c2);
  }

  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorEncoding encoding) {
    return Do(parents.Item1, parents.Item2, encoding, random).child1;
  }
}
