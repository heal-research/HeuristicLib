using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

[Equatable]
public abstract partial record MultiCreator<TCandidate, TSearchSpace, TProblem>
    : Creator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality]
    protected ImmutableArray<ICreator<TCandidate, TSearchSpace, TProblem>> InnerCreators { get; }

    protected MultiCreator(IReadOnlyList<ICreator<TCandidate, TSearchSpace, TProblem>> innerCreators)
    {
        InnerCreators = innerCreators.ToImmutableArray();
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
