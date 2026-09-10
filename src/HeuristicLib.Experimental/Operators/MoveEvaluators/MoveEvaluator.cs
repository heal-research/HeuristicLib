using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

/// <remarks>
/// Derive directly from this base when the move evaluator needs mutable execution data. Use
/// <see cref="StatelessMoveEvaluator{TCandidate,TSearchSpace,TProblem,TMove}"/> when it does not.
/// <para>
/// The type arguments are the search space and problem this move evaluator is written for. The base bridges to whatever a
/// run requests, and a request the move evaluator was not written for is reported when the execution graph is built.
/// </para>
/// </remarks>
public abstract record MoveEvaluator<TCandidate, TSearchSpace, TProblem, TMove, TState>
    : IMoveEvaluator<TCandidate, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private sealed record Instance(MoveEvaluator<TCandidate, TSearchSpace, TProblem, TMove, TState> MoveEvaluator, TState State)
        : IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove>
    {
        public ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            MoveEvaluator.Apply(candidate, move, State, searchSpace, problem, random);

        public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => throw new NotSupportedException("A move evaluator requires an objective and a move.");
    }

    protected abstract ObjectiveVector Apply(TCandidate candidate, TMove move, TState state, TSearchSpace searchSpace, TProblem problem, IRandomNumberGenerator random);

    protected abstract TState InitialState();

    public virtual IMoveEvaluatorInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(this, InitialState());

    public bool Fits(ExecutionSignature execution) => execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    IMoveEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> IMoveEvaluator<TCandidate, TMove>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (IMoveEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove>)CreateExecutionInstance(instanceRegistry);
    }
}
