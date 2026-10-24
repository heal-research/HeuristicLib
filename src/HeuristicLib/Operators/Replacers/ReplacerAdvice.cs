using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice with typed access to replacement executors and their call arguments.</summary>
/// <remarks>A transparent wrapper implements <see cref="IExecutorControlProvider"/> to forward the child's controls.</remarks>
public abstract class ReplacerAdvice<TCandidate> : OperatorAdvice<Replacement, TCandidate>
{
    public sealed override void Check<TSearchSpace, TProblem, TSearchState>(OperationInfo operation,
        IOperator<Replacement, TCandidate> source) => Check<TSearchSpace, TProblem>(source);

    public sealed override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor,
        OperationInfo operation, IOperator<Replacement, TCandidate> source) =>
        Wrap((IReplacementExecutor<TCandidate, TSearchSpace, TProblem>)executor, source);

    /// <summary>Checks the run's types before constructing the selected replacer's executor.</summary>
    /// <remarks>The default accepts every replacer.</remarks>
    protected virtual void Check<TSearchSpace, TProblem>(IOperator<Replacement, TCandidate> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    { }

    /// <summary>Wraps one replacement executor for the run's search space and problem.</summary>
    protected abstract IReplacementExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        IReplacementExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Replacement, TCandidate> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
