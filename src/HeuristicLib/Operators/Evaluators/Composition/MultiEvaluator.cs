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
    public IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildEvaluators.Select(child => typed.Resolve(child))]);
    }

    /// <summary>Combines the children's execution nodes into this operator's own.</summary>
    protected abstract IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> childEvaluators)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiEvaluatorExecution<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>> childEvaluators)
    : EvaluatorExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>> ChildEvaluators { get; } = childEvaluators;
}
