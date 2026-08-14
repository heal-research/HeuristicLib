using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

public sealed record NeighborhoodApplier<TGenotype, TSearchSpace, TProblem, TMove>(
    IDirectNeighborhood<TGenotype, TSearchSpace, TProblem, TMove> neighborhood)
    : StatelessMoveApplier<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public override TGenotype Apply(
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem)
        => neighborhood.Apply(genotype, move, random, searchSpace, problem);
}
