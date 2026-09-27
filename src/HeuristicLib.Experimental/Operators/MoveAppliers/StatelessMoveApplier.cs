using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

/// <remarks>
/// A move applier that carries no mutable execution data is its own execution node, so it needs no separate instance
/// type. The type arguments are still the search space and problem it is written for, and a run it was not written
/// for is reported when the execution graph is built.
/// </remarks>
public abstract record StatelessMoveApplier<TCandidate, TSearchSpace, TProblem, TMove>
    : IMoveApplier<TCandidate, TMove>, IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public virtual IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ResolutionScope scope) => this;

    public abstract TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public bool Fits(ExecutionSignature execution) => execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    IMoveApplierExecution<TCandidate, TRunSearchSpace, TRunProblem, TMove> IMoveApplier<TCandidate, TMove>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (IMoveApplierExecution<TCandidate, TRunSearchSpace, TRunProblem, TMove>)CreateExecutionInstance(scope);
    }
}
