using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

public record RealVectorConstructionNeighborhood : RealVectorNeighborhood<double>
{
    public override IEnumerable<double> Moves(RealVector genotype, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, IProblem<RealVector, RealVectorSearchSpace> problem)
    {
        if (genotype.Count >= searchSpace.Length)
            yield break;
        while (true)
        {
            yield return random.NextDouble(searchSpace.GetMinimum(genotype.Count), searchSpace.GetMaximum(genotype.Count));
        }
    }

    public override RealVector Apply(RealVector genotype, double move, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, IProblem<RealVector, RealVectorSearchSpace> problem)
        => new(genotype.Append(move));
}
