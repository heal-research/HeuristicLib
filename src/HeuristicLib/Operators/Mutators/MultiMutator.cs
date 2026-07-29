using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public abstract partial record MultiMutator<TCandidate, TSearchSpace, TProblem>
    : Mutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality]
    protected ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> InnerMutators { get; }

    protected MultiMutator(IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>> innerMutators)
    {
        InnerMutators = innerMutators.ToImmutableArray();
    }

    protected sealed override IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateMutatorInstance(ExecutionInstanceRegistry registry) =>
        CreateMutatorInstance([.. InnerMutators.Select(registry.Resolve)]);

    protected abstract MultiMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateMutatorInstance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> innerMutators);
}

public abstract class MultiMutatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> innerMutators)
    : MutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> InnerMutators { get; } = innerMutators;
}
