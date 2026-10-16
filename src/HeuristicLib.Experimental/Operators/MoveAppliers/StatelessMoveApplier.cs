using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

/// <remarks>
/// A move applier that carries no mutable execution data is its own executor, so it needs no separate instance
/// type. The type arguments are still the search space and problem it is written for, and a run it was not written
/// for is reported when the execution graph is built.
/// </remarks>
public abstract record StatelessMoveApplier<TCandidate, TSearchSpace, TProblem, TMove>
    : IMoveApplier<TCandidate, TMove>, IMoveApplierExecutor<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public virtual ExecutorFactory<IMoveApplierExecutor<TCandidate, TSearchSpace, TProblem, TMove>> CreateExecutorFactory() => _ => this;

    public abstract TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public bool Fits(ExecutionSignature execution) => execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    ExecutorFactory<IMoveApplierExecutor<TCandidate, TRunSearchSpace, TRunProblem, TMove>> IMoveApplier<TCandidate, TMove>.CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (ExecutorFactory<IMoveApplierExecutor<TCandidate, TRunSearchSpace, TRunProblem, TMove>>)CreateExecutorFactory();
    }
}
