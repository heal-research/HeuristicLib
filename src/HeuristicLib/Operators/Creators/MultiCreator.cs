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

    protected MultiCreator(ImmutableArray<ICreator<TCandidate, TSearchSpace, TProblem>> innerCreators)
    {
        InnerCreators = innerCreators;
    }

    protected sealed override ICreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(IExecutionInstanceResolver resolver) =>
        CreateCreatorInstance([.. InnerCreators.Select(resolver.Resolve)]);

    protected abstract MultiCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> innerCreators);
}

public abstract class MultiCreatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> innerCreators)
    : CreatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> InnerCreators { get; } = innerCreators;
}
