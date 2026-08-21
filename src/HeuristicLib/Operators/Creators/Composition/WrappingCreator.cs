using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record WrappingCreator<TCandidate, TSearchSpace, TProblem>
    : Creator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingCreator(ICreator<TCandidate, TSearchSpace, TProblem> childCreator)
    {
        ChildCreator = childCreator;
    }

    public ICreator<TCandidate, TSearchSpace, TProblem> ChildCreator { get; init; }

    public sealed override ICreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildCreator));

    protected abstract WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator);
}

public abstract class WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator)
    : CreatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ICreatorInstance<TCandidate, TSearchSpace, TProblem> ChildCreator { get; } = childCreator;
}
