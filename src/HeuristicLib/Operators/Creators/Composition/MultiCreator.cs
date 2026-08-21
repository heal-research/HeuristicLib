using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record MultiCreator<TCandidate, TSearchSpace, TProblem>
    : Creator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiCreator(IReadOnlyList<ICreator<TCandidate, TSearchSpace, TProblem>> childCreators)
    {
        ChildCreators = childCreators.ToValueArray();
    }

    public ValueArray<ICreator<TCandidate, TSearchSpace, TProblem>> ChildCreators { get; init; }

    public sealed override ICreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildCreators.Select(instanceRegistry.Resolve)]);

    protected abstract MultiCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> childCreators);
}

public abstract class MultiCreatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> childCreators)
    : CreatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> ChildCreators { get; } = childCreators;
}
