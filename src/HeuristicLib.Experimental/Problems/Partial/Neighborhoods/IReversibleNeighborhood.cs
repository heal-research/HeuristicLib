using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public interface IReversibleNeighborhood<TGenotype, in TSearchSpace, in TProblem, TMove>
    : INeighborhood<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    TGenotype RevertMove(
        TGenotype genotype,
        TMove move,
        TSearchSpace searchSpace,
        TProblem problem);
}
