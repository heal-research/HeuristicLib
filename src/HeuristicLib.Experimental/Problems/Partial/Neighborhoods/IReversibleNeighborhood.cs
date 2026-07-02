using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public interface IReversibleNeighborhood<TCandidate, in TSearchSpace, in TProblem, TMove>
    : INeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    new IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IReversibleNeighborhoodInstance<TCandidate, in TSearchSpace, in TProblem, TMove>
    : INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    TCandidate RevertMove(
        TCandidate candidate,
        TMove move,
        TSearchSpace searchSpace,
        TProblem problem);
}
