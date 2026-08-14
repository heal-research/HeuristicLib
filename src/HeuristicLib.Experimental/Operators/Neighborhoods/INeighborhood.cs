using HEAL.HeuristicLib.Operators.MoveAppliers;
using HEAL.HeuristicLib.Operators.MoveCreators;
using HEAL.HeuristicLib.Operators.MoveEvaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;
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
