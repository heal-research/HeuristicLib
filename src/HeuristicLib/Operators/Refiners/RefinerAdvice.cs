using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice with typed access to refinement executors and their call arguments.</summary>
/// <remarks>A transparent wrapper implements <see cref="IExecutorControlProvider"/> to forward the child's controls.</remarks>
public abstract class RefinerAdvice<TCandidate> : OperatorAdvice<Refinement, TCandidate>
{
    public sealed override void Check<TSearchSpace, TProblem, TSearchState>(OperationExecutorAdapter operation,
        IOperator<Refinement, TCandidate> source) => Check<TSearchSpace, TProblem>(source);

    public sealed override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor,
        OperationExecutorAdapter operation, IOperator<Refinement, TCandidate> source) =>
        Wrap((IRefinementExecutor<TCandidate, TSearchSpace, TProblem>)executor, source);

    /// <summary>Checks the run's types before constructing the selected refiner's executor.</summary>
    /// <remarks>The default accepts every refiner.</remarks>
    protected virtual void Check<TSearchSpace, TProblem>(IOperator<Refinement, TCandidate> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    { }

    /// <summary>Wraps one refinement executor for the run's search space and problem.</summary>
    protected abstract IRefinementExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        IRefinementExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Refinement, TCandidate> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
