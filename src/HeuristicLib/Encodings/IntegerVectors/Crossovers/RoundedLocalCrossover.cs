using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

public record RoundedLocalCrossover : SingleCandidateCrossover<IntegerVector, IntegerVectorSearchSpace>
{
    public override IntegerVector CrossParents(Parents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        Cross(random, parents.Parent1, parents.Parent2, searchSpace);

    public static IntegerVector Cross(IRandomNumberGenerator random, IntegerVector parent1, IntegerVector parent2, IntegerVectorSearchSpace searchSpace) =>
        Cross(random, parent1, parent2, searchSpace.Minimum, searchSpace.Maximum);

    public static IntegerVector Cross(IRandomNumberGenerator random, IntegerVector parent1, IntegerVector parent2, IntegerVector minimum, IntegerVector maximum)
    {
        int length = parent1.Count;
        var result = new int[length];
        for (int i = 0; i < length; i++)
        {
            double factor = random.NextDouble();
            double value = factor * parent1[i] + (1.0 - factor) * parent2[i];
            result[i] = RealVector.RoundToIntegerAt(value, minimum, maximum, i);
        }

        return IntegerVector.FromOwnedArray(result);
    }
}
