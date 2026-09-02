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
    : IMoveApplier<TCandidate, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private sealed record Instance(MoveApplier<TCandidate, TSearchSpace, TProblem, TMove, TState> MoveApplier, TState State)
        : IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove>
    {
        public TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            MoveApplier.Apply(candidate, move, State, searchSpace, problem, random);
    }

    protected abstract TCandidate Apply(TCandidate candidate, TMove move, TState state, TSearchSpace searchSpace, TProblem problem, IRandomNumberGenerator random);

    protected abstract TState InitialState();

    public virtual IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(this, InitialState());

    IMoveApplierInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> IMoveApplier<TCandidate, TMove>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (CreateExecutionInstance(instanceRegistry) is not IMoveApplierInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> instance)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is written for {typeof(TSearchSpace).Name} and {typeof(TProblem).Name}, and cannot run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
        }

        return instance;
    }
}
