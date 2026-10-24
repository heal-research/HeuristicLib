using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice that wraps the mutation executors built for the mutators its pointcut selects.</summary>
/// <remarks>
/// A transparent wrapper implements <see cref="IExecutorControlProvider"/> to forward the wrapped executor's
/// controls, such as mutation strength.
/// </remarks>
public abstract class MutatorAdvice<TCandidate> : OperatorAdvice<Mutation, TCandidate>
{
    public sealed override void Check<TSearchSpace, TProblem, TSearchState>(OperationInfo operation,
        IOperator<Mutation, TCandidate> source) => Check<TSearchSpace, TProblem>(source);

    public sealed override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor,
        OperationInfo operation, IOperator<Mutation, TCandidate> source) =>
        Wrap((IMutationExecutor<TCandidate, TSearchSpace, TProblem>)executor, source);

    /// <summary>Rejects a mutator this advice cannot wrap for the run's search space and problem, before its executor is built.</summary>
    /// <remarks>The default accepts every mutator.</remarks>
    protected virtual void Check<TSearchSpace, TProblem>(IOperator<Mutation, TCandidate> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    { }

    /// <summary>Wraps one mutation executor built for a selected mutator.</summary>
    protected abstract IMutationExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        IMutationExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Mutation, TCandidate> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
