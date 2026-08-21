using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public record SinglePointCrossover : SingleCandidateCrossover<RealVector, RealVectorSearchSpace>
{
    public override RealVector CrossParents(Parents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) =>
        Cross(parents.Parent1, parents.Parent2, random, searchSpace);

    public static RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, int? crossoverPoint = null) =>
        Cross(parent1, parent2, random, searchSpace.Minimum, searchSpace.Maximum, crossoverPoint);

    public static RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random, RealVector minimum, RealVector maximum, int? crossoverPoint = null)
    {
        var cutPoint = crossoverPoint ?? random.NextInt(1, parent1.Count);
        var offspringValues = new double[parent1.Count];
        for (var i = 0; i < cutPoint; i++)
        {
            offspringValues[i] = parent1[i];
        }

        for (var i = cutPoint; i < parent2.Count; i++)
        {
            offspringValues[i] = parent2[i];
        }

        return RealVector.Clamp(RealVector.FromOwnedArray(offspringValues), minimum, maximum);
    }
}
