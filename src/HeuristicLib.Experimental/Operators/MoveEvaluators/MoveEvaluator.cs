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
    : Operator<MoveEvaluation<TMove>, TCandidate, TSearchSpace, TProblem, IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(MoveEvaluator<TCandidate, TSearchSpace, TProblem, TMove, TState> moveEvaluator, TState state)
        : IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>
    {
        public ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            moveEvaluator.Evaluate(oldQuality, candidate, move, state, random, searchSpace, problem);

        public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => throw new NotSupportedException("A move evaluator requires an objective and a move.");
    }
}
