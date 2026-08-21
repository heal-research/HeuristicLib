using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

/// <remarks>
/// It is implemented as described in Michalewicz, Z. 1999. Genetic Algorithms + Data Structures = Evolution Programs. Third, Revised and Extended Edition, Spring-Verlag Berlin Heidelberg.
/// </remarks>
public record SinglePointCrossover : SingleCandidateCrossover<IntegerVector, IntegerVectorSearchSpace>
{
    public override IntegerVector CrossParents(Parents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        Cross(parents.Parent1, parents.Parent2, random);

    public static IntegerVector Cross(IntegerVector parent1, IntegerVector parent2, IRandomNumberGenerator random, int? crossoverPoint = null)
    {
        var cutPoint = crossoverPoint ?? random.NextInt(1, parent1.Count);
        var offspringValues = new int[parent1.Count];
        for (var i = 0; i < cutPoint; i++)
        {
            offspringValues[i] = parent1[i];
        }

        for (var i = cutPoint; i < parent2.Count; i++)
        {
            offspringValues[i] = parent2[i];
        }

        return IntegerVector.FromOwnedArray(offspringValues);
    }
}
