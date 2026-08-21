using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

public interface IDirectNeighborhood<TGenotype, in TSearchSpace, in TProblem, TMove>
{
    IEnumerable<TMove> Moves(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
    TGenotype Apply(TGenotype genotype, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
