using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract record WrappingSelector<TCandidate, TSearchSpace, TProblem>
    : Selector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ISelector<TCandidate, TSearchSpace, TProblem> InnerSelector { get; }

    protected WrappingSelector(ISelector<TCandidate, TSearchSpace, TProblem> innerSelector)
    {
        InnerSelector = innerSelector;
    }

    protected sealed override ISelectorInstance<TCandidate, TSearchSpace, TProblem> CreateSelectorInstance(IExecutionInstanceResolver resolver) =>
        CreateSelectorInstance(resolver.Resolve(InnerSelector));

    protected abstract WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateSelectorInstance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> innerSelector);
}

public abstract class WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(ISelectorInstance<TCandidate, TSearchSpace, TProblem> innerSelector)
    : SelectorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ISelectorInstance<TCandidate, TSearchSpace, TProblem> InnerSelector { get; } = innerSelector;
}
