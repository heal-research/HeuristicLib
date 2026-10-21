using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice that wraps the termination executors built for the terminators its pointcut selects.</summary>
/// <remarks>A transparent wrapper implements <see cref="IExecutorControlProvider"/> to forward the child's controls.</remarks>
public abstract class TerminatorAdvice<TCandidate> : OperatorAdvice<Termination, TCandidate>
{
    public sealed override void Check<TSearchSpace, TProblem, TSearchState>(OperationExecutorAdapter operation,
        IOperator<Termination, TCandidate> source) => Check<TSearchSpace, TProblem, TSearchState>(source);

    public sealed override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor,
        OperationExecutorAdapter operation, IOperator<Termination, TCandidate> source) =>
        Wrap((ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>)executor, source);

    /// <summary>Rejects a terminator this advice cannot wrap for the run's types, before its executor is built.</summary>
    /// <remarks>The default accepts every terminator.</remarks>
    protected virtual void Check<TSearchSpace, TProblem, TSearchState>(IOperator<Termination, TCandidate> terminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    { }

    /// <summary>Wraps one termination executor built for a selected terminator.</summary>
    protected abstract ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem, TSearchState>(
        ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IOperator<Termination, TCandidate> terminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState;
}
