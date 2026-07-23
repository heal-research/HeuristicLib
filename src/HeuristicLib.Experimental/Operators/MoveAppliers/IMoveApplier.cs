namespace HEAL.HeuristicLib.Operators.MoveAppliers;

using Problems;
using SearchSpaces;

public interface IMoveApplier<TGenotype, in TSearchSpace, in TProblem, in TMove>
    : IOperator<IMoveApplierInstance<TGenotype, TSearchSpace, TProblem, TMove>>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>;
