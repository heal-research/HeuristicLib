using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract record WrappingSelector<TCandidate, TSearchSpace, TProblem>
    : Selector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingSelector(ISelector<TCandidate, TSearchSpace, TProblem> childSelector)
    {
        ChildSelector = childSelector;
    }

    public ISelector<TCandidate, TSearchSpace, TProblem> ChildSelector { get; init; }

    public sealed override ISelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildSelector));

    protected abstract WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector);
}

public abstract class WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector)
    : SelectorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ISelectorInstance<TCandidate, TSearchSpace, TProblem> ChildSelector { get; } = childSelector;
}
