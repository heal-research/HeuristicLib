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

public abstract class EvaluationExecutor<TCandidate, TSearchSpace, TProblem>
    : IEvaluationExecutor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected TProblem Problem { get; }
    protected TSearchSpace SearchSpace { get; }

    protected EvaluationExecutor(TProblem problem)
    {
        Problem = problem;
        SearchSpace = problem.SearchSpace;
    }

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}

public abstract class EvaluationExecutor<TCandidate, TSearchSpace>
    : IEvaluationExecutor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected TSearchSpace SearchSpace { get; }

    protected EvaluationExecutor(TSearchSpace searchSpace)
    {
        SearchSpace = searchSpace;
    }

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}

public abstract class EvaluationExecutor<TCandidate>
    : IEvaluationExecutor<TCandidate>
{
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}
