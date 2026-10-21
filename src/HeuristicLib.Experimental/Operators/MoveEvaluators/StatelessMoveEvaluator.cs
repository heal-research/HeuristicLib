using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

/// <remarks>
/// A move evaluator that carries no mutable execution data is its own executor, so it needs no separate instance
/// type. The type arguments are still the search space and problem it is written for, and a run it was not written
/// for is reported when the execution graph is built.
/// </remarks>
public abstract record StatelessMoveEvaluator<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveEvaluation<TMove>, TCandidate, TSearchSpace, TProblem, IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>>,
      IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override ExecutorFactory<IMoveEvaluationExecutor<TCandidate, TSearchSpace, TProblem, TMove>> CreateFactory() => _ => this;

    public abstract ObjectiveVector Evaluate(ObjectiveVector oldQuality, TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => throw new NotSupportedException("A move evaluator requires an objective and a move.");
}
