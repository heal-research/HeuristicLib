using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record MultiEvaluator<TCandidate, TSearchSpace, TProblem>
    : Evaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ValueArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> InnerEvaluators { get; }

    protected MultiEvaluator(IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> innerEvaluators)
    {
        InnerEvaluators = innerEvaluators.ToValueArray();
    }

    protected sealed override IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(ExecutionInstanceRegistry registry) =>
        CreateEvaluatorInstance([.. InnerEvaluators.Select(registry.Resolve)]);

    protected abstract MultiEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(ImmutableArray<IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> innerEvaluators);
}

public abstract class MultiEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> innerEvaluators)
    : EvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> InnerEvaluators { get; } = innerEvaluators;
}
