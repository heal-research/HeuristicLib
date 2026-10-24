using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

/// <remarks>
/// A move applier that carries no mutable execution data is its own executor, so it needs no separate instance
/// type. The type arguments are still the search space and problem it is written for, and a run it was not written
/// for is reported when the execution graph is built.
/// </remarks>
public abstract record StatelessMoveApplier<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveApplication<TMove>, TCandidate, TSearchSpace, TProblem, IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>>,
      IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutor(ResolutionScope scope) => this;

    public abstract TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
