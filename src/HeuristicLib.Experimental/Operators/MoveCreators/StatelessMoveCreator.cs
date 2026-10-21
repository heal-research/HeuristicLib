using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveCreators;

/// <remarks>
/// A move creator that carries no mutable execution data is its own executor, so it needs no separate instance
/// type. The type arguments are still the search space and problem it is written for, and a run it was not written
/// for is reported when the execution graph is built.
/// </remarks>
public abstract record StatelessMoveCreator<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveCreation<TMove>, TCandidate, TSearchSpace, TProblem, IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>>,
      IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override ExecutorFactory<IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>> CreateFactory() => _ => this;

    public abstract IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
