using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    : Evaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator)
    {
        ChildEvaluator = childEvaluator;
    }

    public IEvaluator<TCandidate, TSearchSpace, TProblem> ChildEvaluator { get; init; }

    public sealed override IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildEvaluator));

    protected abstract WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator);
}

public abstract class WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator)
    : EvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> ChildEvaluator { get; } = childEvaluator;
}
