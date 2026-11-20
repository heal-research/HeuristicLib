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
public abstract record MoveEvaluator<TCandidate, TSearchSpace, TProblem, TMove>
    : Operator<MoveEvaluation<TMove>, TCandidate, TSearchSpace, TProblem, IMoveEvaluationExecutor<TCandidate, TMove>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;
