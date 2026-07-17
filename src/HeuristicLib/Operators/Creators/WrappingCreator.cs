using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record WrappingCreator<TCandidate, TSearchSpace, TProblem>
    : Creator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ICreator<TCandidate, TSearchSpace, TProblem> InnerCreator { get; }

    protected WrappingCreator(ICreator<TCandidate, TSearchSpace, TProblem> innerCreator)
    {
        InnerCreator = innerCreator;
    }

    protected sealed override ICreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(IExecutionInstanceResolver resolver) =>
        CreateCreatorInstance(resolver.Resolve(InnerCreator));

    protected abstract WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> innerCreator);
}

public abstract class WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(ICreatorInstance<TCandidate, TSearchSpace, TProblem> innerCreator)
    : CreatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ICreatorInstance<TCandidate, TSearchSpace, TProblem> InnerCreator { get; } = innerCreator;
}
