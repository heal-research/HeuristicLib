using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice with typed access to selection executors and their call arguments.</summary>
/// <remarks>A transparent wrapper implements <see cref="IExecutorControlProvider"/> to forward the child's controls.</remarks>
public abstract class SelectorAdvice<TCandidate> : OperatorAdvice<Selection, TCandidate>
{
    public sealed override void Check<TSearchSpace, TProblem, TSearchState>(OperationInfo operation,
        IOperator<Selection, TCandidate> source) => Check<TSearchSpace, TProblem>(source);

    public sealed override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor,
        OperationInfo operation, IOperator<Selection, TCandidate> source) =>
        Wrap((ISelectionExecutor<TCandidate, TSearchSpace, TProblem>)executor, source);

    /// <summary>Checks the run's types before constructing the selected selector's executor.</summary>
    /// <remarks>The default accepts every selector.</remarks>
    protected virtual void Check<TSearchSpace, TProblem>(IOperator<Selection, TCandidate> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    { }

    /// <summary>Wraps one selection executor for the run's search space and problem.</summary>
    protected abstract ISelectionExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        ISelectionExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Selection, TCandidate> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
