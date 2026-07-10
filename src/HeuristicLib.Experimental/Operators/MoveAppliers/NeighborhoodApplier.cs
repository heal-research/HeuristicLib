namespace HEAL.HeuristicLib.Operators.MoveAppliers;

using Problems;
using Random;
using SearchSpaces;
using Neighborhoods;

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
