using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record MultiEvaluator<TCandidate, TSearchSpace, TProblem>
    : Evaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiEvaluator(IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> childEvaluators)
    {
        ChildEvaluators = childEvaluators.ToValueArray();
    }

    public ValueArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> ChildEvaluators { get; init; }

    public sealed override IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildEvaluators.Select(instanceRegistry.Resolve)]);

    protected abstract MultiEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> childEvaluators);
}

public abstract class MultiEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> childEvaluators)
    : EvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> ChildEvaluators { get; } = childEvaluators;
}
