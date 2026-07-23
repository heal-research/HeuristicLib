namespace HEAL.HeuristicLib.Operators.MoveAppliers;

using Problems;
using Random;
using SearchSpaces;

public interface IMoveApplierInstance<TGenotype, in TSearchSpace, in TProblem, in TMove>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    TGenotype Apply(
        TGenotype genotype,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
