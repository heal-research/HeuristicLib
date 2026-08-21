using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

/// <remarks>
/// It is implemented as described in Gwiazda, T.D. 2006.
/// Genetic algorithms reference Volume I Crossover for single-objective numerical optimization problems, p.17.
/// </remarks>
public record DiscreteCrossover : SingleCandidateCrossover<IntegerVector, IntegerVectorSearchSpace>
{
    public override IntegerVector CrossParents(Parents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        Cross(random, [parents.Parent1, parents.Parent2]);

    public static IntegerVector Cross(IRandomNumberGenerator random, IReadOnlyList<IntegerVector> parents)
    {
        var n = parents.Count;
        int length = parents[0].Count;

        var result = new int[length];
        for (int i = 0; i < length; i++)
            result[i] = parents[random.NextInt(n)][i];

        return IntegerVector.FromOwnedArray(result);
    }
}
