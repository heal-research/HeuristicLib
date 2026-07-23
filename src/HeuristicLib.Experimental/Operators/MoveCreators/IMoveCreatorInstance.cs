namespace HEAL.HeuristicLib.Operators.MoveCreators;

using Problems;
using Random;
using SearchSpaces;

public interface IMoveCreatorInstance<in TGenotype, in TSearchSpace, in TProblem, out TMove>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    IEnumerable<TMove> Moves(
        TGenotype genotype,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
