namespace HEAL.HeuristicLib.Operators.Neighborhoods;

using Genotypes.Vectors;
using Problems;
using Random;
using SearchSpaces.Vectors;

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
