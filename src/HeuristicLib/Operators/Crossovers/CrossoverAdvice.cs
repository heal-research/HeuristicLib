using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice with typed access to crossover executors and their call arguments.</summary>
/// <remarks>A transparent wrapper implements <see cref="IExecutorControlProvider"/> to forward the child's controls.</remarks>
public abstract class CrossoverAdvice<TCandidate> : OperatorAdvice<Crossover, TCandidate>
{
    public sealed override void Check<TSearchSpace, TProblem, TSearchState>(OperationExecutorAdapter operation,
        IOperator<Crossover, TCandidate> source) => Check<TSearchSpace, TProblem>(source);

    public sealed override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor,
        OperationExecutorAdapter operation, IOperator<Crossover, TCandidate> source) =>
        Wrap((ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>)executor, source);

    /// <summary>Checks the run's types before constructing the selected crossover's executor.</summary>
    /// <remarks>The default accepts every crossover.</remarks>
    protected virtual void Check<TSearchSpace, TProblem>(IOperator<Crossover, TCandidate> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    { }

    /// <summary>Wraps one crossover executor for the run's search space and problem.</summary>
    protected abstract ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Crossover, TCandidate> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
