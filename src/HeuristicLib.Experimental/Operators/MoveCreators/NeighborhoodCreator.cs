namespace HEAL.HeuristicLib.Operators.MoveCreators;

using Problems;
using Random;
using SearchSpaces;
using Neighborhoods;

public sealed record NeighborhoodCreator<TGenotype, TSearchSpace, TProblem, TMove>(
    IDirectNeighborhood<TGenotype, TSearchSpace, TProblem, TMove> neighborhood)
    : StatelessMoveCreator<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public override IEnumerable<TMove> Moves(
        TGenotype genotype,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem)
        => neighborhood.Moves(genotype, random, searchSpace, problem);
}
