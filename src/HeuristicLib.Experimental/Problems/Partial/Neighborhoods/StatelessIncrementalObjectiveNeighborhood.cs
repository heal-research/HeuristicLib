using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record StatelessIncrementalObjectiveNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    : StatelessNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalObjectiveNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalObjectiveNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override IIncrementalObjectiveNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract ObjectiveVector? EvaluateIncrement(
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
