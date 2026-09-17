using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <remarks>
/// A multi evaluator owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record MultiEvaluator<TCandidate>
    : IEvaluator<TCandidate>
{
    protected MultiEvaluator(IReadOnlyList<IEvaluator<TCandidate>> childEvaluators)
    {
        ChildEvaluators = childEvaluators.ToValueArray();
    }

    public ValueArray<IEvaluator<TCandidate>> ChildEvaluators { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildEvaluators]);


    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to <see
    /// cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildEvaluators.Select(child => resolver.Resolve(child))]);
    }

    /// <summary>Combines the children's execution instances into this operator's own.</summary>
    protected abstract IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem>> childEvaluators)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> childEvaluators)
    : EvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> ChildEvaluators { get; } = childEvaluators;
}
