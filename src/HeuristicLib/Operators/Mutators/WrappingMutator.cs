using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record WrappingMutator<TCandidate, TSearchSpace, TProblem>
    : Mutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingMutator(IMutator<TCandidate, TSearchSpace, TProblem> childMutator)
    {
        ChildMutator = childMutator;
    }

    public IMutator<TCandidate, TSearchSpace, TProblem> ChildMutator { get; }

    public sealed override IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildMutator));

    protected abstract WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator);
}

public abstract class WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator)
    : MutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IMutatorInstance<TCandidate, TSearchSpace, TProblem> ChildMutator { get; } = childMutator;
}
