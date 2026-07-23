using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    : Evaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IEvaluator<TCandidate, TSearchSpace, TProblem> InnerEvaluator { get; }

    protected WrappingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> innerEvaluator)
    {
        InnerEvaluator = innerEvaluator;
    }

    protected sealed override IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(ExecutionInstanceRegistry registry) =>
        CreateEvaluatorInstance(registry.Resolve(InnerEvaluator));

    protected abstract WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator);
}

public abstract class WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator)
    : EvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> InnerEvaluator { get; } = innerEvaluator;
}
