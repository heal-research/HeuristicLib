using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice with typed access to creation executors and their call arguments.</summary>
/// <remarks>A transparent wrapper implements <see cref="IExecutorControlProvider"/> to forward the child's controls.</remarks>
public abstract class CreatorAdvice<TCandidate> : OperatorAdvice<Creation, TCandidate>
{
    public sealed override void Check<TSearchSpace, TProblem, TSearchState>(OperationInfo operation,
        IOperator<Creation, TCandidate> source) => Check<TSearchSpace, TProblem>(source);

    public sealed override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor,
        OperationInfo operation, IOperator<Creation, TCandidate> source) =>
        Wrap((ICreationExecutor<TCandidate, TSearchSpace, TProblem>)executor, source);

    /// <summary>Checks the run's types before constructing the selected creator's executor.</summary>
    /// <remarks>The default accepts every creator.</remarks>
    protected virtual void Check<TSearchSpace, TProblem>(IOperator<Creation, TCandidate> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    { }

    /// <summary>Wraps one creation executor for the run's search space and problem.</summary>
    protected abstract ICreationExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        ICreationExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Creation, TCandidate> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
