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
    protected MultiSelector(IReadOnlyList<ISelector<TCandidate, TSearchSpace, TProblem>> childSelectors)
    {
        var immutableChildSelectors = childSelectors.ToImmutableArray();
        ChildSelectors = immutableChildSelectors.IsDefault ? [] : immutableChildSelectors;
    }

    [OrderedEquality]
    public ImmutableArray<ISelector<TCandidate, TSearchSpace, TProblem>> ChildSelectors { get; }

    public sealed override ISelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildSelectors.Select(instanceRegistry.Resolve)]);

    protected abstract MultiSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> childSelectors);
}

public abstract class MultiSelectorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> childSelectors)
    : SelectorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> ChildSelectors { get; } = childSelectors;
}
