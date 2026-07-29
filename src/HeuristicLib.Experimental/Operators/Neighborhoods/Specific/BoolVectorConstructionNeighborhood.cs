using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

public record BoolVectorConstructionNeighborhood() : BoolVectorNeighborhood<bool>
{
    public override IEnumerable<bool> Moves(BoolVector genotype, IRandomNumberGenerator random, BoolVectorSearchSpace searchSpace, IProblem<BoolVector, BoolVectorSearchSpace> problem)
    {
        yield return false;
        yield return true;
    }

    public override BoolVector Apply(BoolVector genotype, bool move, IRandomNumberGenerator random, BoolVectorSearchSpace searchSpace, IProblem<BoolVector, BoolVectorSearchSpace> problem)
        => new(genotype.Append(move));
}
