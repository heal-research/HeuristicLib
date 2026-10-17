using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract class MultiMutationExecutor<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> childMutators)
    : MutationExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> ChildMutators { get; } = childMutators;
}
