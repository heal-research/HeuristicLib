namespace HEAL.HeuristicLib.Operators.Neighborhoods;

using Problems;
using SearchSpaces;
using MoveAppliers;
using MoveCreators;
using MoveEvaluators;

#region neighborhoods
public interface INeighborhood<TGenotype, in TSearchSpace, in TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    IMoveCreator<TGenotype, TSearchSpace, TProblem, TMove> MoveCreator { get; }
    IMoveApplier<TGenotype, TSearchSpace, TProblem, TMove> MoveApplier { get; }
    IMoveEvaluator<TGenotype, TSearchSpace, TProblem, TMove> MoveEvaluator { get; }
}
#endregion
