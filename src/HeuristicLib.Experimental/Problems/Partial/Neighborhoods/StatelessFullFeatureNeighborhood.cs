using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public abstract record StatelessFullFeatureNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>
    : StatelessNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IReversibleNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalObjectiveNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalObjectiveNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalBoundNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>,
      IIncrementalBoundNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        => this;

    IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> IReversibleNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => this;

    IIncrementalObjectiveNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> IIncrementalObjectiveNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => this;

    IIncrementalBoundNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> IIncrementalBoundNeighborhood<TCandidate, TSearchSpace, TProblem, TMove>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => this;

    public abstract TCandidate RevertMove(
        TCandidate candidate,
        TMove move,
        TSearchSpace searchSpace,
        TProblem problem);

    public abstract ObjectiveVector? EvaluateIncrement(
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);

    public abstract ObjectiveVector? BoundIncrement(
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
