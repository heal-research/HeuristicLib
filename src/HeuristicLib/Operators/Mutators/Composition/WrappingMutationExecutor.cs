using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract class WrappingMutationExecutor<TCandidate, TSearchSpace, TProblem>(IMutationExecutor<TCandidate, TSearchSpace, TProblem> childMutator)
    : MutationExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IMutationExecutor<TCandidate, TSearchSpace, TProblem> ChildMutator { get; } = childMutator;
}
