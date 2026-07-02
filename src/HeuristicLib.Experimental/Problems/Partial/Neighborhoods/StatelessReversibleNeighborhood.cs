using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record StatelessReversibleNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    : StatelessNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IReversibleNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract TCandidate RevertMove(
        TCandidate candidate,
        TMove move,
        TSearchSpace searchSpace,
        TProblem problem);
}
