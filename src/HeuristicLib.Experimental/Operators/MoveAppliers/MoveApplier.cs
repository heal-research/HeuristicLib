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

    public bool Fits(ExecutionSignature execution) => execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    IMoveApplierInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> IMoveApplier<TCandidate, TMove>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (IMoveApplierInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove>)CreateExecutionInstance(instanceRegistry);
    }
}
