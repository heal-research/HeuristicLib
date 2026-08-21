using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveCreators;

public interface IMoveCreator<in TGenotype, in TSearchSpace, in TProblem, out TMove>
    : IOperator<IMoveCreatorInstance<TGenotype, TSearchSpace, TProblem, TMove>>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>;
