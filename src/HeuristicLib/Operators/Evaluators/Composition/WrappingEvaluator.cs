using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <remarks>
/// A wrapping evaluator owns a child, so it stays agnostic in the search space and problem and passes the run's
/// triple through unchanged. Binding is a leaf concept.
/// </remarks>
public abstract record WrappingEvaluator<TCandidate>
    : IEvaluator<TCandidate>
{
    protected WrappingEvaluator(IEvaluator<TCandidate> childEvaluator)
    {
        ChildEvaluator = childEvaluator;
    }

    public IEvaluator<TCandidate> ChildEvaluator { get; init; }

    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to
    /// <see cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        WrapExecutionInstance(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildEvaluator));

    /// <summary>Wraps the child's execution instance in this operator's own.</summary>
    protected abstract IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator)
    : EvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> ChildEvaluator { get; } = childEvaluator;
}
