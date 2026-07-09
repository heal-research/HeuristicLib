using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record StatelessIncrementalBoundNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    : StatelessNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalBoundNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalBoundNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override IIncrementalBoundNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract ObjectiveVector? BoundIncrement(
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
