using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

public interface IMoveApplier<TGenotype, in TSearchSpace, in TProblem, in TMove>
    : IOperator<IMoveApplierInstance<TGenotype, TSearchSpace, TProblem, TMove>>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>;
