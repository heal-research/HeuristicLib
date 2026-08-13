using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;

public record SimulatedBinaryCrossover : SingleCandidateCrossover<RealVector, RealVectorSearchSpace>
{
    /// <summary>
    /// Controls how close the offspring stays to its parents. Larger values concentrate offspring near the parents;
    /// typical values are in the range <c>[2;5]</c>.
    /// </summary>
    /// <remarks>
    /// A value below zero spreads offspring further apart than their parents instead of failing, and a non-finite
    /// value degenerates the spread factor. The result is not restricted to the search space bounds in either case.
    /// </remarks>
    public double Contiguity { get; init; } = 2;

    public override RealVector CrossParents(Parents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) => Cross(random, parents.Parent1, parents.Parent2, Contiguity);

    /// <summary>
    ///   Performs the simulated binary crossover on a real vector. Each position is crossed with a probability of 50% and if
    ///   crossed either a contracting crossover or an expanding crossover is performed, again with equal probability.
    ///   For more details refer to the paper by Deb and Agrawal.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the parents' vectors are of unequal length.</exception>
    /// <remarks>
    ///   The manipulated value is not restricted by the (possibly) specified lower and upper bounds. Use the
    ///   <see cref="BoundsChecker" /> to correct the values after performing the crossover.
    /// </remarks>
    /// <param name="contiguity">
    ///   Specifies how close a child should be to its parents; larger values mean closer. Typical values are in the
    ///   range [2;5]. See <see cref="Contiguity"/> for the behavior outside that range.
    /// </param>
    public static RealVector Cross(IRandomNumberGenerator random, RealVector parent1, RealVector parent2, double contiguity)
    {
        var length = parent1.Count;
        if (length != parent2.Count)
        {
            throw new ArgumentException("SimulatedBinaryCrossover: Parents are of unequal length");
        }

        var result = new double[length];
        for (var i = 0; i < length; i++)
        {
            if (length == 1 || random.NextDouble() < 0.5)
            {
                // cross this variable
                var u = random.NextDouble();
                var beta = u switch
                {
                    < 0.5 => Math.Pow(2 * u, 1.0 / (contiguity + 1)),
                    > 0.5 => Math.Pow(0.5 / (1.0 - u), 1.0 / (contiguity + 1)),
                    0.5 => 1,
                    _ => 0
                };

                if (random.NextDouble() < 0.5)
                {
                    result[i] = ((parent1[i] + parent2[i]) / 2.0) - (beta * 0.5 * Math.Abs(parent1[i] - parent2[i]));
                }
                else
                {
                    result[i] = ((parent1[i] + parent2[i]) / 2.0) + (beta * 0.5 * Math.Abs(parent1[i] - parent2[i]));
                }
            }
            else
            {
                result[i] = parent1[i];
            }
        }

        return RealVector.FromOwnedArray(result);
    }

    protected RealVector Cross(IRandomNumberGenerator random, RealVector[] parents)
    {
        if (parents.Length != 2)
        {
            throw new ArgumentException("SimulatedBinaryCrossover: The number of parents is not equal to 2");
        }

        return Cross(random, parents[0], parents[1], Contiguity);
    }
}

public static class Sbx
{
    /// <summary>
    ///   Simulated Binary Crossover (SBX) for two parents p1, p2.
    ///   - RealVector length is nVar and must match encoding.Minimum/Maximum length.
    ///   - Scalar eta, probVar, probBin to mirror the Python source.
    ///   Returns (child1, child2).
    /// </summary>
    public static (RealVector child1, RealVector child2) CrossSbx(RealVector p1, RealVector p2, RealVectorSearchSpace searchSpace, double eta, double probVar, double probBin, IRandomNumberGenerator rng, double eps = 1.0e-14)
    {
        var nVar = p1.Count;
        if (p2.Count != nVar)
        {
            throw new ArgumentException("p1 and p2 must have the same length.");
        }

        var xl = searchSpace.Minimum; // IReadOnlyList<double>
        var xu = searchSpace.Maximum;

        // Children start as copies (preserve parents where crossover does not apply)
        var c1 = p1.ToArray();
        var c2 = p2.ToArray();

        for (var v = 0; v < nVar; v++)
        {
            var x1 = p1[v];
            var x2 = p2[v];

            // skip fixed variables (equal bounds)
#pragma warning disable S1244
            if (xl[v % xl.Count] == xu[v % xu.Count])
            {
#pragma warning restore S1244
                continue;
            }

            // per-variable crossover decision (scalar probVar)
            var doCross = rng.NextDouble() < probVar;

            // disable if too close
            if (Math.Abs(x1 - x2) <= eps)
            {
                doCross = false;
            }

            if (!doCross)
            {
                continue;
            }

            // SBX core
            var y1 = Math.Min(x1, x2);
            var y2 = Math.Max(x1, x2);
            var delta = y2 - y1;

            if (delta <= eps)
            {
                continue;
            }

            var rv = rng.NextDouble(); // one random per locus (as in Python)

            // lower side
            var beta = 1.0 + (2.0 * (y1 - xl[v % xl.Count]) / delta);
            var betaq = CalcBetaQ(beta, eta, rv);
            var cc1 = 0.5 * (y1 + y2 - (betaq * delta));

            // upper side
            beta = 1.0 + (2.0 * (xu[v % xu.Count] - y2) / delta);
            betaq = CalcBetaQ(beta, eta, rv);
            var cc2 = 0.5 * (y1 + y2 + (betaq * delta));

            // map back to each parent�s side
            var ch1 = x1 < x2 ? cc1 : cc2; // child for parent 1
            var ch2 = x1 < x2 ? cc2 : cc1; // child for parent 2

            // exchange with XOR: (rng < probBin) XOR (x1 > x2)
            if ((rng.NextDouble() < probBin) ^ (x1 > x2))
            {
                (ch1, ch2) = (ch2, ch1);
            }

            c1[v] = ch1;
            c2[v] = ch2;
        }

        return (RealVector.Clamp(RealVector.FromOwnedArray(c1), searchSpace.Minimum, searchSpace.Maximum),
            RealVector.Clamp(RealVector.FromOwnedArray(c2), searchSpace.Minimum, searchSpace.Maximum));
    }

    public static double CalcBetaQ(double beta, double d, double rv)
    {
        var alpha = 2.0 - Math.Pow(beta, -(d + 1.0));
        return rv <= 1.0 / alpha
            ? Math.Pow(rv * alpha, 1.0 / (d + 1.0))
            : Math.Pow(1.0 / (2.0 - (rv * alpha)), 1.0 / (d + 1.0));
    }
}

public record SelfAdaptiveSimulatedBinaryCrossover : SingleCandidateCrossover<RealVector, RealVectorSearchSpace>
{
    /// <summary>
    /// Probability of crossing an individual variable, normally in <c>[0,1]</c>.
    /// </summary>
    /// <remarks>
    /// Used as a threshold: at most zero, and <c>NaN</c>, never crosses a variable, while at least one always does.
    /// </remarks>
    public double ProbVar { get; init; } = 0.5;

    /// <summary>
    /// Distribution index controlling how close the offspring stays to its parents. Larger values concentrate
    /// offspring near the parents; typical values are in the range <c>[5;30]</c>.
    /// </summary>
    /// <remarks>
    /// A value below zero spreads offspring further apart than their parents, and a non-finite value degenerates the
    /// spread factor. The result is still clamped to the search space bounds.
    /// </remarks>
    public double Eta { get; init; } = 15.0;

    /// <summary>
    /// Probability that the whole call performs the offspring exchange at all, drawn once per call rather than per
    /// variable. Normally in <c>[0,1]</c>; the default of <c>1</c> always exchanges.
    /// </summary>
    /// <remarks>
    /// Used as a threshold: at most zero, and <c>NaN</c>, always skips the exchange, while at least one always
    /// performs it.
    /// </remarks>
    public double ProbExch { get; init; } = 1.0;

    /// <summary>
    /// Probability of swapping the two offspring at a crossed variable, normally in <c>[0,1]</c>.
    /// </summary>
    /// <remarks>
    /// Used as a threshold: at most zero, and <c>NaN</c>, never swaps, while at least one always swaps.
    /// </remarks>
    public double ProbBin { get; init; } = 0.5;

    public (RealVector child1, RealVector child2) Do(RealVector p1, RealVector p2, RealVectorSearchSpace searchSpace, IRandomNumberGenerator rng, double? eta = null, double? probVar = null, double? probBin = null)
    {
        var e = eta ?? Eta;
        var pv = probVar ?? ProbVar;
        var pb = probBin ?? ProbBin;

        // Gate exchange once per call (like your simplified version)
        if (rng.NextDouble() > ProbExch)
        {
            pb = 0.0;
        }

        var (c1, c2) = Sbx.CrossSbx(p1, p2, searchSpace, e, pv, pb, rng);
        return (c1, c2);
    }

    public override RealVector CrossParents(Parents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) => Do(parents.Parent1, parents.Parent2, searchSpace, random).child1;
}
