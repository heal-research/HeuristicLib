using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveCreators;

/// <remarks>
/// Derive directly from this base when the move creator needs mutable execution data. Use
/// <see cref="StatelessMoveCreator{TCandidate,TSearchSpace,TProblem,TMove}"/> when it does not.
/// <para>
/// The type arguments are the search space and problem this move creator is written for. The base bridges to whatever a
/// run requests, and a request the move creator was not written for is reported when the execution graph is built.
/// </para>
/// </remarks>
public abstract record MoveCreator<TCandidate, TSearchSpace, TProblem, TMove, TState>
    : IMoveCreator<TCandidate, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private sealed record Instance(MoveCreator<TCandidate, TSearchSpace, TProblem, TMove, TState> MoveCreator, TState State)
        : IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>
    {
        public IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            MoveCreator.Moves(candidate, State, searchSpace, problem, random);
    }

    protected abstract IEnumerable<TMove> Moves(TCandidate candidate, TState state, TSearchSpace searchSpace, TProblem problem, IRandomNumberGenerator random);

    protected abstract TState InitialState();

    public virtual IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(this, InitialState());

    public bool Fits(ExecutionSignature execution) => execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    IMoveCreatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> IMoveCreator<TCandidate, TMove>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (IMoveCreatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove>)CreateExecutionInstance(instanceRegistry);
    }
}
