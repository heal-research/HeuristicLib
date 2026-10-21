using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

/// <remarks>
/// Derive directly from this base when the move applier needs mutable execution data. Use
/// <see cref="StatelessMoveApplier{TCandidate,TSearchSpace,TProblem,TMove}"/> when it does not.
/// <para>
/// The type arguments are the search space and problem this move applier is written for. The base bridges to whatever a
/// run requests, and a request the move applier was not written for is reported when the execution graph is built.
/// </para>
/// </remarks>
public abstract record MoveApplier<TCandidate, TSearchSpace, TProblem, TMove, TState>
    : Operator<MoveApplication<TMove>, TCandidate, TSearchSpace, TProblem, IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract TCandidate Apply(TCandidate candidate, TMove move, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override ExecutorFactory<IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>> CreateFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(MoveApplier<TCandidate, TSearchSpace, TProblem, TMove, TState> moveApplier, TState state)
        : IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>
    {
        public TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            moveApplier.Apply(candidate, move, state, random, searchSpace, problem);
    }
}
