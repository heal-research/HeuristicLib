using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

[Equatable]
public abstract partial record MultiSelector<TCandidate, TSearchSpace, TProblem>
    : Selector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality]
    protected ImmutableArray<ISelector<TCandidate, TSearchSpace, TProblem>> InnerSelectors { get; }

    protected MultiSelector(ImmutableArray<ISelector<TCandidate, TSearchSpace, TProblem>> innerSelectors)
    {
        InnerSelectors = innerSelectors;
    }

    protected sealed override ISelectorInstance<TCandidate, TSearchSpace, TProblem> CreateSelectorInstance(IExecutionInstanceResolver resolver) =>
        CreateSelectorInstance([.. InnerSelectors.Select(resolver.Resolve)]);

    protected abstract MultiSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateSelectorInstance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> innerSelectors);
}

public abstract class MultiSelectorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> innerSelectors)
    : SelectorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> InnerSelectors { get; } = innerSelectors;
}
