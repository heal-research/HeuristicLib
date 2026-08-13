using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record MultiCreator<TCandidate, TSearchSpace, TProblem>
    : Creator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ValueArray<ICreator<TCandidate, TSearchSpace, TProblem>> InnerCreators { get; }

    protected MultiCreator(IReadOnlyList<ICreator<TCandidate, TSearchSpace, TProblem>> innerCreators)
    {
        InnerCreators = innerCreators.ToValueArray();
    }

    protected sealed override ICreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ExecutionInstanceRegistry registry) =>
        CreateCreatorInstance([.. InnerCreators.Select(registry.Resolve)]);

    protected abstract MultiCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> innerCreators);
}

public abstract class MultiCreatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> innerCreators)
    : CreatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> InnerCreators { get; } = innerCreators;
}
