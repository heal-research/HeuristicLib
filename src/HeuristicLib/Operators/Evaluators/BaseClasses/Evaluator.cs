using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <remarks>
/// Derive directly from this base when the evaluator needs direct control over its execution structure.
/// Use <see cref="StatelessEvaluator{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// </remarks>
public abstract record Evaluator<TCandidate, TSearchSpace, TProblem>
    : Operator<Evaluation, TCandidate, TSearchSpace, TProblem, IEvaluationExecutor<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Evaluator<TCandidate, TSearchSpace>
    : Evaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Evaluator<TCandidate>
    : Evaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class EvaluationExecutor<TCandidate>
    : IEvaluationExecutor<TCandidate>
{
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}
