using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record MultiMutator<TCandidate, TSearchSpace, TProblem>
    : Mutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiMutator(IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>> childMutators)
    {
        ChildMutators = childMutators.ToValueArray();
    }

    public ValueArray<IMutator<TCandidate, TSearchSpace, TProblem>> ChildMutators { get; }

    public sealed override IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildMutators.Select(instanceRegistry.Resolve)]);

    protected abstract MultiMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> childMutators);
}

public abstract class MultiMutatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> childMutators)
    : MutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> ChildMutators { get; } = childMutators;
}
